using System.Threading.Tasks;

namespace Coflnet.Sky.PlayerState.Services;

public class RecentViewsUpdate : UpdateListener
{
    /// <inheritdoc/>
    public override Task Process(UpdateArgs args)
    {
        var recentViews = args.currentState.RecentViews;
        recentViews.Enqueue(args.msg.Chest);
        while (recentViews.Count > 3)
            recentViews.TryDequeue(out _);

        return Task.CompletedTask;
    }
}
