using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Coflnet.Sky.PlayerState.Services;
using Coflnet.Sky.PlayerState.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace Coflnet.Sky.PlayerState.Bazaar;

public class BazaarListener : UpdateListener
{
    public override Task Process(UpdateArgs args)
    {
        if (args.msg.Chest?.Name != "Your Bazaar Orders" && args.msg.Chest?.Name != "Co-op Bazaar Orders")
            return Task.CompletedTask;
        var offers = new List<Offer>();
        // only the first 5 rows (x9) are potential orders (to include bazaar upgrade)
        var bazaarItems = args.msg.Chest.Items.Take(45);
        var orderLookup = args.currentState.BazaarOffers.ToDictionary(OrderKey, o => o);
        foreach (var item in bazaarItems)
        {
            if (string.IsNullOrWhiteSpace(item?.Description) || string.IsNullOrWhiteSpace(item.ItemName))
                continue;
            if (item.ItemName.Contains("Go Back"))
                break;

            try
            {
                Offer offer = ParseOffer(item);
                var key = OrderKey(offer);
                if(orderLookup.TryGetValue(key, out var existing))
                {
                    offer.Created = existing.Created;
                    // update customer timestamps
                    foreach (var customer in offer.Customers)
                    {
                        var existingCustomer = existing.Customers.FirstOrDefault(c => c.PlayerName == customer.PlayerName);
                        if(existingCustomer != null)
                        {
                            customer.TimeStamp = existingCustomer.TimeStamp;
                        }
                    }
                }
                else
                {
                    offer.Created = args.msg.ReceivedAt;
                }
                offers.Add(offer);
            }
            catch (Exception e)
            {
                if (args.currentState.PlayerId == null)
                    throw; // for test
                args.GetService<ILogger<BazaarListener>>()
                    .LogError(e, "Error parsing bazaar offer: {0} {chest} {user}", JsonConvert.SerializeObject(item), args.msg.Chest.Name, args.currentState.PlayerId);
            }
        }
        Console.WriteLine($"Found {offers.Count} bazaar offers for {args.currentState.PlayerId}");
        args.currentState.BazaarOffers = offers;
        return Task.CompletedTask;
    }

    public static string OrderKey(Offer o)
    {
        return o.Amount + Regex.Replace(o.ItemName, "(§.)*", "") + o.PricePerUnit;
    }

    private static Offer ParseOffer(Item item)
    {
        var parts = item.Description!.Split("\n");

        var amount = parts.Where(p => p.Contains("amount: §a")).First().Split("amount: §a").Last().Split("§").First();
        var pricePerUnit = parts.Where(p => p.StartsWith("§7Price per unit: §6")).First().Split("§7Price per unit: §6").Last().Split(" coins").First();
        var customers = parts.Where(p => p.StartsWith("§8- §a")).Select(p => new Fill()
        {
            Amount = ParseInt(p.Split("§8- §a").Last().Split("§7x").First()),
            PlayerName = p.Split("§8- §a").Last().Split("§7x").Last().Split("§f §8").First().Trim(),
            TimeStamp = DateTime.Now
        }).ToList();

        var offer = new Offer()
        {
            IsSell = item.ItemName!.StartsWith("§6§lSELL"),
            ItemTag = item.Tag,
            Amount = ParseInt(amount),
            PricePerUnit = double.Parse(pricePerUnit, System.Globalization.CultureInfo.InvariantCulture),
            ItemName = item.ItemName.Substring("§6§lSELL ".Length),
            Created = item.Description.Contains("Expired") ? default : DateTime.Now,
            Customers = customers
        };
        return offer;
    }

    private static int ParseInt(string amount)
    {
        return int.Parse(amount, System.Globalization.NumberStyles.AllowThousands, System.Globalization.CultureInfo.InvariantCulture);
    }
}
