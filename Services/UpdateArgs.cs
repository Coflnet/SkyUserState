using System;
using Coflnet.Sky.PlayerState.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Coflnet.Sky.PlayerState.Services;

public class UpdateArgs : IDisposable
{
    public UpdateMessage msg;
    public StateObject currentState;
    public PlayerStateBackgroundService stateService;
    private AsyncServiceScope? scope;


    /// <summary>
    /// Send message to user
    /// </summary>
    /// <param name="text"></param>
    public void SendMessage(string text)
    {
        stateService.TryExecuteInScope(async provider =>
        {
            var messageService = provider.GetRequiredService<EventBroker.Client.Api.IMessageApi>();
            await messageService.MessageSendUserIdPostAsync(currentState.McInfo.Uuid.ToString("N"), new()
            {
                Message = text,
                Data = msg
            });
        });
    }

    public virtual T GetService<T>() where T : notnull
    {
        if (scope == null)
            scope = stateService.scopeFactory.CreateAsyncScope();
        return scope.Value.ServiceProvider.GetRequiredService<T>();
    }

    public void Dispose()
    {
        scope?.Dispose();
    }
}
