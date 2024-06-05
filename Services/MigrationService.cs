using System.Threading;
using System.Threading.Tasks;
using Coflnet.Sky.PlayerState.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Cassandra.Data.Linq;
using Coflnet.Sky.Core;
using System.Collections.Generic;
using System.Linq;
using Cassandra;

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
        var session = await cassandraService.GetSession();
        var oldTable = cassandraService.GetItemsTable(session);
        var newTable = cassandraService.GetSplitItemsTable(session);
        var semaphore = new SemaphoreSlim(40);

        await ItemDetails.Instance.LoadLookup();
        var tags = ItemDetails.Instance.TagLookup.Keys;
        var cacheKey = "playerStatemigrateTagsDone2";
        var doneTags = await CacheService.Instance.GetFromRedis<List<string>>(cacheKey) ?? new();
        foreach (var tag in tags.Except(doneTags))
        {
            logger.LogInformation($"Migrating {tag} at {cacheKey}");
            var items = await oldTable.Where(t => t.Tag == tag).ExecuteAsync();
            foreach (var item in Batch(items, 20))
            {
                _ = Task.Run(async () =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        var batch = new BatchStatement();
                        foreach (var i in item)
                        {
                            batch.Add(newTable.Insert(i));
                        }
                        batch.SetConsistencyLevel(ConsistencyLevel.Quorum);
                        await session.ExecuteAsync(batch);
                        migrateCount.Inc(item.Count());
                        if (migrateCount.Value % 20 == 0)
                            logger.LogInformation($"Migrated {item.First().Id} {item.First().ItemName}");
                    }
                    catch (Exception e)
                    {
                        migrateFailed.Inc();
                        logger.LogError(e, $"Failed to migrate {item.First().Id} {item.First().ItemName}");
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });
                if (semaphore.CurrentCount == 0)
                    await Task.Delay(100, stoppingToken);
            }

            doneTags.Add(tag);
            await CacheService.Instance.SaveInRedis(cacheKey, doneTags);
        }

        logger.LogInformation("Migrated items");
    }

    private IEnumerable<IEnumerable<T>> Batch<T>(IEnumerable<T> values, int batchSize)
    {
        var list = new List<T>(batchSize);
        foreach (var value in values)
        {
            if (value == null)
                continue;
            list.Add(value);
            if (list.Count == batchSize)
            {
                yield return list;
                list = new List<T>(batchSize);
            }
        }

        if (list.Count > 0)
        {
            yield return list;
        }
    }
}