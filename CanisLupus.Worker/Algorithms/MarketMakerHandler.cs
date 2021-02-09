using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CanisLupus.Worker.Algorithms;
using CanisLupus.Worker.Events;
using CanisLupus.Worker.Exchange;
using CanisLupus.Worker.Extensions;
using CanisLupus.Common.Models;
using CanisLupus.Worker.Trader;
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
        private readonly IEventPublisher eventPublisher;
        private readonly IWeightedMovingAverageCalculator weightedMovingAverageCalculator;
        private readonly IIntersectionClient intersectionClient;
        private readonly ITradingClient tradingClient;
        public static List<Intersection> Intersections = new List<Intersection>();

        public static Wallet Wallet;

        public MarketMakerHandler(ILogger<MarketMakerHandler> logger,
                                  IBinanceClient binanceClient,
                                  IEventPublisher candleDataPublisher,
                                  IWeightedMovingAverageCalculator weightedMovingAverageCalculator,
                                  IIntersectionClient intersectionFinder,
                                  ITradingClient tradingClient)
        {
            this.logger = logger;
            this.binanceClient = binanceClient;
            this.eventPublisher = candleDataPublisher;
            this.weightedMovingAverageCalculator = weightedMovingAverageCalculator;
            this.intersectionClient = intersectionFinder;
            this.tradingClient = tradingClient;
        }

        public async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //var latestSymbolCandle = await binanceClient.GetLatestCandleAsync("DOGEUSDT");
            var allCandleData = await binanceClient.GetCandlesAsync("DOGEUSDT", 144 + 60, "1m");
            var candleData = allCandleData?.TakeLast(60).ToList();
            logger.LogInformation("Candle {candle}", candleData?.FirstOrDefault().ToLoggableMin());

            var result = await eventPublisher.PublishAsync(new EventRequest()
            {
                QueueName = "candleData",
                Value = JsonConvert.SerializeObject(candleData)
            });

            var wmaData = await weightedMovingAverageCalculator.Calculate(allCandleData, candleData?.Count, "wmaData");
            var smmaData = await weightedMovingAverageCalculator.Calculate(allCandleData?.Skip(139).ToList(), candleData?.Count, "smmaData");

            var intersections = intersectionClient.ExtractFromChart(candleData, wmaData?.ToArray(), smmaData?.ToArray(), candleData?.Count);
            
            // 1. Process intersections
            // - add NEW intersections
            // - update NEW -> OLD intersections

            // intersections.ForEach(async (x) => await intersectionClient.InsertAsync(x));
            

            // foreach(var intersection in intersections)
            // {
            //     var existingIntersection = await intersectionClient.FindByIntersectionDetails(intersection);

            //     if(existingIntersection != null)
            //     {
            //         if(existingIntersection.Status == IntersectionStatus.New && existingIntersection.Point.X < 55)
            //         {
            //             existingIntersection.Status = IntersectionStatus.Old;
            //             await intersectionClient.Update(existingIntersection);
            //         }
            //     }
            //     else
            //     {
            //         await intersectionClient.InsertAsync(intersection);
            //     }         
            // }

            // var activeIntersection = Intersections.Where(x => x.Status == IntersectionStatus.Active).FirstOrDefault();
            // // check if we chould sell
            // var openOrder = TradingClient.OpenOrders.FirstOrDefault();
            // if (openOrder != null)
            // {
            //     var currentPrice = (decimal)candleData.LastOrDefault().Middle;
            //     var targetPrice = openOrder.Price + openOrder.Price * 0.02m;
            //     if (targetPrice >= currentPrice)
            //     {
            //         await tradingClient.CreateSellOrder(openOrder.Amount, targetPrice);
            //         if (activeIntersection != null)
            //         {
            //             activeIntersection.Status = IntersectionStatus.Finished;
            //         }
            //     }
            // }

            // foreach (var item in intersections)
            // {
            //     var existingIntersection = Intersections?.Where(x => x.Point.Y == item.Point.Y && x.Type == item.Type).FirstOrDefault();

            //     if (existingIntersection != null)
            //     {
            //         //check if old
            //         if (existingIntersection.Point.X < 55 && existingIntersection.Status == IntersectionStatus.New)
            //         {
            //             existingIntersection.Status = IntersectionStatus.Old;
            //         }
            //     }
            //     else
            //     {
            //         //add intersection as new
            //         item.Id = Guid.NewGuid();
            //         item.Status = IntersectionStatus.New;
            //         Intersections.Add(item);
            //     }
            // }

            // var newestIntersection = Intersections.Where(x => x.Status == IntersectionStatus.New).OrderByDescending(x => x.Point.X).FirstOrDefault();

            // if (activeIntersection != null && newestIntersection != null)
            // {
            //     // create stop loss if buy order
            //     if (newestIntersection.Type == IntersectionType.Downward && activeIntersection.Type == IntersectionType.Upward)
            //     {
            //         if (openOrder != null)
            //         {
            //             await tradingClient.CreateSellOrder(openOrder.Amount, (decimal)newestIntersection.Point.Y);
            //             if (activeIntersection != null)
            //             {
            //                 activeIntersection.Status = IntersectionStatus.Finished;
            //             }
            //         }

            //     }
            //     // cancel stop loss if uptrend

            // }
            // else if (newestIntersection != null && !TradingClient.OpenOrders.Any())
            // {
            //     // check to see if can put order in to buy
            //     if (newestIntersection.Type == IntersectionType.Upward)
            //     {
            //         var price = newestIntersection.Point.Y;
            //         var orderResult = await tradingClient.CreateBuyOrder((decimal)price, 100.0m);
            //         if (orderResult.Success)
            //         {
            //             newestIntersection.Status = IntersectionStatus.Active;
            //         }
            //     }
            // }

            var tradingInfo = new TradingInfo
            {
                Wallet = TradingClient.Wallet,
                Intersections = Intersections,
                OpenOrders = TradingClient.OpenOrders
            };

            await eventPublisher.PublishAsync(new EventRequest
            {
                QueueName = "tradingInfo",
                Value = JsonConvert.SerializeObject(tradingInfo)
            });
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

            eventPublisher.PublishAsync(new EventRequest { QueueName = "tradingLogs", Value = JsonConvert.SerializeObject(messages) });
        }
    }
}