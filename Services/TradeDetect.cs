using System.Threading.Tasks;
using Coflnet.Sky.PlayerState.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Collections.Generic;
using System;

namespace Coflnet.Sky.PlayerState.Services;

public class TradeDetect : UpdateListener
{
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
        foreach (var item in args.currentState.RecentViews.Where(t => t.Name.StartsWith("You    ")).Last().Items)
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
            return new Transaction()
            {
                // TODO Amount = s.
                PlayerUuid = Guid.Parse(args.currentState.McInfo.Uuid),
                Type = Transaction.TransactionType.TRADE | Transaction.TransactionType.REMOVE,
                ItemId = s.Id.HasValue ? s.Id.Value : GetIdForTag(s.Tag),
                TimeStamp = timestamp
            };
        }));
        transactions.AddRange(received.Select(s =>
        {
            return new Transaction()
            {
                // TODO Amount = s.
                PlayerUuid = Guid.Parse(args.currentState.McInfo.Uuid),
                Type = Transaction.TransactionType.TRADE | Transaction.TransactionType.RECEIVE,
                ItemId = s.Id.HasValue ? s.Id.Value : GetIdForTag(s.Tag),
                TimeStamp = timestamp
            };
        }));

        await args.stateService.ExecuteInScope(async sp =>
        {
            var service = sp.GetRequiredService<ITransactionService>();

            await service.AddTransactions(transactions.Where(t=>t.ItemId > 0));
        });
    }

    private int GetIdForTag(string tag)
    {
        return -1;
    }
}
