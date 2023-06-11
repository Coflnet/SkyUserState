using System;
using System.Collections.Generic;
using System.Linq;

namespace Coflnet.Sky.PlayerState.Models;
#nullable enable
/// <summary>
/// see <see cref="TransactionService.GetItemsTable"/> for key definition
/// </summary>
public class CassandraItem
{
    public Guid ItemId { get; set; }
    /// <summary>
    /// Numeric internal id
    /// </summary>
    public long? Id { get; set; }
    public string? ItemName { get; set; }
    public string Tag { get; set; } = null!;
    public string ExtraAttributesJson { get; set; } = null!;
    public Dictionary<string, int>? Enchantments { get; set; }
    public int? Color { get; set; }

    public CassandraItem(Item item)
    {
        ItemId = item.ExtraAttributes?.TryGetValue("uuid", out var uuid) == true ? Guid.Parse(uuid.ToString()!) : default;
        ItemName = item.ItemName;
        Tag = item.Tag;
        Enchantments = item.Enchantments?.OrderBy(k=>k.Key).ToDictionary(x => x.Key, x =>(int) x.Value) ?? new Dictionary<string, int>();
        Color = item.Color;
        Id = item.Id;
        ExtraAttributesJson = Newtonsoft.Json.JsonConvert.SerializeObject(item.ExtraAttributes);
    }

    public CassandraItem()
    {
    }

    internal Item ToTransfer()
    {
        return new Item()
        {
            Color = Color,
            Enchantments = Enchantments?.ToDictionary(x => x.Key, x =>(byte) x.Value),
            ExtraAttributes = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(ExtraAttributesJson),
            Id = Id,
            ItemName = ItemName,
            Tag = Tag,
            Count = 1
        };
    }
}


#nullable restore