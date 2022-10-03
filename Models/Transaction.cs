using System;

namespace Coflnet.Sky.PlayerState.Models;

public class Transaction
{
    [Cassandra.Mapping.Attributes.PartitionKey]
    public Guid PlayerUuid;
    public Guid ProfileUuid;
    public TransactionType Type;
    public long ItemId;
    public long Amount;
    [Cassandra.Mapping.Attributes.ClusteringKey]
    public DateTime TimeStamp;


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
        BAZAAR_SELL = BAZAAR | REMOVE,
    }
}
#nullable restore