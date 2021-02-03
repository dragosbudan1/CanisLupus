using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CanisLupus
{
    public class MarketMakerHandler : IMarketMakerHandler
    {
        private readonly ILogger<MarketMakerHandler> logger;
        private readonly IBinanceClient binanceClient;

        public MarketMakerHandler(ILogger<MarketMakerHandler> logger,
                                  IBinanceClient binanceClient)
        {
            this.logger = logger;
            this.binanceClient = binanceClient;
        }

        public async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var latestSymbolCandle = await binanceClient.GetLatestCandleAsync("DOGEUSDT");

            // get market movement

            // figure out upward, downward or plateau trend

            // if on downward trend
                // wait for bottom (or near bottom)
                // create a buy order
            
            // if upward trend
                // and there has been a honored buy order (ie there are funds to sell) (try +1% first)
                // sell order

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}