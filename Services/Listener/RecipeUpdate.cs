using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Coflnet.Sky.PlayerState.Services;

public class RecipeUpdate : UpdateListener
{
    /// <inheritdoc/>
    public override async Task Process(UpdateArgs args)
    {
        if (false && (args.msg.Chest?.Name?.Contains("Carpentry B") ?? false))
        {
            var result = new Dictionary<string, List<Cost>>();
            // sample: §8Furniture\n\n§7Opens the Anvil menu!\n\n§f§lCOMMON\n§8§m-----------------\n§7Cost\n§fDark Oak Wood Plank §8x20\n§fOak Wood Plank §8x3\n§fStone §8x5\n§fStick\n§fAnvil\n§fIron Shovel\n§fIron Pickaxe\n§aDiamond Chestplate\n\n§cRequires Carpentry Skill XXIII!
            foreach (var item in args.msg.Chest.Items.Take(45))
            {
                if (item.Tag == null)
                    continue;
                var tag = item.Tag;
                var costs = item.Description.Split("Cost")[1].Split("\n\n")[0].Split("\n").Skip(1)
                    .Select(x =>
                    {
                        var match = Regex.Match(x, @"§.([^§]*)(§.x(\d+)|)$");
                        if (!match.Success)
                            return new();
                        if (!int.TryParse(match.Groups[3].Value, out var count))
                            count = 1;
                        var tag = args.GetService<Coflnet.Sky.Items.Client.Api.IItemsApi>().ItemsSearchTermGet(match.Groups[1].Value.Trim()).First().Tag;
                        return new Cost() { Item = tag, Count = count };
                    }).Where(x => x.Item != "").ToList();
                result.Add(tag, costs);
            }
            Console.WriteLine($"Recipe update {args.msg.Chest.Name} {JsonConvert.SerializeObject(result, Formatting.Indented)}");
        }
        if (!(args.msg.Chest?.Name?.Contains("Recipe") ?? false))
            return;
        Console.WriteLine($"Recipe update {args.msg.Chest?.Name} {JsonConvert.SerializeObject(args.msg.Chest?.Items.Take(36))}");
    }

    public class Cost
    {
        public string Item { get; set; }
        public int Count { get; set; }
    }
}