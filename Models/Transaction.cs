using System;

namespace Coflnet.Sky.PlayerState.Models;

public class Transaction
{
    public Guid PlayerUuid { get; set; }
    public Guid ProfileUuid { get; set; }
    public TransactionType Type { get; set; }
    public long ItemId { get; set; }
    public long Amount { get; set; }
    public DateTime TimeStamp { get; set; }


    public enum TransactionType
    {
        UNKOWN,
        RECEIVE = 1,
        REMOVE = 2,
        BAZAAR = 4,
        AH = 8,
        NPC = 16,
        TRADE = 32,
        /// <summary>
        /// Picking up or dropping
        /// </summary>
        WORLD = 64,
        CRAFT = 128,
        BAZAAR_SELL = BAZAAR | REMOVE,
    }

    public Transaction(Transaction t)
    {
        PlayerUuid = t.PlayerUuid;
        ProfileUuid = t.ProfileUuid;
        Type = t.Type;
        ItemId = t.ItemId;
        Amount = t.Amount;
        TimeStamp = t.TimeStamp;
    }

    public Transaction()
    {
    }
}

/// <summary>
/// Maps movements of items 
/// </summary>
public class ItemTransaction : Transaction
{
    public ItemTransaction()
    {
    }

    public ItemTransaction(Transaction t) : base(t)
    {
    }
}

/// <summary>
/// Maps transactions of a player
/// </summary>
public class PlayerTransaction : Transaction
{
    public PlayerTransaction()
    {
    }

    public PlayerTransaction(Transaction t) : base(t)
    {
    }

}
#nullable restore