using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Coflnet.Sky.PlayerState.Services;

public class ShensListener : UpdateListener
{
    private readonly ILogger<ShensListener> logger;

    public ShensListener(ILogger<ShensListener> logger)
    {
        this.logger = logger;
    }

    public override async Task Process(UpdateArgs args)
    {
        if (args.msg.Chest.Name != "Shen's Auction")
            return;
        logger.LogInformation("Shen's auction detected\n{chest}", JsonConvert.SerializeObject(args.msg.Chest));
    }
}
