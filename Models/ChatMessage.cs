using System;
using MessagePack;

namespace Coflnet.Sky.PlayerState.Models;

[MessagePackObject]
public class ChatMessage
{
    [Key(0)]
    public string Content;
    [Key(1)]
    public DateTime Time = DateTime.UtcNow;
}
#nullable restore