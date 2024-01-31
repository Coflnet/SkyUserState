using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Coflnet.Sky.PlayerState.Services;

public class BoosterCookieExtractor : UpdateListener
{
    public override Task Process(UpdateArgs args)
    {
        if (args.msg.Chest.Name != "SkyBlock Menu")
            return Task.CompletedTask;
        var state = args.currentState;
        var items = args.msg.Chest.Items;
        var time = default(DateTime);
        foreach (var item in items)
        {
            if (item.ItemName == "§6Booster Cookie")
            {
                var matchGroup = Regex.Match(item.Description, @"§7Duration: §a(\d+d \d+h \d+m \d+s)");
                if (matchGroup.Success)
                {
                    var withoutLetters = Regex.Replace(matchGroup.Groups[1].Value.Replace(" ", ":"), "[a-z]", "");
                    time = DateTime.UtcNow + TimeSpan.Parse(withoutLetters);
                    state.ExtractedInfo.BoosterCookieExpires = time;
                }
            }
        }
        return Task.CompletedTask;
    }
}
