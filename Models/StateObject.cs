using System;
using System.Collections.Generic;
using System.Threading;
using MessagePack;

namespace Coflnet.Sky.PlayerState.Models;

/// <summary>
/// Container for the player state
/// </summary>
[MessagePackObject]
public class StateObject
{
    /// <summary>
    /// Items in the inventory
    /// </summary>
    [Key(0)]
    public List<Item> Inventory = new List<Item>();
    /// <summary>
    /// Items in some kind of storage (enderchest, backpack etc)
    /// </summary>
    [Key(1)]
    public List<List<Item>> Storage = new List<List<Item>>();
    /// <summary>
    /// The last few views uploaded (for context)
    /// </summary>
    [Key(2)]
    public Queue<ChestView> RecentViews = new();
    /// <summary>
    /// last chat contents
    /// </summary>
    [Key(3)]
    public Queue<ChatMessage> ChatHistory = new();
    /// <summary>
    /// Purse changes
    /// </summary>
    [Key(4)]
    public Queue<PurseUpdate> PurseHistory = new();
    [Key(5)]
    public McInfo McInfo = new();
    [Key(6)]
    public string PlayerId;
    [IgnoreMember]
    public SemaphoreSlim Lock = new SemaphoreSlim(1);
}

[MessagePackObject]
public class McInfo
{
    [Key(0)]
    public string Uuid;
    [Key(1)]
    public string Name;
}
[MessagePackObject]
public class PurseUpdate
{
    [Key(0)]
    public double Amount;
    [Key(1)]
    public DateTime Time;
}
#nullable restore