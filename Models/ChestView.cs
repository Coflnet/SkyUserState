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
    [Key(1)]
    public string Name;
}
#nullable restore