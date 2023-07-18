using System.Threading.Tasks;
using Coflnet.Sky.PlayerState.Models;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using Moq;
using Newtonsoft.Json;

namespace Coflnet.Sky.PlayerState.Services;

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