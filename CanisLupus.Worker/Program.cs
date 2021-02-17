using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CanisLupus.Common.Database;
using CanisLupus.Worker.Account;
using CanisLupus.Worker.Algorithms;
using CanisLupus.Worker.Events;
using CanisLupus.Worker.Exchange;
using CanisLupus.Worker.Infrastructure;
using CanisLupus.Worker.Trader;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CanisLupus.Worker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>();

                    services.AddTransient<IBinanceClient, BinanceClient>()
                            .AddTransient<IMarketMakerHandler, MarketMakerHandler>()
                            .AddTransient<IEventPublisher, EventPublisher>()
                            .AddTransient<IClusterGenerator, ClusterGenerator>()
                            .AddTransient<IWeightedMovingAverageCalculator, WeightedMovingAverageCalculator>()
                            .AddTransient<IIntersectionClient, IntersectionClient>()
                            .AddTransient<ITradingClient, TradingClient>()
                            .AddTransient<IDbClient, MongoDbClient>()
                            .AddTransient<IOrderClient, OrderClient>()
                            .AddTransient<ITradingSettingsClient, TradingSettingsClient>()
                            .AddTransient<IWalletClient, WalletClient>();

                    services.Configure<DbSettings>(hostContext.Configuration.GetSection("DbSettings"))
                            .Configure<BinanceSettings>(hostContext.Configuration.GetSection("BinanceSettings"));
                });
    }
}
