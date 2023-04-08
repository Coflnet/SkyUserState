using System.Threading.Tasks;
using Coflnet.Sky.PlayerState.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Collections.Generic;
using System;

namespace Coflnet.Sky.PlayerState.Services;

public class ItemIdAssignUpdate : UpdateListener
{
    private ItemCompare comparer = new();
    public override async Task Process(UpdateArgs args)
    {

        await args.stateService.ExecuteInScope(async sp =>
        {
            var service = sp.GetRequiredService<ItemsService>();
            var collection = args.msg.Chest.Items;
            var chestName = args.msg.Chest.Name;
            var toSearchFor = collection.Where(i=>HasToBeStoredInMongo(i,chestName)).ToHashSet();
            var localPresent = args.currentState.RecentViews.SelectMany(s => s.Items).GroupBy(e => e, comparer).Select(e => e.First()).ToDictionary(e => e, comparer);
            var foundLocal = toSearchFor.Select(s => localPresent.Values.Where(b => comparer.Equals(b, s)).FirstOrDefault()).Where(s => s != null).ToList();
            var itemsWithIds = await service.FindOrCreate(toSearchFor.Except(foundLocal, comparer));

            Console.WriteLine("to search: " + toSearchFor.Count + " found local: " + foundLocal.Count + " from db: " + itemsWithIds.Count + " present: " + localPresent.Count);
            args.msg.Chest.Items = Join(collection, itemsWithIds.Concat(foundLocal)).ToList();
        });
    }

    private static bool HasToBeStoredInMongo(Item i, string chestName)
    {
        return i.ExtraAttributes != null && i.ExtraAttributes.Count != 0 && i.Enchantments?.Count != 0 && !IsNpcSell(i) && !IsBazaar(chestName);
    }

    private static bool IsBazaar(string chestName)
    {
        return chestName.Contains("➜");
    }

    private static bool IsNpcSell(Item i)
    {
        // Another valid indicator would be "Click to trade!"
        return i.Description?.Contains("§7Cost\n") ?? false;
    }

    private IEnumerable<Item> Join(IEnumerable<Item> original, IEnumerable<Item> mongo)
    {
        var mcount = 0;
        foreach (var item in original)
        {
            var inMogo = mongo.Where(m => comparer.Equals(item, m)).FirstOrDefault();
            if (inMogo != null)
            {
                yield return inMogo;
                mcount++;
            }
            else
                yield return item;
        }
    }
}
