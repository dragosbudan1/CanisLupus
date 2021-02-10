using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using NLog;

namespace CanisLupus.Worker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger logger;
        private readonly IMarketMakerHandler marketMakerHandler;

        public Worker(IMarketMakerHandler marketMakerHandler)
        {
            this.logger = LogManager.GetCurrentClassLogger();
            this.marketMakerHandler = marketMakerHandler;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    logger.Info("Worker running at: {time}", DateTimeOffset.Now);

                    await marketMakerHandler.ExecuteAsync(stoppingToken);
                    
                }
                catch (System.Exception ex)
                {

                    logger.Error(ex, ex.Message);
                }

                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            }
        }
    }
}
