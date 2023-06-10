using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Coflnet.Sky.Items.Client.Api;
using Coflnet.Sky.PlayerState.Models;
using Coflnet.Sky.PlayerState.Services;
using Moq;
using NUnit.Framework;

namespace Coflnet.Sky.PlayerState.Bazaar;

public class BazaarOrderTests
{
    Mock<ITransactionService> transactionService = null!;
    BazaarOrderListener listener = new BazaarOrderListener();
    StateObject currentState = null!;
    int invokeCount = 0;
    [SetUp]
    public void Setup()
    {
        transactionService = new Mock<ITransactionService>();
        transactionService.Setup(t => t.AddTransactions(It.IsAny<Transaction>()))
            .Callback(() =>
            {
                invokeCount++;
            });
        currentState = new();
        invokeCount = 0;
    }
    [Test]
    public async Task BuyOrderCreateAndFill()
    {
        UpdateArgs args = CreateArgs("[Bazaar] Submitting buy order...",
                    "[Bazaar] Buy Order Setup! 64x Coal for 134.4 coins");
        await listener.Process(args);

        // coins are locked up
        transactionService.Verify(t => t.AddTransactions(It.Is<Transaction>(t =>
            t.Type == Transaction.TransactionType.BazaarListSell
            && t.Amount == 1344
            && t.ItemId == TradeDetect.IdForCoins
            )
        ), Times.Once);
        Assert.AreEqual(1, currentState.BazaarOffers.Count);
        Assert.AreEqual(64, currentState.BazaarOffers[0].Amount);
        Assert.AreEqual(1344 / 64, currentState.BazaarOffers[0].PricePerUnit);

        return;
        await listener.Process(CreateArgs("[Bazaar] Your Buy Order for 64x Coal was filled!"));
        AssertCoalBuy();
        Assert.AreEqual(1, currentState.BazaarOffers.Count);
        Assert.AreEqual(3, invokeCount);
    }

    private void AssertCoalBuy()
    {
        // coins exchanged to item
        transactionService.Verify(t => t.AddTransactions(It.Is<Transaction>(t =>
                        t.Type == (Transaction.TransactionType.BAZAAR | Transaction.TransactionType.RECEIVE)
                        && t.Amount == 64
                        && t.ItemId == 5
                        )
                    ), Times.Once);
        transactionService.Verify(t => t.AddTransactions(It.Is<Transaction>(t =>
            t.Type == (Transaction.TransactionType.BAZAAR | Transaction.TransactionType.REMOVE)
            && t.Amount == 1344
            && t.ItemId == TradeDetect.IdForCoins
            )
        ), Times.Once);
    }
    [TestCase("[Bazaar] Buy Order Setup! 1x Ultimate Wise V for 3,570,083 coins.")]
    public async Task RunParse(string line)
    {
        await listener.Process(CreateArgs(line));
    }
    [Test]
    public async Task SellOrderCreateAndFill()
    {
        UpdateArgs args = CreateArgs("[Bazaar] Submitting sell order...",
                    "[Bazaar] Sell Offer Setup! 64x Coal for 303.7 coins.");
        await listener.Process(args);

        var expectedType = Transaction.TransactionType.BAZAAR | Transaction.TransactionType.Move;
        transactionService.Verify(t => t.AddTransactions(It.Is<Transaction>(t =>
                        t.Type == Transaction.TransactionType.BazaarListSell
                        && t.Amount == 64
                        && t.ItemId == 5
                        )
                    ), Times.Once);

        await listener.Process(CreateArgs("[Bazaar] Your Sell Offer for 64x Coal was filled!"));

        return;
        AssertCoalSell();
        Assert.AreEqual(3, invokeCount);
    }

    private void AssertCoalSell()
    {
        transactionService.Verify(t => t.AddTransactions(It.Is<Transaction>(t =>
                                t.Type == (Transaction.TransactionType.BAZAAR | Transaction.TransactionType.REMOVE)
                                && t.Amount == 64
                                && t.ItemId == 5
                                )
                            ), Times.Once);
        transactionService.Verify(t => t.AddTransactions(It.Is<Transaction>(t =>
            t.Type == (Transaction.TransactionType.BAZAAR | Transaction.TransactionType.RECEIVE)
            && t.Amount == 3037
            && t.ItemId == TradeDetect.IdForCoins
            )
        ), Times.Once);
    }

