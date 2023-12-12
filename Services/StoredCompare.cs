using Coflnet.Sky.PlayerState.Models;
using System.Linq;
using MongoDB.Driver;
using System.Collections.Generic;
using NUnit.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Coflnet.Sky.PlayerState.Services;
public class StoredCompare : IEqualityComparer<StoredItem>
{
    public bool Equals(StoredItem? x, StoredItem? y)
    {
        return x != null && y != null && x.ExtraAttributes.Equals(y.ExtraAttributes)
           && (x.Enchantments == y.Enchantments || y.Enchantments?.Count == x.Enchantments?.Count && y.Enchantments != null && x.Enchantments != null && !x.Enchantments.Except(y.Enchantments).Any())
           && x.Color == y.Color
           && x.Tag == y.Tag;
    }

    int IEqualityComparer<StoredItem>.GetHashCode(StoredItem obj)
    {
        return HashCode.Combine(obj.ExtraAttributes, obj.Tag);
    }
}

public class CassandraItemCompare : IEqualityComparer<CassandraItem>
{
    public bool Equals(CassandraItem? x, CassandraItem? y)
    {
        return x != null && y != null && JToken.DeepEquals(Normalize(x), Normalize(y))
           && (x.Enchantments == y.Enchantments || y.Enchantments?.Count == x.Enchantments?.Count && y.Enchantments != null && x.Enchantments != null && !x.Enchantments.Except(y.Enchantments).Any())
           && x.Color == y.Color
           && x.Tag == y.Tag
           && (x.ItemName == null || y.ItemName == null || x.ItemName == y.ItemName);
    }

    private static JObject Normalize(CassandraItem? x)
    {
        var left = JsonConvert.DeserializeObject<JObject>(x?.ExtraAttributesJson ?? "{}");
        left!.Remove("drill_fuel");
        left.Remove("compact_blocks");
        left.Remove("bottle_of_jyrre_seconds");
        left.Remove("bottle_of_jyrre_last_update");
        left.Remove("builder's_ruler_data");
        left.Remove("champion_combat_xp");
        left.Remove("farmed_cultivating");
        left.Remove("mined_crops");
        if(left.TryGetValue("petInfo", out var petInfoGeneric) && petInfoGeneric is JObject petInfo)
        {
            petInfo.Remove("active");
            petInfo.Remove("noMove");
            petInfo.Remove("uniqueId");
            petInfo.Remove("exp");
            petInfo.Remove("noMove");
            petInfo.Remove("hideInfo");
            petInfo.Remove("hideRightClick");
            left.Remove("timestamp");
            left.Remove("tier");
        }
        foreach (var item in left.Properties().ToList())
        {
            if (item.Value.Type == JTokenType.Integer && item.Value.Value<long>() > 20)
                left.Remove(item.Name);
            // also for float 
            if (item.Value.Type == JTokenType.Float)
                left.Remove(item.Name);
            if(item.Name.EndsWith("_data") || item.Name.StartsWith("personal_deletor_"))
                left.Remove(item.Name);
        }
        return left;
    }

    int IEqualityComparer<CassandraItem>.GetHashCode(CassandraItem obj)
    {
        var left = Normalize(obj);
        var hash = 17;
        foreach (var item in left)
        {
            hash = hash * 23 + item.Key.GetHashCode();
            if (item.Value.Type == JTokenType.Integer)
                hash = hash * 23 + unchecked((int)item.Value.Value<long>());
            else if (item.Value.Type == JTokenType.Float)
                hash = hash * 23 + item.Value.Value<double>().GetHashCode();
            else if (item.Value.Type == JTokenType.String)
                hash = hash * 23 + item.Value.Value<string>()?.GetHashCode() ?? 0;
            else
                // for nested objects
                hash = hash * 23 + item.Value.ToString().GetHashCode();
        }
        foreach (var item in obj.Enchantments ?? new Dictionary<string, int>())
        {
            hash = hash * 23 + item.Key.GetHashCode() + item.Value;
        }
        return HashCode.Combine(hash, obj.Tag, obj.ItemName);
    }
}

public class CassandraCompareTests
{
    [Test]
    public void UpgradeEnchant()
    {
        var compare = new CassandraItemCompare();
        var item = new Item()
        {
            Tag = "ASPECT_OF_THE_END",
            Enchantments = new Dictionary<string, byte>() { { "sharpness", 1 } },
            ExtraAttributes = new Dictionary<string, object>() { { "uuid", "96606179-dc64-4184-a356-6758856f593b" } }
        };
        var cassandraItem = new CassandraItem(item);
        var cassandraItem2 = new CassandraItem(item);
        cassandraItem.Enchantments!["sharpness"] = 2;
        Assert.IsFalse(compare.Equals(cassandraItem, cassandraItem2));
    }

    [Test]
    public void IgnoreCompactBlocks()
    {
        var compare = new CassandraItemCompare() as IEqualityComparer<CassandraItem>;
        var item = new Item()
        {
            Tag = "ASPECT_OF_THE_END",
            Enchantments = new Dictionary<string, byte>() { { "sharpness", 1 } },
            ExtraAttributes = new Dictionary<string, object>() { { "uuid", "96606179-dc64-4184-a356-6758856f593b" }, { "compact_blocks", 1 } }
        };
        var cassandraItem = new CassandraItem(item);
        item.ExtraAttributes["compact_blocks"] = 20000;
        var cassandraItem2 = new CassandraItem(item);
        Assert.IsTrue(compare.Equals(cassandraItem, cassandraItem2));
        // hashcode
        Assert.AreEqual(compare.GetHashCode(cassandraItem), compare.GetHashCode(cassandraItem2));
    }

    [Test]
    public void RemovesHighFloats()
    {
        var compare = new CassandraItemCompare() as IEqualityComparer<CassandraItem>;
        var item = new Item()
        {
            Tag = "ASPECT_OF_THE_END",
            ExtraAttributes = new Dictionary<string, object>() { { "uuid", "96606179-dc64-4184-a356-6758856f593b" }, { "champion_combat_xp", 53415000075.308676436 } }
        };
        var cassandraItem = new CassandraItem(item);
        item.ExtraAttributes["champion_combat_xp"] = 20000.0f;
        var cassandraItem2 = new CassandraItem(item);
        Assert.IsTrue(compare.Equals(cassandraItem, cassandraItem2));
        // hashcode
        Assert.AreEqual(compare.GetHashCode(cassandraItem), compare.GetHashCode(cassandraItem2));
    }

    [Test]
    public void Match()
    {
        var compare = new CassandraItemCompare();
        var item = new Item()
        {
            Tag = "ASPECT_OF_THE_END",
            Enchantments = new Dictionary<string, byte>() { { "sharpness", 1 }, { "growth", 4 }, { "protection", 4 } },
            ExtraAttributes = new Dictionary<string, object>() { { "uuid", "96606179-dc64-4184-a356-6758856f593b" } }
        };
        var cassandraItem = new CassandraItem(item);
        var cassandraItem2 = JsonConvert.DeserializeObject<CassandraItem>(JsonConvert.SerializeObject(new CassandraItem(item)));
        Assert.IsTrue(compare.Equals(cassandraItem, cassandraItem2));
    }
}