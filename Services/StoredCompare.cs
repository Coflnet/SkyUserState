using Coflnet.Sky.PlayerState.Models;
using System.Linq;
using MongoDB.Driver;
using System.Collections.Generic;
using NUnit.Framework;
using Newtonsoft.Json;

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
        return x != null && y != null && x.ExtraAttributesJson == y.ExtraAttributesJson
           && (x.Enchantments == y.Enchantments || y.Enchantments?.Count == x.Enchantments?.Count && y.Enchantments != null && x.Enchantments != null && !x.Enchantments.Except(y.Enchantments).Any())
           && x.Color == y.Color
           && x.Tag == y.Tag;
    }

    int IEqualityComparer<CassandraItem>.GetHashCode(CassandraItem obj)
    {
        return HashCode.Combine(obj.ExtraAttributesJson, obj.Enchantments.Sum(e => e.Value), obj.Tag);
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