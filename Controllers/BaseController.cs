using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Coflnet.Sky.PlayerState.Models;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Collections;
using System.Collections.Generic;
using Coflnet.Sky.PlayerState.Services;
using MongoDB.Bson;
using Newtonsoft.Json;
using System.Dynamic;

namespace Coflnet.Sky.PlayerState.Controllers
{
    /// <summary>
    /// Main Controller handling tracking
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class PlayerStateController : ControllerBase
    {
        private readonly PlayerStateService service;

        /// <summary>
        /// Creates a new instance of <see cref="PlayerStateController"/>
        /// </summary>
        /// <param name="service"></param>
        public PlayerStateController(PlayerStateService service)
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

    [ApiController]
    [Route("api/items")]
    public class ItemsController : ControllerBase
    {
        private readonly ItemsService _booksService;

        public ItemsController(ItemsService booksService) =>
            _booksService = booksService;

        [HttpPost]
        [Route("mock")]
        public async Task<IActionResult> Create()
        {
            var sourceData = "{\"rarity_upgrades\":1,\"gems\":{\"unlocked_slots\":[\"AMBER_0\",\"AMBER_1\",\"JADE_0\",\"JADE_1\",\"TOPAZ_0\"]},\"uid\":\"d8196ed3fcfa\"}";
            var data = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(sourceData);
            Item newItem = new Item()
            {
                Enchantments = new Dictionary<string, byte>() { { "sharpness", 1 } },
                ExtraAttributes = data// BsonDocument.Parse(sourceData) //new() { { "exp", 5 }, { "attr", new List<string>() { "kk", "bb" }.ToArray() } }
            };
            
            Console.WriteLine(data.ToBsonDocument().ToJson());
            Console.WriteLine(JsonConvert.SerializeObject(newItem.ExtraAttributes));
            await _booksService.CreateAsync(newItem);
            await _booksService.CreateAsync(newItem);
            await _booksService.CreateAsync(newItem);
            await _booksService.CreateAsync(newItem);

            return CreatedAtAction(nameof(Get), new
            {
                id = newItem.Id
            });
        }

        [HttpGet]
        public async Task<List<Item>> Get() =>
            await _booksService.GetAsync();

        [HttpGet("{id:length(24)}")]
        public async Task<ActionResult<Item>> Get(long id)
        {
            var book = await _booksService.GetAsync(id);

            if (book is null)
            {
                return NotFound();
            }

            return book;
        }

        [HttpPost]
        public async Task<IActionResult> Post(Item newItem)
        {
            await _booksService.CreateAsync(newItem);

            return CreatedAtAction(nameof(Get), new { id = newItem.Id }, newItem);
        }

        [HttpPut("{id:length(24)}")]
        public async Task<IActionResult> Update(long id, Item updatedItem)
        {
            var book = await _booksService.GetAsync(id);

            if (book is null)
            {
                return NotFound();
            }

            updatedItem.Id = book.Id;

            await _booksService.UpdateAsync(id, updatedItem);

            return NoContent();
        }

        [HttpDelete("{id:length(24)}")]
        public async Task<IActionResult> Delete(long id)
        {
            var book = await _booksService.GetAsync(id);

            if (book is null)
            {
                return NotFound();
            }

            await _booksService.RemoveAsync(id);

            return NoContent();
        }
    }
}
