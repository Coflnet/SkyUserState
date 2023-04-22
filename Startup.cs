using System;
using System.IO;
using System.Reflection;
using Coflnet.Sky.PlayerState.Models;
using Coflnet.Sky.PlayerState.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Prometheus;
using MongoDB.Driver;
using Coflnet.Sky.EventBroker.Client.Api;
using Coflnet.Sky.PlayerName.Client.Api;

namespace Coflnet.Sky.PlayerState
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers().AddNewtonsoftJson();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "SkyPlayerState", Version = "v1" });
                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });

            // Replace with your server version and type.
            // Use 'MariaDbServerVersion' for MariaDB.
            // Alternatively, use 'ServerVersion.AutoDetect(connectionString)'.
            // For common usages, see pull request #1233.
            //var serverVersion = new MariaDbServerVersion(new Version(Configuration["MARIADB_VERSION"]));

            // Replace 'YourDbContext' with the name of your own DbContext derived class.
            /*services.AddDbContext<PlayerStateDbContext>(
                dbContextOptions => dbContextOptions
                    .UseMySql(Configuration["DB_CONNECTION"], serverVersion)
                    .EnableSensitiveDataLogging() // <-- These two calls are optional but help
                    .EnableDetailedErrors()       // <-- with debugging (remove for production).
            );*/
            services.AddSingleton(a => new MongoClient(
                Configuration["Mongo:ConnectionString"]
            ));
            services.AddHostedService<PlayerStateBackgroundService>();
            //services.AddJaeger();
            services.AddTransient<PlayerStateService>();
            services.AddResponseCaching();
            services.AddResponseCompression();

            services.Configure<MongoSettings>(Configuration.GetSection("Mongo"));
            services.AddSingleton<ItemsService>();
            services.AddSingleton<CoinParser>();
            services.AddSingleton<IPersistenceService, PersistenceService>();
            services.AddSingleton<ITransactionService, TransactionService>();
            services.AddSingleton<ICassandraService>(di=>di.GetRequiredService<ITransactionService>() as ICassandraService);
            services.AddSingleton<IMessageApi>(sp => new MessageApi(Configuration["EVENTS_BASE_URL"]));
            services.AddSingleton<IPlayerNameApi>(sp => new PlayerNameApi(Configuration["PLAYERNAME_BASE_URL"]));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "SkyPlayerState v1");
                c.RoutePrefix = "api";
            });

            app.UseResponseCaching();
            app.UseResponseCompression();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapMetrics();
                endpoints.MapControllers();
            });
        }
    }
}
