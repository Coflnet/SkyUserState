using System.Threading;
using System.Threading.Tasks;
using Coflnet.Sky.PlayerState.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using System.Linq;
using Microsoft.Extensions.Logging;
using Coflnet.Sky.PlayerState.Controllers;
using Coflnet.Sky.Core;

namespace Coflnet.Sky.PlayerState.Services
{

    public class PlayerStateBackgroundService : BackgroundService
    {
        private IServiceScopeFactory scopeFactory;
        private IConfiguration config;
        private ILogger<PlayerStateBackgroundService> logger;
        private Prometheus.Counter consumeCount = Prometheus.Metrics.CreateCounter("sky_base_conume", "How many messages were consumed");

        public PlayerStateBackgroundService(
            IServiceScopeFactory scopeFactory, IConfiguration config, ILogger<PlayerStateBackgroundService> logger)
        {
            this.scopeFactory = scopeFactory;
            this.config = config;
            this.logger = logger;
        }
        /// <summary>
        /// Called by asp.net on startup
        /// </summary>
        /// <param name="stoppingToken">is canceled when the applications stops</param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return;
            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PlayerStateDbContext>();
            // make sure all migrations are applied
            await context.Database.MigrateAsync();

            var flipCons = Coflnet.Kafka.KafkaConsumer.ConsumeBatch<LowPricedAuction>(config["KAFKA_HOST"], config["TOPICS:LOW_PRICED"], async batch =>
            {
                var service = GetService();
                foreach (var lp in batch)
                {
                    // do something
                }
                consumeCount.Inc(batch.Count());
            }, stoppingToken, "skybase");

            await Task.WhenAll(flipCons);
        }

        private PlayerStateService GetService()
        {
            return scopeFactory.CreateScope().ServiceProvider.GetRequiredService<PlayerStateService>();
        }
    }
}