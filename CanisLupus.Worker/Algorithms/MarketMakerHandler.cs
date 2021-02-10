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
using Newtonsoft.Json;
using NLog;

namespace CanisLupus.Worker
{
    public interface IMarketMakerHandler
    {
        Task ExecuteAsync(CancellationToken stoppingToken);
    }

    public class MarketMakerHandler : IMarketMakerHandler
    {
        private readonly ILogger logger;
        private readonly IBinanceClient binanceClient;
        private readonly IEventPublisher eventPublisher;
        private readonly IWeightedMovingAverageCalculator weightedMovingAverageCalculator;
        private readonly IIntersectionClient intersectionClient;
        private readonly ITradingClient tradingClient;
        private readonly IOrderClient orderClient;
        private const decimal IntersectionOldThreshold = 55;
        private const decimal IntersectionFinishedThreshold = 1;
        public static List<Intersection> Intersections = new List<Intersection>();

        public static Wallet Wallet;

        public MarketMakerHandler(IBinanceClient binanceClient,
                                  IEventPublisher candleDataPublisher,
                                  IWeightedMovingAverageCalculator weightedMovingAverageCalculator,
                                  IIntersectionClient intersectionFinder,
                                  ITradingClient tradingClient,
                                  IOrderClient orderClient)
        {
            this.logger = LogManager.GetCurrentClassLogger();
            this.binanceClient = binanceClient;
            this.eventPublisher = candleDataPublisher;
            this.weightedMovingAverageCalculator = weightedMovingAverageCalculator;
            this.intersectionClient = intersectionFinder;
            this.tradingClient = tradingClient;
            this.orderClient = orderClient;
        }

        public async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //var latestSymbolCandle = await binanceClient.GetLatestCandleAsync("DOGEUSDT");
            var allCandleData = await binanceClient.GetCandlesAsync("DOGEUSDT", 144 + 60, "1m");
            var candleData = allCandleData?.TakeLast(60).ToList();
            logger.Info("Candle {0}", candleData?.FirstOrDefault().ToLoggableMin());

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
            Intersection newestIntersection = null;
            foreach(var intersection in intersections)
            {
                var existingIntersection = await intersectionClient.FindByIntersectionDetails(intersection);

                if(existingIntersection != null)
                {
                    existingIntersection.Point.X = intersection.Point.X;

                    if(existingIntersection.Status == IntersectionStatus.New && existingIntersection.Point.X < IntersectionOldThreshold)
                    {
                        existingIntersection.Status = IntersectionStatus.Old;
                    } 
                    else if (existingIntersection.Status == IntersectionStatus.Old && existingIntersection.Point.X <= IntersectionFinishedThreshold)
                    {
                        existingIntersection.Status = IntersectionStatus.Finished;
                    }

                    var updatedIntersection = await intersectionClient.UpdateAsync(existingIntersection);
                    if(updatedIntersection.Status == IntersectionStatus.New)
                    {
                        newestIntersection = updatedIntersection;
                    }
                }
                else
                {
                    intersection.Status = IntersectionStatus.New;
                    var inserted = await intersectionClient.InsertAsync(intersection);
                    if(inserted)
                    {
                        newestIntersection = intersection;
                    }
                }         
            }
            
            // 2. Process Open Orders
            var openOrders = await orderClient?.FindOpenOrders();
            if(openOrders != null && openOrders.Any())
            {
                var openBuyOrder = openOrders.FirstOrDefault(x => x.Type == OrderType.Buy);
                var openSellOrder = openOrders.FirstOrDefault(x => x.Type == OrderType.Sell);

                if(openBuyOrder != null)
                {
                    if(newestIntersection?.Type == IntersectionType.Downward) 
                    {
                        var cancelledOrder = await orderClient.CancelOrder(openBuyOrder);
                    }
                }

                if(openSellOrder != null)
                {
                    if(newestIntersection?.Type == IntersectionType.Upward)
                    {
                        var cancelledOrder = await orderClient.CancelOrder(openSellOrder);
                    }
                }
            }

            // 3. Process Active trades
            var activeTrades = await tradingClient.FindActiveTrades();
            if(activeTrades != null && activeTrades.Any())
            {
                var currentPrice = candleData.LastOrDefault().Middle;
                // var linkedOrder = orderClient.FindOrderById();
                // if()
            }
             
            // 4. try to buy
            if((activeTrades == null || !activeTrades.Any()) && (openOrders == null || !openOrders.Any()))
            {
                if(newestIntersection?.Type == IntersectionType.Upward)
                {
                    var price = newestIntersection.Point.Y;
                    var spend = 100;
                    var order = orderClient.CreateOrder(new Order()
                    {
                        Spend = spend,
                        Price = price,
                        Amount = spend * price,
                        ProfitPercentage = 2,
                        Type = OrderType.Buy
                    });
                }
            }
            
            var tradingInfo = new TradingInfo
            {
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
                logger.Info(message);
                messages.Add(message);
            }
            else
            {
                foreach (var item in intersectionList)
                {
                    var message = $"{DateTime.UtcNow} Intersection found: {candleData[(int)item.X].ToLoggable()}, wma: {item.Y}";
                    logger.Info(message);
                    messages.Add(message);
                }
            }

            eventPublisher.PublishAsync(new EventRequest { QueueName = "tradingLogs", Value = JsonConvert.SerializeObject(messages) });
        }
    }
}