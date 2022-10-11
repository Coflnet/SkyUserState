using System.Threading.Tasks;
using Coflnet.Sky.PlayerState.Models;
using System.Linq;
using System;

namespace Coflnet.Sky.PlayerState.Services;

public class InventoryChangeUpdate : UpdateListener
{
    /// <inheritdoc/>
    public override async Task Process(UpdateArgs args)
    {
        Console.WriteLine("updated now there is " + args.msg.Chest.Items.Where(i => i != null && string.IsNullOrWhiteSpace(i.ItemName)).FirstOrDefault()?.ItemName);
        //Console.WriteLine(args.msg.Chest.Name + "\n" + JsonConvert.SerializeObject(args.msg.Chest.Items));
        args.currentState.Inventory = args.msg.Chest.Items.Reverse<Item>().Take(36).Reverse().ToList();
    }
}
