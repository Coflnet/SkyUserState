using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Coflnet.Sky.PlayerState.Models;
using System;
using System.Collections.Generic;

namespace Coflnet.Sky.PlayerState.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionService service;

        /// <summary>
        /// Creates a new instance of <see cref="PlayerStateController"/>
        /// </summary>
        /// <param name="service"></param>
        public TransactionController(ITransactionService service)
        {
            this.service = service;
        }

        /// <summary>
        /// Returns the transactions of a player
        /// </summary>
        /// <param name="playerUuid"></param>
        /// <param name="end"></param>
        /// <param name="seconds"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("player/{playerUuid}")]
        public async Task<IEnumerable<Transaction>> TrackFlip(Guid playerUuid, int seconds, DateTime end = default)
        {
            if (end == default)
                end = DateTime.UtcNow;
            var querSize = TimeSpan.FromSeconds(seconds);
            if (querSize > TimeSpan.FromDays(30))
                return null;
            return await service.GetTransactions(playerUuid, querSize, end);
        }

        /// <summary>
        /// Returns the transactions of an item
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("item/{itemId}")]
        public async Task<IEnumerable<Transaction>> TrackItem(long itemId, int max = 100)
        {
            return await service.GetItemTransactions(itemId, max);
        }

        /// <summary>
        /// Returns the (recent traded) item ids for one uuid
        /// </summary>
        /// <param name="uuid"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("uuid/itemId/{uuid}")]
        public async Task<IEnumerable<long>> GetItemId(Guid uuid)
        {
            return await service.GetItemIdsFromUuid(uuid);
        }

        /// <summary>
        /// Returns the (recent traded) item ids for multiple uuids
        /// </summary>
        /// <param name="uuids"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("uuid/itemId")]
        public async Task<Dictionary<Guid, long[]>> GetItemId([FromBody] List<Guid> uuids)
        {
            return await service.GetItemIdsFromUuids(uuids);
        }
    }
}
