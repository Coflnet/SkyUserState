using System.Threading.Tasks;

namespace Coflnet.Sky.PlayerState.Services;

public class SettingsListener : UpdateListener
{
     /// <inheritdoc/>
    public override Task Process(UpdateArgs args)
    {
        args.currentState.Settings = args.msg.Settings;
        return Task.CompletedTask;
    }
}