using System.Threading;
using System.Threading.Tasks;
using Coflnet.Sky.PlayerState.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using System.Linq;
using Microsoft.Extensions.Logging;
using Coflnet.Sky.PlayerState.Controllers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;

namespace Coflnet.Sky.PlayerState.Services;

public class PlayerStateBackgroundService : BackgroundService
{
    private IServiceScopeFactory scopeFactory;
    private IConfiguration config;
    private ILogger<PlayerStateBackgroundService> logger;
    private Prometheus.Counter consumeCount = Prometheus.Metrics.CreateCounter("sky_base_conume", "How many messages were consumed");

    public ConcurrentDictionary<string, StateObject> States = new();

    private ConcurrentDictionary<UpdateMessage.UpdateKind, List<UpdateListener>> Handlers = new();

    public PlayerStateBackgroundService(
        IServiceScopeFactory scopeFactory, IConfiguration config, ILogger<PlayerStateBackgroundService> logger)
    {
        this.scopeFactory = scopeFactory;
        this.config = config;
        this.logger = logger;

        AddHandler<ChatHistoryUpdate>(UpdateMessage.UpdateKind.CHAT);


        AddHandler<ItemIdAssignUpdate>(UpdateMessage.UpdateKind.INVENTORY);
        AddHandler<RecentViewsUpdate>(UpdateMessage.UpdateKind.INVENTORY);
        AddHandler<InventoryChangeUpdate>(UpdateMessage.UpdateKind.INVENTORY);
    }

    private void AddHandler<T>(UpdateMessage.UpdateKind kinds = UpdateMessage.UpdateKind.UNKOWN) where T : UpdateListener
    {
        var handler = Activator.CreateInstance<T>();
        foreach (var item in Enum.GetValues<UpdateMessage.UpdateKind>())
        {
            if (kinds != UpdateMessage.UpdateKind.UNKOWN && (item == UpdateMessage.UpdateKind.UNKOWN || !kinds.HasFlag(item)))
                continue;
            Handlers.GetOrAdd(item, k => new List<UpdateListener>()).Add(handler);
        }
    }
    /// <summary>
    /// Called by asp.net on startup
    /// </summary>
    /// <param name="stoppingToken">is canceled when the applications stops</param>
    /// <returns></returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("booting handlers");
        foreach (var item in Handlers.SelectMany(h => h.Value).GroupBy(h => h.GetType()).Select(g => g.First()))
        {
            await item.Load(stoppingToken);
        }
        logger.LogInformation("Initialized handlers, consuming");
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = config["KAFKA_HOST"],
            SessionTimeoutMs = 9_000,
            AutoOffsetReset = AutoOffsetReset.Latest,
            GroupId = config["KAFKA_GROUP_ID"]
        };

        await Kafka.KafkaConsumer.ConsumeBatch<UpdateMessage>(consumerConfig, new string[] { config["TOPICS:STATE_UPDATE"] }, async batch =>
        {
            if (batch.Max(b => b.ReceivedAt) < DateTime.Now - TimeSpan.FromHours(3))
                return;
            await Task.WhenAll(batch.Select(async update =>
            {
                await Update(update);
            }));
        }, stoppingToken, 5);
        var retrieved = new UpdateMessage();
    }

    private async Task Update(UpdateMessage msg)
    {
        if (msg.PlayerId == null)
            msg.PlayerId = "!anonym";
        var state = States.GetOrAdd(msg.PlayerId, (p) => new StateObject() { PlayerId = p });
        var args = new UpdateArgs()
        {
            currentState = state,
            msg = msg,
            stateService = this
        };
        try
        {
            await state.Lock.WaitAsync();
            foreach (var item in Handlers[msg.Kind])
            {
                await item.Process(args);
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "failed update state on " + msg.Kind + " with " + JsonConvert.SerializeObject(msg));
        }
        finally
        {
            state.Lock.Release();
        }
    }

    public async Task ExecuteInScope(Func<IServiceProvider, Task> todo)
    {
        using var scope = scopeFactory.CreateScope();
        await todo(scope.ServiceProvider);
    }

    public void TryExecuteInScope(Func<IServiceProvider, Task> todo)
    {
        Task.Run(async () =>
        {
            try
            {
                await ExecuteInScope(todo);
            }
            catch (Exception e)
            {
                logger.LogError(e, "failed to execute in scope");
            }
        });
    }
}

