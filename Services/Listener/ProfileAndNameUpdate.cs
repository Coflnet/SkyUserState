using System.Threading.Tasks;
using System;
using Coflnet.Sky.PlayerName.Client.Api;

namespace Coflnet.Sky.PlayerState.Services;

/// <summary>
/// Detects profile and minecraft name/account changes and updates them
/// </summary>
public class ProfileAndNameUpdate : UpdateListener
{
    public override async Task Process(UpdateArgs args)
    {
        var state = args.currentState;
        var nameService = args.GetService<IPlayerNameApi>();
        if (state.McInfo.Uuid == default)
        {
            var uuid = await nameService.PlayerNameUuidNameGetAsync(args.msg.PlayerId);
            state.McInfo.Uuid = Guid.Parse(uuid.Trim('"'));
        }
        // TODO find profile
    }
}

/// <summary>
/// Tries to find new listings from AH Browser
/// </summary>
public class AhBrowserListener : UpdateListener
{
    public override async Task Process(UpdateArgs args)
    {
        //if (args.msg.Chest.Name != "Auctions Browser")
        //    return;
        foreach (var item in args.msg.Chest.Items)
        {
            if (item?.Description == null)
                continue;
            if (item.Description.Contains("05h 59m 5") || item.Description.Contains("Can buy in"))
            {
                Console.WriteLine("found new listing \n" + item.Description);
            }
        }
        // TODO find profile
    }
}

