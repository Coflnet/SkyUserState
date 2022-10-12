using System.Threading.Tasks;

namespace Coflnet.Sky.PlayerState.Services;

public class ChatHistoryUpdate : UpdateListener
{
    /// <inheritdoc/>
    public override Task Process(UpdateArgs args)
    {
        var chatHistory = args.currentState.ChatHistory;
        foreach (var chatMsg in args.msg.ChatBatch)
        {
            chatHistory.Enqueue(new() { Content = chatMsg, Time = args.msg.ReceivedAt });
        }
        while (chatHistory.Count > 100)
            chatHistory.TryDequeue(out _);
        return Task.CompletedTask;
    }
}
