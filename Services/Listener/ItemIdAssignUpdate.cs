using System.Threading.Tasks;
using Coflnet.Sky.PlayerState.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Collections.Generic;
using System;
using NUnit.Framework;
using Moq;
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
        var localPresent = args.currentState.RecentViews.SelectMany(s => s.Items).GroupBy(e => e, comparer).Select(e => e.First()).ToDictionary(e => e, comparer);
        var foundLocal = toSearchFor.Select(s => localPresent.Values.Where(b => comparer.Equals(b, s)).FirstOrDefault()).Where(s => s != null).ToList();
        var toSearchInDb = toSearchFor.Except(foundLocal, comparer);
        var itemsWithIds = await service.FindOrCreate(toSearchInDb);

        Console.WriteLine("to search: " + toSearchFor.Count + " found local: " + foundLocal.Count + " from db: " + itemsWithIds.Count + " present: " + localPresent.Count);
        args.msg.Chest.Items = Join(collection, itemsWithIds.Concat(foundLocal)).ToList();
    }

    private static bool CanGetAnIdByStoring(Item i, string chestName)
    {
        // one extra attribute is the tier
        return (i.ExtraAttributes != null && i.ExtraAttributes.Count > 1 || i.Enchantments?.Count != 0) && !IsNpcSell(i) && !IsBazaar(chestName);
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
        var mcount = 0;
        foreach (var item in original)
        {
            var inMogo = stored.Where(m => comparer.Equals(item, m)).FirstOrDefault();
            if (inMogo != null)
            {
                item.Id = inMogo.Id;
                mcount++;
            }
            yield return item;
        }
    }
}

public class ItemIdAssignUpdateTest
{
    private StateObject currentState = new();
    private Mock<IItemsService> itemsService;
    private List<Item> calledWith;
    [Test]
    public async Task HigherEnchantIsNew()
    {
        var listener = new ItemIdAssignUpdate();

        await listener.Process(CreateArgs(new Item()
        {
            ItemName = "Lapis Helmet",
            Enchantments = new Dictionary<string, byte>() { { "protection", 2 } },
        }));
        Assert.IsNotNull(calledWith);
        Assert.AreEqual(1, calledWith.Count, JsonConvert.SerializeObject(calledWith));
        itemsService.Verify(s => s.FindOrCreate(It.Is<IEnumerable<Item>>(i => i.Count() == 1)), Times.Once);
    }

    private MockedUpdateArgs CreateArgs(params Item[] items)
    {
        currentState.RecentViews.Enqueue(new()
        {
            Items = new List<Item>(){
                new Item()
                    {
                        ItemName = "Lapis Helmet",
                        Enchantments = new Dictionary<string, byte>() { { "protection", 1 } },
                    }
            }
        });
        var args = new MockedUpdateArgs()
        {
            currentState = currentState,
            msg = new UpdateMessage()
            {
                Chest = new()
                {
                    Items = items.ToList()
                }
            }
        };
        // args.AddService<ITransactionService>(transactionService.Object);
        itemsService = new Mock<IItemsService>();
        itemsService.Setup(s => s.FindOrCreate(It.IsAny<IEnumerable<Item>>())).Callback<IEnumerable<Item>>((v) =>
        {
            calledWith = v.ToList();
        }).ReturnsAsync(items.ToList());
        args.AddService<IItemsService>(itemsService.Object);

        return args;
    }

    private class MockedUpdateArgs : UpdateArgs
    {
        private Dictionary<Type, object> services = new();
        public override T GetService<T>()
        {
            if (services.ContainsKey(typeof(T)))
                return (T)services[typeof(T)];
            throw new Exception($"Service {typeof(T)} not found");
        }

        public void AddService<T>(T service)
        {
            services.Add(typeof(T), service);
        }
    }
}