using System.Threading.Tasks;
using System;
using System.Linq;
using System.Collections.Generic;
using Coflnet.Sky.PlayerState.Services;

namespace Coflnet.Sky.PlayerState.Bazaar;

public class BazaarListener : UpdateListener
{
    public override async Task Process(UpdateArgs args)
    {
        if(args.msg.Chest.Name != "Your Bazaar Orders")
            return;
        var offers = new List<Offer>();
        foreach (var item in args.msg.Chest.Items)
        {
            if (string.IsNullOrWhiteSpace(item?.Description))
                continue;
            if(item.ItemName.Contains("Go Back"))
                break;
            
            var parts = item.Description.Split("\n");

            var offer = new Offer()
            {
                IsSell = item.ItemName.StartsWith("§6§lSELL"),
                ItemTag = item.Tag,
                Amount = int.Parse(parts.Where(p => p.StartsWith("§7Offer amount: §a")).First().Split("§7Offer amount: §a").Last().Split("§").First()),
                PricePerUnit = double.Parse(parts.Where(p => p.StartsWith("§7Price per unit: §6")).First().Split("§7Price per unit: §6").Last().Split(" coins").First(), System.Globalization.CultureInfo.InvariantCulture),
                ItemName = item.ItemName.Substring("§6§lSELL ".Length),
                Created = item.Description.Contains("Expired") ? default : DateTime.Now,
                Customers = parts.Where(p=>p.StartsWith("§8- §a")).Select(p => new Fill(){
                    Amount = int.Parse(p.Split("§8- §a").Last().Split("§7x").First()),
                    PlayerName = p.Split("§8- §a").Last().Split("§7x").Last().Split("§f §8").First().Trim(),
                    TimeStamp = DateTime.Now
                }).ToList()
            };

            offers.Add(offer);
        }
        Console.WriteLine($"Found {offers.Count} bazaar offers for {args.currentState.PlayerId}");
        args.currentState.BazaarOffers = offers;
    }
}
