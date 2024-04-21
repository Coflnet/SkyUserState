using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Coflnet.Sky.PlayerState.Services;

public class RecipeUpdate : UpdateListener
{
    /// <inheritdoc/>
    public override async Task Process(UpdateArgs args)
    {
        if (!(args.msg.Chest?.Name?.Contains("Recipe") ?? false))
            return;
        Console.WriteLine($"Recipe update {args.msg.Chest?.Name} {JsonConvert.SerializeObject(args.msg.Chest?.Items.Take(36))}");
    }
}