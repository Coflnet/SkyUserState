using System.Threading.Tasks;
using Coflnet.Sky.PlayerState.Models;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using Microsoft.Extensions.Options;
using System.Collections.Generic;

namespace Coflnet.Sky.PlayerState.Services
{
    public class PlayerStateService
    {
        private PlayerStateDbContext db;

        public PlayerStateService(PlayerStateDbContext db)
        {
            this.db = db;
        }

        public async Task<Flip> AddFlip(Flip flip)
        {
            if (flip.Timestamp == default)
            {
                flip.Timestamp = DateTime.Now;
            }
            db.Flips.Add(flip);
            await db.SaveChangesAsync();
            return flip;
        }
    }

#nullable enable
    public class ItemsService
    {
        private readonly IMongoCollection<Item> _booksCollection;

        public ItemsService(
            IOptions<MongoSettings> bookStoreDatabaseSettings)
        {
            var mongoClient = new MongoClient(
                bookStoreDatabaseSettings.Value.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(
                bookStoreDatabaseSettings.Value.DatabaseName);

            _booksCollection = mongoDatabase.GetCollection<Item>(
                bookStoreDatabaseSettings.Value.ItemsCollectionName);
        }

        public async Task<List<Item>> GetAsync() =>
            await _booksCollection.Find(_ => true).ToListAsync();

        public async Task<Item?> GetAsync(string id) =>
            await _booksCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

        public async Task CreateAsync(Item newItem) =>
            await _booksCollection.InsertOneAsync(newItem);

        public async Task UpdateAsync(string id, Item updatedItem) =>
            await _booksCollection.ReplaceOneAsync(x => x.Id == id, updatedItem);

        public async Task RemoveAsync(string id) =>
            await _booksCollection.DeleteOneAsync(x => x.Id == id);
    }
}
