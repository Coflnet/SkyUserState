using System.Threading.Tasks;
using Coflnet.Sky.PlayerState.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Diagnostics;
using Newtonsoft.Json;

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
        var localPresent = new Dictionary<Item,Item>(args.currentState.RecentViews.SelectMany(s => s.Items).GroupBy(e => e, comparer).Select(e => e.First()).ToDictionary(e => e, comparer), comparer);
        var foundLocal = toSearchFor.Select(s => localPresent.GetValueOrDefault(s)).Where(s => s != null).ToList();
        var toSearchInDb = toSearchFor.Except(foundLocal, comparer).ToList();
        var itemsWithIds = toSearchInDb.Count > 0 ? await service.FindOrCreate(toSearchInDb) : new List<Item>();
        foreach (var item in localPresent.Keys)
        {
            if(item.Tag != "BOOSTER_COOKIE")
                continue;
            Console.WriteLine($"found local: {JsonConvert.SerializeObject(item)}");
        }
        Console.WriteLine("to search: " + toSearchFor.Count + " found local: " + foundLocal.Count + " from db: " + itemsWithIds.Count + " present: " + localPresent.Count);
        Activity.Current?.AddTag("to search", toSearchFor.Count.ToString());
        Activity.Current?.AddTag("found local", foundLocal.Count.ToString());
        Activity.Current?.AddTag("from db", itemsWithIds.Count.ToString());
        Activity.Current?.AddTag("present", localPresent.Count.ToString());
        Activity.Current?.AddTag("chest", chestName);
        foreach (var item in itemsWithIds)
        {
            if(item.Tag != "BOOSTER_COOKIE")
                continue;
            Console.WriteLine($"from db: {JsonConvert.SerializeObject(item)}");
        }
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
            var inMogo = stored.Where(m => comparer.Equals(item, m)).Where(m => m.Id != null).FirstOrDefault();
            if (inMogo != null)
            {
                item.Id = inMogo.Id;
            }
            yield return item;
        }
    }
}
