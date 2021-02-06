using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using CanisLupus.Worker.Algorithms;
using CanisLupus.Worker.Events;
using CanisLupus.Worker.Exchange;
using CanisLupus.Worker.Extensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CanisLupus.Worker
{
    public interface IMarketMakerHandler
    {
         Task ExecuteAsync(CancellationToken stoppingToken);
    }

    public class MarketMakerHandler : IMarketMakerHandler
    {
        private readonly ILogger<MarketMakerHandler> logger;
        private readonly IBinanceClient binanceClient;
        private readonly IEventPublisher candleDataPublisher;
        private readonly IClusterGenerator clusterGenerator;
        private readonly ISwingPointsGenerator swingPointsGenerator;

        public MarketMakerHandler(ILogger<MarketMakerHandler> logger,
                                  IBinanceClient binanceClient,
                                  IEventPublisher candleDataPublisher,
                                  IClusterGenerator clusterGenerator,
                                  ISwingPointsGenerator swingPointsGenerator)
        {
            this.logger = logger;
            this.binanceClient = binanceClient;
            this.candleDataPublisher = candleDataPublisher;
            this.clusterGenerator = clusterGenerator;
            this.swingPointsGenerator = swingPointsGenerator;
        }

        public async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //var latestSymbolCandle = await binanceClient.GetLatestCandleAsync("DOGEUSDT");

            var listSymbolCandle = await binanceClient.GetCandlesAsync("DOGEUSDT", 30, "1m");
            logger.LogInformation("Candle {candle}", listSymbolCandle.FirstOrDefault().ToLoggableMin());

            var result = await candleDataPublisher.PublishAsync(new EventRequest()
            {
                QueueName = "candleData",
                Value = JsonConvert.SerializeObject(listSymbolCandle)
            });

            var highClusters = await clusterGenerator.GenerateClusters(listSymbolCandle, ClusterType.High);
            result = await candleDataPublisher.PublishAsync(new EventRequest()
            {
                QueueName = "highClusterData",
                Value = JsonConvert.SerializeObject(highClusters)
            });

            var lowClusters = await clusterGenerator.GenerateClusters(listSymbolCandle, ClusterType.Low);
            result = await candleDataPublisher.PublishAsync(new EventRequest()
            {
                QueueName = "lowClusterData",
                Value = JsonConvert.SerializeObject(lowClusters)
            });

            // var swingPoints = await swingPointsGenerator.GeneratePoints(listSymbolCandle);
            // result = await candleDataPublisher.PublishAsync(new EventRequest()
            // {
            //     QueueName = "swingPointsData",
            //     Value = JsonConvert.SerializeObject(swingPoints)
            // });
            // get market movement

            // figure out upward, downward or plateau trend

            // if on downward trend
            // wait for bottom (or near bottom)
            // create a buy order

            // if upward trend
            // and there has been a honored buy order (ie there are funds to sell) (try +1% first)
            // sell order

            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
        }
    }
}