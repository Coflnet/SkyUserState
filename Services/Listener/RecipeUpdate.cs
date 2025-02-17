using System.Collections.Generic;
using System.IO;
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
        if (!(args.msg.Chest?.Name?.StartsWith("Museum - disabled") ?? false))
            return;
        Console.WriteLine($"Museum update {args.msg.Chest?.Name} {JsonConvert.SerializeObject(args.msg.Chest?.Items.Take(36))}");
        ExtractMuseumExp(args);
    }

    private static void ExtractMuseumExp(UpdateArgs args)
    {
        if (!(args.msg.Chest?.Name?.Contains("Museum") ?? false))
            return;
        
        var existing = new Dictionary<string, int>();
        if (File.Exists("museum.json"))
        {
            existing = JsonConvert.DeserializeObject<Dictionary<string, int>>(File.ReadAllText("museum.json"));
        }
        foreach (var item in args.msg.Chest.Items.Skip(9).Take(36))
        {
            if (item.Description == null || !item.Description.Contains("SkyBlock XP"))
                continue;
            var name = item.ItemName;
            // extract the exp from "§7Click on this item in your inventory to\n§7add it to your §9Museum§7!\n\n§7Reward: §b+5 SkyBlock XP"
            var exp = Regex.Match(item.Description, @"§7Reward: §b\+(\d+) SkyBlock XP").Groups[1].Value;
            Console.WriteLine($"Museum update {name} {exp}");
            if (exp == "")
                continue;
            existing[name] = int.Parse(exp);
        }
        File.WriteAllText("museum.json", JsonConvert.SerializeObject(existing, Formatting.Indented));
    }
    private static void ExtractCarpentryCost(UpdateArgs args)
    {
        if (true || !(args.msg.Chest?.Name?.Contains("Carpentry B") ?? false))
        {
            return;
        }
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

    public class Cost
    {
        public string Item { get; set; }
        public int Count { get; set; }
    }
}