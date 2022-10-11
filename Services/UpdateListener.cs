using System.Threading;
using System.Threading.Tasks;

namespace Coflnet.Sky.PlayerState.Services;

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
