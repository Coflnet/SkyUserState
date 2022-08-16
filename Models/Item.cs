using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Coflnet.Sky.PlayerState.Models;
#nullable enable
public class Item 
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("Name")]
    public string ItemName { get; set; } = null!;

    public decimal Price { get; set; }

    public string Category { get; set; } = null!;

    public string Author { get; set; } = null!;
}
#nullable restore