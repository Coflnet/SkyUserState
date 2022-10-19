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
        /// Tracks a flip
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
            if(querSize > TimeSpan.FromDays(30))
                return null;
            return await service.GetTransactions(playerUuid, querSize, end);
        }
    }
}
