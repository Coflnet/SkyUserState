using System.Threading.Tasks;
using Coflnet.Sky.PlayerName.Client.Api;

namespace Coflnet.Sky.PlayerState.Services;

/// <summary>
/// Detects profile and minecraft name/account changes and updates them
/// </summary>
public class ProfileAndNameUpdate : UpdateListener
{
    public override async Task Process(UpdateArgs args)
    {
        var state = args.currentState;
        var nameService = args.GetService<IPlayerNameApi>();
        if (state.McInfo.Uuid == default)
        {
            var uuid = await nameService.PlayerNameUuidNameGetAsync(args.msg.PlayerId);
            if (uuid != null)
                state.McInfo.Uuid = Guid.Parse(uuid.Trim('"'));
        }
        // TODO find profile
    }
}
