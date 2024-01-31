using MessagePack;

namespace Coflnet.Sky.PlayerState.Models;

/// <summary>
/// Player specific variables extracted from chat/chests
/// </summary>
[MessagePackObject]
public class ExtractedInfo 
{
    [Key(0)]
    public DateTime BoosterCookieExpires;
}
#nullable restore