using System.Collections.Generic;
using MessagePack;

namespace Coflnet.Sky.PlayerState.Models;

[MessagePackObject]
public class ChestView
{
    /// <summary>
    /// All items in the ui view
    /// </summary>
    [Key(0)]
    public List<Item> Items = new ();
    /// <summary>
    /// Name of the chest
    /// </summary>
    [Key(1)]
    public string Name = string.Empty;
}
#nullable restore