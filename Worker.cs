using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CanisLupus
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> logger;
        private readonly IMarketMakerHandler marketMakerHandler;

        public Worker(ILogger<Worker> logger,
                      IMarketMakerHandler marketMakerHandler)
        {
            this.logger = logger;
            this.marketMakerHandler = marketMakerHandler;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {            
            while (!stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                await marketMakerHandler.ExecuteAsync(stoppingToken);
            }
        }
    }
}