public class InventoryChangeUpdate : UpdateListener
{
    /// <inheritdoc/>
    public override async Task Process(UpdateArgs args)
    {
        Console.WriteLine("updated now there is " + args.msg.Chest.Items.Where(i => i != null && string.IsNullOrWhiteSpace(i.ItemName)).FirstOrDefault()?.ItemName);
        //Console.WriteLine(args.msg.Chest.Name + "\n" + JsonConvert.SerializeObject(args.msg.Chest.Items));
        args.currentState.Inventory = args.msg.Chest.Items.Reverse<Item>().Take(36).Reverse().ToList();
    }
}

public class ItemIdAssignUpdate : UpdateListener
{
    private ItemCompare comparer = new();
    public override async Task Process(UpdateArgs args)
    {

        await args.stateService.ExecuteInScope(async sp =>
        {
            var service = sp.GetRequiredService<ItemsService>();
            var collection = args.msg.Chest.Items;
            var toSearchFor = collection.Where(HasToBeStoredInMongo).ToHashSet();
            var localPresent = args.currentState.RecentViews.SelectMany(s => s.Items).GroupBy(e => e, comparer).Select(e => e.First()).ToDictionary(e => e, comparer);
            var foundLocal = toSearchFor.Select(s => localPresent.Values.Where(b => comparer.Equals(b, s)).FirstOrDefault()).Where(s => s != null).ToList();
            var itemsWithIds = await service.FindOrCreate(toSearchFor.Except(foundLocal, comparer));

            Console.WriteLine("to search: " + toSearchFor.Count + " found local: " + foundLocal.Count + " from db: " + itemsWithIds.Count + " present: " + localPresent.Count);
            args.msg.Chest.Items = Join(collection, itemsWithIds.Concat(foundLocal)).ToList();
        });
    }

    private static bool HasToBeStoredInMongo(Item i)
    {
        return i.ExtraAttributes != null && i.ExtraAttributes.Count != 0 && i.Enchantments?.Count != 0 && !IsNpcSell(i);
    }

    private static bool IsNpcSell(Item i)
    {
        // Another valid indicator would be "Click to trade!"
        return i.Description?.Contains("§7Cost\n") ?? false;
    }

    private IEnumerable<Item> Join(IEnumerable<Item> original, IEnumerable<Item> mongo)
    {
        var mcount = 0;
        foreach (var item in original)
        {
            var inMogo = mongo.Where(m => comparer.Equals(item, m)).FirstOrDefault();
            if (inMogo != null)
            {
                yield return inMogo;
                mcount++;
            }
            else
                yield return item;
        }
        Console.WriteLine("replaced count: " + mcount);
    }
}

public class UpdateArgs
{
    public UpdateMessage msg;
    public StateObject currentState;
    public PlayerStateBackgroundService stateService;

    /// <summary>
    /// Send message to user
    /// </summary>
    /// <param name="text"></param>
    public void SendMessage(string text)
    {
        stateService.TryExecuteInScope(async provider =>
        {
            var messageService = provider.GetRequiredService<EventBroker.Client.Api.IMessageApi>();
            await messageService.MessageSendUserIdPostAsync(currentState.McInfo.Uuid, new()
            {
                Message = text,
                Data = msg
            });
        });
    }
}

public abstract class UpdateListener
{
    /// <summary>
    /// Process an update
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public abstract Task Process(UpdateArgs args);
    /// <summary>
    /// Called when registering to do async loading stuff
    /// </summary>
    public virtual Task Load(CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }
}
