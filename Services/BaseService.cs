using System.Threading.Tasks;
using Coflnet.Sky.Base.Models;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Coflnet.Sky.Base.Services
{
    public class BaseService
    {
        private BaseDbContext db;

        public BaseService(BaseDbContext db)
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
}
