using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace Coflnet.Sky.PlayerState.Models;

#nullable enable
/// <summary>
/// Internal storage object for item data (mongodb)
/// </summary>
public class StoredItem
{
    [BsonId]
    //[BsonRepresentation(BsonType.ObjectId)]
    public long? Id { get; set; }

    [BsonElement("Name")]
    public string ItemName { get; set; } = null!;
    /// <summary>
    /// Hypixel item tag for this item
    /// </summary>
    [BsonElement("Tag")]
    public string Tag { get; set; } = null!;

    /// <summary>
    /// Extra attributes object
    /// </summary>
    [JsonIgnore]
    public BsonDocument ExtraAttributes { get; set; } = new();
    [MongoDB.Bson.Serialization.Attributes.BsonIgnore]
    public Dictionary<string, object> ExtraAttrib => Traverse(ExtraAttributes.ToDictionary());

    public Dictionary<string, object> Traverse(Dictionary<string, object> target)
    {
        return target.Select(KeyValuePair<string, object> (KeyValuePair<string, object> t) =>
        {
            if (t.Value is Dictionary<object, object> sub)
                return MapObjectKeyToString(t, sub);
            return t;
        }).ToDictionary(a => a.Key, a => a.Value);
    }

    private static KeyValuePair<string, object> MapObjectKeyToString(KeyValuePair<string, object> t, Dictionary<object, object> sub)
    {
        return new(t.Key, sub.Select(innerT =>
        {
            if (t.Value is Dictionary<object, object> sub)
                return MapObjectKeyToString(innerT, sub);
            return new (innerT.Key.ToString()!, innerT.Value);
        }).ToDictionary(a => a.Key.ToString()!, a => a.Value));
    }
    private static KeyValuePair<string, object> MapObjectKeyToString(KeyValuePair<object, object> t, Dictionary<object, object> sub)
    {
        return new(t.Key.ToString()!, sub.Select(innerT =>
        {
            if (t.Value is Dictionary<object, object> sub)
                return MapObjectKeyToString(innerT, sub);
            return new (innerT.Key.ToString()!, innerT.Value);
        }).ToDictionary(a => a.Key.ToString()!, a => a.Value));
    }

    /// <summary>
    /// Enchantments if any
    /// </summary>
    public Dictionary<string, byte>? Enchantments { get; set; } = new();
    /// <summary>
    /// Color element
    /// </summary>
    public int? Color { get; set; }

    public StoredItem(Item item)
    {
        this.Id = item.Id;
        if (this.Id == null || this.Id == 0)
            this.Id = ThreadSaveIdGenerator.NextId;
        this.Enchantments = item.Enchantments;
        this.Color = item.Color;
        this.ItemName = item.ItemName;
        this.Tag = item.Tag;
        if (item.ExtraAttributes != null)
            this.ExtraAttributes = BsonDocument.Parse(JsonConvert.SerializeObject(item.ExtraAttributes));
    }

    public Item ToTransfer()
    {
        return new Item()
        {
            Color = Color,
            Enchantments = Enchantments,
            ExtraAttributes = ExtraAttrib,
            Id = Id,
            ItemName = ItemName,
            Tag = Tag
        };
    }
}


#nullable restore