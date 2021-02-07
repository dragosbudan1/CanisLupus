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
using CanisLupus.Worker.Models;
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
        private readonly IWeightedMovingAverageCalculator weightedMovingAverageCalculator;
        private readonly IIntersectionFinder intersectionFinder;

        public MarketMakerHandler(ILogger<MarketMakerHandler> logger,
                                  IBinanceClient binanceClient,
                                  IEventPublisher candleDataPublisher,
                                  IClusterGenerator clusterGenerator,
                                  ISwingPointsGenerator swingPointsGenerator,
                                  IWeightedMovingAverageCalculator weightedMovingAverageCalculator,
                                  IIntersectionFinder intersectionFinder)
        {
            this.logger = logger;
            this.binanceClient = binanceClient;
            this.candleDataPublisher = candleDataPublisher;
            this.clusterGenerator = clusterGenerator;
            this.swingPointsGenerator = swingPointsGenerator;
            this.weightedMovingAverageCalculator = weightedMovingAverageCalculator;
            this.intersectionFinder = intersectionFinder;
        }

        public async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //var latestSymbolCandle = await binanceClient.GetLatestCandleAsync("DOGEUSDT");
            var allCandleData = await binanceClient.GetCandlesAsync("DOGEUSDT", 144 + 60, "1m");
            var candleData = allCandleData.TakeLast(60).ToList();
            logger.LogInformation("Candle {candle}", candleData.FirstOrDefault().ToLoggableMin());

            var result = await candleDataPublisher.PublishAsync(new EventRequest()
            {
                QueueName = "candleData",
                Value = JsonConvert.SerializeObject(candleData)
            });

            var highClusters = await clusterGenerator.GenerateClusters(candleData, ClusterType.High);
            result = await candleDataPublisher.PublishAsync(new EventRequest()
            {
                QueueName = "highClusterData",
                Value = JsonConvert.SerializeObject(highClusters)
            });

            var lowClusters = await clusterGenerator.GenerateClusters(candleData, ClusterType.Low);
            result = await candleDataPublisher.PublishAsync(new EventRequest()
            {
                QueueName = "lowClusterData",
                Value = JsonConvert.SerializeObject(lowClusters)
            });

            var wmaData = weightedMovingAverageCalculator.Calculate(allCandleData, candleData.Count);
            result = await candleDataPublisher.PublishAsync(new EventRequest
            {
                QueueName = "wmaData",
                Value = JsonConvert.SerializeObject(wmaData)
            });


            var smmaData = weightedMovingAverageCalculator.Calculate(allCandleData.Skip(139).ToList(), candleData.Count);
            result = await candleDataPublisher.PublishAsync(new EventRequest
            {
                QueueName = "smmaData",
                Value = JsonConvert.SerializeObject(smmaData)
            });

            var intersections = intersectionFinder.Find(candleData, wmaData.ToArray(), smmaData.ToArray(), candleData.Count);

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

        private void ProcessData(List<CandleRawData> candleData, List<Vector2> allWmaData, List<Vector2> allSmmaData, int dataSetCount = 60)
        {
            // find intersection in current data sample
            var wmaData = allWmaData.TakeLast(dataSetCount).ToArray();
            var smmaData = allSmmaData.TakeLast(dataSetCount).ToArray();
            var intersectionList = new List<Vector2>();
            Console.WriteLine($"{wmaData.Length} {smmaData.Length}");
            for (int i = 0; i < wmaData.Length - 1; i++)
            {
                var diff = Math.Abs((decimal)(wmaData[i].Y - smmaData[i].Y));
                if (diff < 0.00005m)
                {
                    Console.WriteLine($"Diff {diff} Wma: {wmaData[i]} Smma: {smmaData[i]}");
                    intersectionList.Add(wmaData[i]);
                }
            }

            List<string> messages = new List<string>();
            if (!intersectionList.Any())
            {
                // log no interesections found
                var message = $"{DateTime.UtcNow} No intersections found between {candleData.FirstOrDefault().OpenTime} - {candleData.LastOrDefault().CloseTime}";
                logger.LogInformation(message);
                messages.Add(message);
            }
            else
            {
                foreach (var item in intersectionList)
                {
                    var message = $"{DateTime.UtcNow} Intersection found: {candleData[(int)item.X].ToLoggable()}, wma: {item.Y}";
                    logger.LogInformation(message);
                    messages.Add(message);
                }
            }

            candleDataPublisher.PublishAsync(new EventRequest { QueueName = "tradingLogs", Value = JsonConvert.SerializeObject(messages) });
        }
    }
}