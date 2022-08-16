namespace Coflnet.Sky.PlayerState.Models;

public class MongoSettings
{
    public string ConnectionString { get; set; } = null!;

    public string DatabaseName { get; set; } = null!;

    public string ItemsCollectionName { get; set; } = null!;
}
#nullable restore