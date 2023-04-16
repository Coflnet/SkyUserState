using System.Collections.Generic;
using System.Threading.Tasks;
using Coflnet.Sky.PlayerState.Models;
using Coflnet.Sky.PlayerState.Services;
using NUnit.Framework;

namespace Coflnet.Sky.PlayerState.Bazaar;

public class BazaarListnerTests
{
    [Test]
    public async Task Parse()
    {
        var args = new UpdateArgs()
        {
            currentState = new(),
            msg = new UpdateMessage()
            {
                Chest = new ChestView()
                {
                    Name = "Your Bazaar Orders",
                    Items = new List<Item>(){
                        new Item(){
                            ItemName = "§6§lSELL §aEnchanted Rotten Flesh",
                            Description = """
§Worth 519k coins

§7Offer amount: §a2,640§7x
§7Filled: §6640§7/640 §a§l100%!

§8Expired!

§7Price per unit: §61,820.9 coins

§7Customers:
§8- §a501§7x §b[MVP§2+§b] Terminator602§f §819d ago
§8- §a139§7x §a[VIP§6+§a] Luka_Daddy§f §819d ago

§eYou have §6519,466 coins §eto claim!

§eClick to claim!
""",
Tag = "ROTTEN_FLESH"

                        },
                        new Item(){
                            ItemName="§a§lBUY §aWorm Membrane",
                            Description="§8Worth 10M coins\n\n§7Order amount: §a400§7x\n\n§7Price per unit: §625,130.6 coins\n\n§eClick to view options!"
                        }
                    }
                }
            }
        };
        var listener = new BazaarListener();
        await listener.Process(args);
        Assert.AreEqual(2, args.currentState.BazaarOffers.Count);
        Assert.AreEqual(2640, args.currentState.BazaarOffers[0].Amount);
        Assert.AreEqual(1820.9, args.currentState.BazaarOffers[0].PricePerUnit);
        Assert.AreEqual(2, args.currentState.BazaarOffers[0].Customers.Count);
        Assert.AreEqual(501, args.currentState.BazaarOffers[0].Customers[0].Amount);
        Assert.AreEqual("§b[MVP§2+§b] Terminator602", args.currentState.BazaarOffers[0].Customers[0].PlayerName);
        Assert.AreEqual(139, args.currentState.BazaarOffers[0].Customers[1].Amount);
        Assert.AreEqual("§a[VIP§6+§a] Luka_Daddy", args.currentState.BazaarOffers[0].Customers[1].PlayerName);
        Assert.AreEqual(400, args.currentState.BazaarOffers[1].Amount);
        Assert.AreEqual(25130.6, args.currentState.BazaarOffers[1].PricePerUnit);
    }
}