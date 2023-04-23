using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Coflnet.Sky.PlayerState.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Collections;
using Coflnet.Sky.PlayerState.Services;
using System.Dynamic;
using System.Collections.Generic;

namespace Coflnet.Sky.PlayerState.Controllers
{
    /// <summary>
    /// Main Controller handling tracking
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class PlayerStateController : ControllerBase
    {
        private readonly IPersistenceService service;

        /// <summary>
        /// Creates a new instance of <see cref="PlayerStateController"/>
        /// </summary>
        /// <param name="service"></param>
        public PlayerStateController(IPersistenceService service)
        {
            this.service = service;
        }

        /// <summary>
        /// Retrieves bazaar order state
        /// </summary>
        /// <param name="playerId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("{playerId}/bazaar")]
        public async Task<List<Bazaar.Offer>> TrackFlip(string playerId)
        {
            var data = await service.GetStateObject(playerId);
            return data.BazaarOffers;
        }

        [HttpGet]
        [Route("{playerId}/lastChest")]
        public async Task<List<Coflnet.Sky.PlayerState.Models.Item>> GetInventory(string playerId)
        {
            var data = await service.GetStateObject(playerId);
            return data.RecentViews.Last().Items;
        }
    }
}
