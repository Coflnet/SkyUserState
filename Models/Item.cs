using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading;
using System.Threading.Tasks;
using Cassandra;
using Cassandra.Data.Linq;
using Cassandra.Mapping;
using MessagePack;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Coflnet.Sky.PlayerState.Models;
#nullable enable
/// <summary>
/// Transfer Object for item data
/// </summary>
[MessagePackObject]
public class Item
{
    /// <summary>
    /// 
    /// </summary>
    [Key(0)]
    public long? Id { get; set; }
    /// <summary>
    /// The item name for display
    /// </summary>
    [Key(1)]
    [BsonElement("Name")]
    public string ItemName { get; set; } = null!;
    /// <summary>
    /// Hypixel item tag for this item
    /// </summary>
    [Key(2)]
    public string Tag { get; set; } = null!;
    /// <summary>
    /// Other aditional attributes
    /// </summary>
    [Key(3)]
    public Dictionary<string, object>? ExtraAttributes { get; set; }

    /// <summary>
    /// Enchantments if any
    /// </summary>
    [Key(4)]
    public Dictionary<string, byte>? Enchantments { get; set; }  = new();
    /// <summary>
    /// Color element
    /// </summary>
    [Key(5)]
    public int? Color { get; set; } 
    /// <summary>
    /// Item Description aka Lore displayed in game, is a written form of <see cref="ExtraAttributes"/>
    /// </summary>
    [Key(6)]
    public string? Description { get; set; }

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