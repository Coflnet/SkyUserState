using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Coflnet.Sky.Base.Models;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Collections;
using System.Collections.Generic;
using Coflnet.Sky.Base.Services;

namespace Coflnet.Sky.Base.Controllers
{
    /// <summary>
    /// Main Controller handling tracking
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class BaseController : ControllerBase
    {
        private readonly BaseService service;

        /// <summary>
        /// Creates a new instance of <see cref="BaseController"/>
        /// </summary>
        /// <param name="service"></param>
        public BaseController(BaseService service)
        {
            this.service = service;
        }

        /// <summary>
        /// Tracks a flip
        /// </summary>
        /// <param name="flip"></param>
        /// <param name="AuctionId"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("flip/{AuctionId}")]
        public async Task<Flip> TrackFlip([FromBody] Flip flip, string AuctionId)
        {
            await service.AddFlip(flip);
            return flip;
        }
    }
}
