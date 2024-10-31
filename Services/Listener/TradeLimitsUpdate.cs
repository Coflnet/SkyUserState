using System.Linq;
using System.Threading.Tasks;

namespace Coflnet.Sky.PlayerState.Services;

public class TradeLimitsUpdate : UpdateListener
{
    /// <inheritdoc/>
    public override Task Process(UpdateArgs args)
    {
        foreach (var chatMsg in args.msg.ChatBatch)
        {
            if ((chatMsg.StartsWith(" + ") || chatMsg.StartsWith(" - ")) && chatMsg.Contains(" coins"))
            {
                UpdateTradeLimit(args, chatMsg);
            }
        }

        return Task.CompletedTask;

        static void UpdateTradeLimit(UpdateArgs args, string chatMsg)
        {
            var limits = args.currentState.Limits;
            while (limits.Trade.Peek().Time < args.msg.ReceivedAt.AddHours(-24))
                limits.Trade.TryDequeue(out _);
            //  + 2k coins
            var amount = Core.CoinParser.ParseCoinAmount(chatMsg.Split(' ')[1]);
            limits.Trade.Enqueue(new() { Time = args.msg.ReceivedAt, Amount = amount, Message = chatMsg });
            Console.WriteLine($"Trades value done is now {limits.Trade.Sum(t => t.Amount)} for {args.msg.PlayerId}");
        }
    }
}
