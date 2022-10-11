using Coflnet.Sky.PlayerState.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Coflnet.Sky.PlayerState.Services;

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
