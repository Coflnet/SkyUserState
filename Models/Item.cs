using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Cassandra;
using Cassandra.Data.Linq;
using Cassandra.Mapping;
using Microsoft.Extensions.Configuration;
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
    /// <summary>
    /// Hypixel item tag for this item
    /// </summary>
    [BsonElement("Tag")]
    public string Tag { get; set; } = null!;

    /// <summary>
    /// Extra attributes object
    /// </summary>
    [JsonIgnore]
    public BsonDocument ExtraAttributes { get; set; } = new();
    [MongoDB.Bson.Serialization.Attributes.BsonIgnore]
    public Dictionary<string, object> ExtraAttrib => ExtraAttributes.ToDictionary();

    /// <summary>
    /// Enchantments if any
    /// </summary>
    public Dictionary<string, byte>? Enchantments { get; set; }  = new();
    /// <summary>
    /// Color element
    /// </summary>
    public int? Color { get; set; } 

}

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

public class TransactionService
{
    ISession _session;
    private SemaphoreSlim sessionOpenLock = new SemaphoreSlim(1);
    private IConfiguration config;
    public async Task Create()
    {
        var session = await GetSession();
        var mapping = new MappingConfiguration();
        //mapping.Define(new Map<Transaction>().Column(t => t.PlayerUuid, cm => cm.WithDbType<TimeUuid>()));
        var table = new Table<Transaction>(session, mapping, "transactions");
        table.SetConsistencyLevel(ConsistencyLevel.Quorum);
    }

    public async Task<ISession> GetSession(string keyspace = "sky_item_movement")
    {
        if (_session != null)
            return _session;
        await sessionOpenLock.WaitAsync();
        if (_session != null)
            return _session;
        try
        {

            var cluster = Cluster.Builder()
                                .WithCredentials(config["CASSANDRA:USER"], config["CASSANDRA:PASSWORD"])
                                .AddContactPoints(config["CASSANDRA:HOSTS"].Split(","))
                                .Build();
            if (keyspace == null)
                return await cluster.ConnectAsync();
            _session = await cluster.ConnectAsync(keyspace);
        }
        catch (Exception e)
        {
            //logger.LogError(e, "failed to connect to cassandra");
            throw e;
        }
        finally
        {
            sessionOpenLock.Release();
        }
        return _session;
    }
}
#nullable restore