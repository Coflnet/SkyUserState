using System.Threading.Tasks;
using System;
using Coflnet.Sky.PlayerName.Client.Api;
using System.Linq;
using Coflnet.Sky.Proxy.Client.Api;
using System.Diagnostics;

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
            if (uuid != null)
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
                if (item.Description.Contains("Refreshing"))
                    Console.WriteLine("found listing with no username: " + item.ItemName);
                var sellerName = item.Description.Split('\n')
                        .Where(x => x.StartsWith("§7Seller:"))
                        .FirstOrDefault()?.Replace("§7Seller: §7", "")
                        .Split(' ').Last(); // skip rank prefix
                if (sellerName == null)
                {
                    Console.WriteLine("found listing with no username: " + item.Description);
                    continue;
                }
                var nameService = args.GetService<IPlayerNameApi>();
                // var uuid = await nameService.PlayerNameUuidNameGetAsync(sellerName);
                // Console.WriteLine("Checking listings for " + sellerName + " uuid " + uuid + " " + args.msg.Chest.Name);
                // await args.GetService<IBaseApi>().BaseAhPlayerIdPostAsync(uuid, $"player: {args.msg.PlayerId}");
            }
            Activity.Current?.AddTag("name", args.msg.Chest.Name);
            if (item.Description.Contains("Sold for"))
            {
                var parts = item.Description.Split('\n');
                Console.WriteLine($"Item from {parts.Where(x => x.StartsWith("§7Seller:")).FirstOrDefault()?.Replace("§7Seller: ", "")} sold to: "
                        + parts.Where(x => x.StartsWith("§7Buyer:")).FirstOrDefault()?.Replace("§7Buyer: ", ""));
            }
        }
        // TODO find profile
    }
}