    [Test]
    public async Task ClaimSellWithNoFill()
    {
        currentState.BazaarOffers.Add(new Offer()
        {
            Amount = 64,
            ItemName = "Coal",
            PricePerUnit = 4.8,
            IsSell = true,
            Created = DateTime.Now - TimeSpan.FromHours(1),
        });
        await listener.Process(CreateArgs("[Bazaar] Claiming order...",
                "[Bazaar] Claimed 303.7 coins from selling 64x Coal at 4.8 each!"));

        transactionService.Verify(t => t.AddTransactions(It.Is<Transaction>(t =>
            t.Type == (Transaction.TransactionType.BAZAAR | Transaction.TransactionType.RECEIVE | Transaction.TransactionType.Move)
            && t.Amount == 3037
            && t.ItemId == TradeDetect.IdForCoins
            )
        ), Times.Once);
        AssertCoalSell();
        Assert.AreEqual(3, invokeCount);
    }
    [Test]
    public async Task InstaBuy()
    {
        await listener.Process(CreateArgs("[Bazaar] Executing instant buy...",
                "[Bazaar] Bought 1,280x Coal for 5,120 coins!"));
        transactionService.Verify(t => t.AddTransactions(It.Is<Transaction>(t =>
                    t.Type == (Transaction.TransactionType.BAZAAR | Transaction.TransactionType.REMOVE)
                    && t.Amount == 51200
                    && t.ItemId == TradeDetect.IdForCoins
                    )
                ), Times.Once);
        transactionService.Verify(t => t.AddTransactions(It.Is<Transaction>(t =>
                    t.Type == (Transaction.TransactionType.BAZAAR | Transaction.TransactionType.RECEIVE)
                    && t.Amount == 1280
                    && t.ItemId == 5
                    )
                ), Times.Once);
        Assert.AreEqual(0, currentState.BazaarOffers.Count);
        Assert.AreEqual(2, invokeCount);
    }
    [Test]
    public async Task InstaSell()
    {
        await listener.Process(CreateArgs("[Bazaar] Executing instant sell...",
            "[Bazaar] Sold 1,280x Coal for 3,840 coins!"));
        transactionService.Verify(t => t.AddTransactions(It.Is<Transaction>(t =>
                    t.Type == (Transaction.TransactionType.BAZAAR | Transaction.TransactionType.REMOVE)
                    && t.Amount == 1280
                    && t.ItemId == 5
                    )
                ), Times.Once);
        transactionService.Verify(t => t.AddTransactions(It.Is<Transaction>(t =>
                    t.Type == (Transaction.TransactionType.BAZAAR | Transaction.TransactionType.RECEIVE)
                    && t.Amount == 38400
                    && t.ItemId == TradeDetect.IdForCoins
                    )
                ), Times.Once);
        Assert.AreEqual(0, currentState.BazaarOffers.Count);
        Assert.AreEqual(2, invokeCount);
    }
    [Test]
    public async Task CancelOrder()
    {
        currentState.BazaarOffers.Add(new Offer()
        {
            Amount = 926,
            ItemName = "Enchanted End Stone",
            PricePerUnit = 303.7,
            IsSell = true,
            Created = DateTime.Now - TimeSpan.FromHours(1),
        });
        await listener.Process(CreateArgs("[Bazaar] Cancelling order...",
                "[Bazaar] Cancelled! Refunded 926x Enchanted End Stone from cancelling Sell Offer!"));
        transactionService.Verify(t => t.AddTransactions(It.Is<Transaction>(t =>
                    t.Type == (Transaction.TransactionType.BAZAAR | Transaction.TransactionType.Move | Transaction.TransactionType.RECEIVE)
                    && t.Amount == 926
                    && t.ItemId == 5
                    )
                ), Times.Once);
        Assert.AreEqual(0, currentState.BazaarOffers.Count);
        transactionService.VerifyAll();
    }

    private MockedUpdateArgs CreateArgs(params string[] msgs)
    {
        var args = new MockedUpdateArgs()
        {
            currentState = currentState,
            msg = new UpdateMessage()
            {
                ChatBatch = msgs.ToList()
            }
        };
        var itemsApi = new Mock<IItemsApi>();
        itemsApi.Setup(i => i.ItemsSearchTermIdGetAsync(It.IsAny<string>(), 0, default)).ReturnsAsync(5);
        args.AddService<IItemsApi>(itemsApi.Object);
        args.AddService<ITransactionService>(transactionService.Object);

        return args;
    }

    private class MockedUpdateArgs : UpdateArgs
    {
        private Dictionary<Type, object> services = new();
        public override T GetService<T>()
        {
            if (services.ContainsKey(typeof(T)))
                return (T)services[typeof(T)];
            return base.GetService<T>();
        }

        public void AddService<T>(T service)
        {
            services.Add(typeof(T), service);
        }
    }
}