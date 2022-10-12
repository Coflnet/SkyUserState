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
        args.currentState.Inventory = args.msg.Chest.Items.Reverse<Item>().Take(36).Reverse().ToList();
    }
}
