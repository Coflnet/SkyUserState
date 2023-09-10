using System.Threading.Tasks;
using Coflnet.Sky.PlayerState.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Diagnostics;

namespace Coflnet.Sky.PlayerState.Services;

public class ItemIdAssignUpdate : UpdateListener
{
    private ItemCompare comparer = new();
    public override async Task Process(UpdateArgs args)
    {
        var service = args.GetService<IItemsService>();
        var collection = args.msg.Chest.Items;
        var chestName = args.msg.Chest.Name;
        var toSearchFor = collection.Where(i => CanGetAnIdByStoring(i, chestName)).ToHashSet();
        var localPresent = args.currentState.RecentViews.SelectMany(s => s.Items).GroupBy(e => e, comparer).Select(e => e.First()).ToDictionary(e => e, comparer);
        var foundLocal = toSearchFor.Select(s => localPresent.Values.Where(b => comparer.Equals(b, s)).FirstOrDefault()).Where(s => s != null).ToList();
        var toSearchInDb = toSearchFor.Except(foundLocal, comparer);
        var itemsWithIds = await service.FindOrCreate(toSearchInDb);

        Console.WriteLine("to search: " + toSearchFor.Count + " found local: " + foundLocal.Count + " from db: " + itemsWithIds.Count + " present: " + localPresent.Count);
        Activity.Current?.AddTag("to search", toSearchFor.Count.ToString());
        Activity.Current?.AddTag("found local", foundLocal.Count.ToString());
        Activity.Current?.AddTag("from db", itemsWithIds.Count.ToString());
        Activity.Current?.AddTag("present", localPresent.Count.ToString());
        Activity.Current?.AddTag("chest", chestName);
        args.msg.Chest.Items = Join(collection, itemsWithIds.Concat(foundLocal)).ToList();
    }

    private static bool CanGetAnIdByStoring(Item i, string chestName)
    {
        // one extra attribute is the tier
        return (i.ExtraAttributes != null && i.ExtraAttributes.Count > 1) && !IsNpcSell(i) && !IsBazaar(chestName);
    }

    private static bool IsBazaar(string chestName)
    {
        return chestName?.Contains("➜") ?? false;
    }

    private static bool IsNpcSell(Item i)
    {
        // Another valid indicator would be "Click to trade!"
        return i.Description?.Contains("§7Cost\n") ?? false;
    }

    private IEnumerable<Item> Join(IEnumerable<Item> original, IEnumerable<Item> stored)
    {
        foreach (var item in original)
        {
            var inMogo = stored.Where(m => comparer.Equals(item, m)).FirstOrDefault();
            if (inMogo != null)
            {
                item.Id = inMogo.Id;
            }
            yield return item;
        }
    }
}
