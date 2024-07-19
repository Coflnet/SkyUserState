using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Coflnet.Sky.Core;
using Item = Coflnet.Sky.Core.Item;
using RestSharp;
using MessagePack;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Coflnet.Sky.PlayerState.Models;

namespace Coflnet.Sky.PlayerState.Services;

public class TradeInfoListener : UpdateListener
{
    public ILogger<TradeInfoListener> logger;
    private RestClient sniperClient;

    public TradeInfoListener(ILogger<TradeInfoListener> logger)
    {
        this.logger = logger;
    }

    public override async Task Process(UpdateArgs args)
    {
        if (sniperClient == null)
        {
            var config = args.GetService<IConfiguration>();
            sniperClient = new(config["SNIPER_BASE_URL"] ?? throw new ArgumentNullException("SNIPER_BASE_URL"));
        }
        var chest = args.msg.Chest;
        if (chest.Name == null || !chest.Name.StartsWith("You                  "))
            return; // not a trade

        var previousChest = args.currentState.RecentViews.Where(t => t.Name?.StartsWith("You    ") ?? false).Skip(1).Take(1).LastOrDefault();
        TradeDetect.ParseTradeWindow(chest, out _, out var received);
        TradeDetect.ParseTradeWindow(chest, out _, out var previousReceived);

        var newItems = received.Where(r => !previousReceived.Any(p => p.ItemName == r.ItemName && p.Count == r.Count)).ToList();
        var prices = await GetPrices(FromitemRepresent(newItems.ToArray()));
        logger.LogInformation("Found " + newItems.Count + " new items in trade window");
        for (int i = 0; i < newItems.Count; i++)
        {
            var item = newItems[i];
            var price = prices[i];
            args.SendMessage($"Item: {item.ItemName} Count: {item.Count} showed up in trade window");
            var uid = price.Lbin.AuctionId;
            var lbin = await GetAuction(uid);
            if (lbin == null)
            {
                args.SendMessage("Most similar lbin not found");
            }
            else
                args.SendMessage($"Lbin is {price.Lbin} - click to open on ah", $"/viewauction {lbin?.Uuid}");

        }
    }

    private static async Task<SaveAuction?> GetAuction(long uid)
    {
        using (var context = new HypixelContext())
        {
            IQueryable<SaveAuction> select = context.Auctions;
            return await context.Auctions.Where(a => a.UId == uid).FirstOrDefaultAsync();

        }
    }

    public async Task<List<Sniper.Client.Model.PriceEstimate>> GetPrices(IEnumerable<SaveAuction> auctionRepresent)
    {
        var request = new RestRequest("/api/sniper/prices", RestSharp.Method.Post);
        var options = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4Block);
        request.AddJsonBody(JsonConvert.SerializeObject(Convert.ToBase64String(MessagePackSerializer.Serialize(auctionRepresent, options))));

        var respone = await sniperClient.ExecuteAsync(request).ConfigureAwait(false);
        if (respone.StatusCode == 0)
        {
            logger.LogError("sniper service could not be reached");
            return auctionRepresent.Select(a => new Sniper.Client.Model.PriceEstimate()).ToList();
        }
        try
        {
            return JsonConvert.DeserializeObject<List<Sniper.Client.Model.PriceEstimate>>(respone.Content);
        }
        catch (System.Exception)
        {
            logger.LogError("responded with " + respone.StatusCode + respone.Content);
            throw;
        }
    }

    public IEnumerable<SaveAuction> FromitemRepresent(Models.Item[] items)
    {
        return items.Select(ToAuctionRepresent);
    }

    private static SaveAuction ToAuctionRepresent(Models.Item i)
    {
        var auction = new SaveAuction()
        {
            Count = i.Count ?? 1,
            Tag = i.Tag,
            ItemName = i.ItemName,

        };
        auction.Enchantments = i.Enchantments?.Select(e => new Enchantment()
        {
            Type = Enum.TryParse<Enchantment.EnchantmentType>(e.Key, out var type) ? type : Enchantment.EnchantmentType.unknown,
            Level = e.Value
        }).ToList() ?? new();
        auction.Tier = Enum.TryParse<Tier>(i.ExtraAttributes.FirstOrDefault(a => a.Key == "tier").Value?.ToString() ?? "", out var tier) ? tier : Tier.UNKNOWN;
        auction.Reforge = Enum.TryParse<ItemReferences.Reforge>(i.ExtraAttributes.FirstOrDefault(a => a.Key == "modifier").Value?.ToString() ?? "", out var reforge) ? reforge : ItemReferences.Reforge.Unknown;
        auction.SetFlattenedNbt(NBT.FlattenNbtData(i.ExtraAttributes));
        return auction;
    }
}
