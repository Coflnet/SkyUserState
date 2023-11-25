using System.Threading;
using System.Threading.Tasks;
using Coflnet.Sky.PlayerState.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Cassandra.Data.Linq;
using Coflnet.Sky.Core;
using System.Collections.Generic;
using System.Linq;
using ZstdSharp.Unsafe;

namespace Coflnet.Sky.PlayerState.Services;

/// <summary>
/// This service is used to migrate the data from the old cassandra database to the new one
/// </summary>
public class MigrationService : BackgroundService
{
    private ILogger<MigrationService> logger;
    private IConfiguration config;
    private IPersistenceService persistenceService;
    private ITransactionService transactionService;
    private ICassandraService cassandraService;
    private Prometheus.Counter migrateCount = Prometheus.Metrics.CreateCounter("sky_playerstate_migrate", "How many players were migrated");
    private Prometheus.Counter migrateFailed = Prometheus.Metrics.CreateCounter("sky_playerstate_migrate_fail", "How many players failed to migrate");

    public MigrationService(ILogger<MigrationService> logger, IConfiguration config, IPersistenceService persistenceService, ITransactionService transactionService, ICassandraService playerStateService)
    {
        this.logger = logger;
        this.config = config;
        this.persistenceService = persistenceService;
        this.transactionService = transactionService;
        this.cassandraService = playerStateService;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var oldDb = await cassandraService.GetCassandraSession();
        var itemsTable = cassandraService.GetItemsTable(oldDb);
        var newItemsTable = cassandraService.GetItemsTable(await cassandraService.GetSession());
        var itemTransactionsTable = TransactionService.GetItemTable(oldDb);
        var semaphore = new SemaphoreSlim(90);

        await ItemDetails.Instance.LoadLookup();
        var tags = ItemDetails.Instance.TagLookup.Keys;
        var cacheKey = "playerStatemigratedTags";
        var doneTags = await CacheService.Instance.GetFromRedis<List<string>>(cacheKey) ?? new();
        foreach (var tag in tags.Except(doneTags))
        {
            logger.LogInformation($"Migrating {tag}");
            var items = await itemsTable.Where(t => t.Tag == tag).ExecuteAsync();
            foreach (var item in items)
            {
                _ = Task.Run(async () =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        var transactionsTask = itemTransactionsTable.Where(t => t.ItemId == item.Id).ExecuteAsync();
                        await newItemsTable.Insert(item).ExecuteAsync();
                        await transactionService.AddTransactions((await transactionsTask).ToList());
                        migrateCount.Inc();
                        if (migrateCount.Value % 20 == 0)
                            logger.LogInformation($"Migrated {item.Id} {item.ItemName}");
                    }
                    catch (Exception e)
                    {
                        migrateFailed.Inc();
                        logger.LogError(e, $"Failed to migrate {item.Id} {item.ItemName}");
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });
                if (semaphore.CurrentCount == 0)
                    await Task.Delay(100);
            }

            doneTags.Add(tag);
            await CacheService.Instance.SaveInRedis(cacheKey, doneTags);
        }

        logger.LogInformation("Migrated player states");
    }
}