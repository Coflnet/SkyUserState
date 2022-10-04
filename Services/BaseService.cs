using System.Threading.Tasks;
using Coflnet.Sky.PlayerState.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;

namespace Coflnet.Sky.PlayerState.Services
{
    public class PlayerStateService
    {


        public async Task<Flip> AddFlip(Flip flip)
        {

            return flip;
        }
    }

#nullable enable
    public class ItemsService
    {
        private readonly IMongoCollection<StoredItem> _booksCollection;
        private static StoredCompare compare = new();
        private Prometheus.Counter insertCount = Prometheus.Metrics.CreateCounter("sky_playerstate_mongo_insert", "How many items were inserted");
        private Prometheus.Counter selectCount = Prometheus.Metrics.CreateCounter("sky_playerstate_mongo_select", "How many items were selected");

        public ItemsService(
            IOptions<MongoSettings> bookStoreDatabaseSettings, MongoClient mongoClient)
        {
            var mongoDatabase = mongoClient.GetDatabase(
                bookStoreDatabaseSettings.Value.DatabaseName);

            _booksCollection = mongoDatabase.GetCollection<StoredItem>(
                bookStoreDatabaseSettings.Value.ItemsCollectionName);
        }

        public async Task<List<Item>> GetAsync() =>
            (await _booksCollection.Find(_ => true).ToListAsync()).Select(a => a.ToTransfer()).ToList();

        public async Task<Item?> GetAsync(long id) =>
            (await _booksCollection.Find(x => x.Id == id).FirstOrDefaultAsync()).ToTransfer();

        public async Task CreateAsync(Item newItem) =>
            await _booksCollection.InsertOneAsync(new StoredItem(newItem));

        public async Task UpdateAsync(long id, Item updatedItem) =>
            await _booksCollection.ReplaceOneAsync(x => x.Id == id, new StoredItem(updatedItem));

        public async Task RemoveAsync(long id) =>
            await _booksCollection.DeleteOneAsync(x => x.Id == id);

        public async Task<List<Item>> FindOrCreate(IEnumerable<Item> original)
        {
            var batch = original.Select(o => new StoredItem(o)).ToList();
            var builder = Builders<StoredItem>.Filter;
            var filter = builder.And(
                builder.In(e => e.ExtraAttributes, batch.Select(e => e.ExtraAttributes)),
                builder.In(e => e.Enchantments, batch.Select(e => e.Enchantments)),
                builder.In(e => e.Tag, batch.Select(e => e.Tag))
                //builder.In(e => e.ItemName, batch.Select(e => e.ItemName)) the name sometimes changes depending on the inventory, we ignore this
                );


            var query = await _booksCollection.FindAsync(filter);
            var res = await query.ToListAsync();


            var found = new List<StoredItem>();
            foreach (var item in batch)
            {
                var match = res.Where(r => compare.Equals(r, item)).FirstOrDefault();
                if (match != null)
                {
                    found.Add(match);
                    selectCount.Inc();
                }
            }

            var toCreate = batch.Except(found, compare).ToList();
            await InsertBatch(toCreate);

            return found.Concat(toCreate).Select(s => s.ToTransfer()).ToList();

        }

        private async Task InsertBatch(List<StoredItem> toCreate)
        {
            if (toCreate.Count == 0)
                return;
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    foreach (var item in toCreate)
                    {
                        Console.WriteLine("creating item " + item.ItemName + item.Tag + JsonConvert.SerializeObject(item.ExtraAttrib));
                        item.Id = ThreadSaveIdGenerator.NextId;
                    }
                    await _booksCollection.InsertManyAsync(toCreate);
                    insertCount.Inc(toCreate.Count);
                    return;
                }
                catch (System.Exception e)
                {
                    Console.WriteLine(e);
                    await Task.Delay(Random.Shared.Next(0, 100));
                }
            }
        }
    }
}