using System.Threading.Tasks;
using Coflnet.Sky.PlayerState.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Collections.Generic;
using System;
using Microsoft.Extensions.Logging;
using Coflnet.Sky.PlayerName.Client.Api;

namespace Coflnet.Sky.PlayerState.Services;

public class TradeDetect : UpdateListener
{
    private const int IdForCoins = 1_000_001;
    public ILogger<TradeDetect> logger;
    private CoinParser parser = new();

    public TradeDetect(ILogger<TradeDetect> logger)
    {
        this.logger = logger;
    }

    /// <inheritdoc/>
    public override async Task Process(UpdateArgs args)
    {
        if (args.msg.Kind == UpdateMessage.UpdateKind.CHAT)
        {
            Console.WriteLine("chat msg");

            var lastMessage = args.currentState.ChatHistory.Last().Content;
            if (!lastMessage.StartsWith(" + ") && !lastMessage.StartsWith(" - "))
                return;

            Console.WriteLine("trade completed");
            await StoreTrade(args);

            return;
        }
        var chest = args.msg.Chest;
        if (chest.Name == null || !chest.Name.StartsWith("You                  "))
            return; // not a trade
        var otherSide = args.msg.Chest.Name.Substring(21);
        Console.WriteLine("Got trade menu with " + args.msg.Chest.Name.Substring(21));
    }

    private async Task StoreTrade(UpdateArgs args)
    {
        var spent = new List<Item>();
        var received = new List<Item>();
        var index = 0;
        var tradeView = args.currentState.RecentViews.Where(t => t.Name?.StartsWith("You    ") ?? false).LastOrDefault();
        if (tradeView == null)
        {
            logger.LogError("no trade view was found");
            return;
        }
        foreach (var item in tradeView.Items)
        {
            var i = index++;
            if (i >= 36)
                break;
            if (item == null || item.ItemName == null)
                continue;
            var column = i % 9;
            if (column < 4)
                spent.Add(item);
            else if (column > 4)
                received.Add(item);
        }
        foreach (var item in spent)
        {
            Console.WriteLine("sent " + item.ItemName);
        }
        foreach (var item in received)
        {
            Console.WriteLine("got " + item.ItemName);
        }
        var timestamp = DateTime.UtcNow;
        var transactions = new List<Transaction>();
        transactions.AddRange(spent.Select(s =>
        {
            return CreateTransaction(args, s, timestamp, Transaction.TransactionType.TRADE | Transaction.TransactionType.REMOVE);
        }));
        transactions.AddRange(received.Select(s =>
        {
            return CreateTransaction(args, s, timestamp, Transaction.TransactionType.TRADE | Transaction.TransactionType.RECEIVE);
        }));

        // other player
        try
        {
            await AddOtherSideOfTrade(args, spent, received, timestamp, transactions, tradeView);
        }
        catch (System.Exception e)
        {
            logger.LogError(e, "Trying to add other side of trade " + tradeView.Name);
        }

        await args.stateService.ExecuteInScope(async sp =>
        {
            var service = sp.GetRequiredService<ITransactionService>();

            await service.AddTransactions(transactions.Where(t => t.ItemId > 0));
        });
    }

    private async Task AddOtherSideOfTrade(UpdateArgs args, List<Item> spent, List<Item> received, DateTime timestamp, List<Transaction> transactions, ChestView chest)
    {
        await args.stateService.ExecuteInScope(async sp =>
        {
            var nameService = args.GetService<IPlayerNameApi>();
            var uuid = Guid.Parse(await nameService.PlayerNameUuidNameGetAsync(chest.Name.Substring(21)));
            transactions.AddRange(spent.Select(s =>
            {
                return CreateTransaction(uuid, s, timestamp, Transaction.TransactionType.TRADE | Transaction.TransactionType.RECEIVE);
            }));
            transactions.AddRange(received.Select(s =>
            {
                return CreateTransaction(uuid, s, timestamp, Transaction.TransactionType.TRADE | Transaction.TransactionType.REMOVE);
            }));
        });
    }

    private Transaction CreateTransaction(UpdateArgs args, Item s, DateTime timestamp, Transaction.TransactionType type)
    {
        var playerUuid = args.currentState.McInfo.Uuid;
        return CreateTransaction(playerUuid, s, timestamp, type);
    }

    private Transaction CreateTransaction(Guid playerUuid, Item s, DateTime timestamp, Transaction.TransactionType type)
    {
        var transaction = new Transaction()
        {
            PlayerUuid = playerUuid,
            Type = type,
            ItemId = s.Id ?? GetIdForItem(s),
            TimeStamp = timestamp,
            Amount = s.Count ?? -1
        };
        if (transaction.ItemId >= 1_000_000 && transaction.ItemId < 1_999_999) // special property id
        {
            transaction.Amount = transaction.ItemId switch
            {
                IdForCoins => parser.GetCoinAmount(s),
                _ => 0
            };
        }

        return transaction;
    }

    private int GetIdForItem(Item item)
    {
        if (parser.IsCoins(item))
            return IdForCoins;
        return -1;
    }
}
