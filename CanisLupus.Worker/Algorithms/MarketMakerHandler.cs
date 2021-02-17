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
using CanisLupus.Worker.Account;

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
        private readonly IWalletClient walletClient;
        private readonly ITradingSettingsClient tradingSettingsClient;
        private const decimal IntersectionOldThreshold = 55;
        private const decimal IntersectionFinishedThreshold = 1;
        public static List<Intersection> Intersections = new List<Intersection>();

        public static Wallet Wallet;

        public MarketMakerHandler(IBinanceClient binanceClient,
                                  IEventPublisher candleDataPublisher,
                                  IWeightedMovingAverageCalculator weightedMovingAverageCalculator,
                                  IIntersectionClient intersectionFinder,
                                  ITradingClient tradingClient,
                                  IOrderClient orderClient,
                                  IWalletClient walletClient,
                                  ITradingSettingsClient tradingSettingsClient)
        {
            this.logger = LogManager.GetCurrentClassLogger();
            this.binanceClient = binanceClient;
            this.eventPublisher = candleDataPublisher;
            this.weightedMovingAverageCalculator = weightedMovingAverageCalculator;
            this.intersectionClient = intersectionFinder;
            this.tradingClient = tradingClient;
            this.orderClient = orderClient;
            this.walletClient = walletClient;
            this.tradingSettingsClient = tradingSettingsClient;
        }

        public async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //var latestSymbolCandle = await binanceClient.GetLatestCandleAsync("DOGEUSDT");
            var allCandleData = await binanceClient.GetCandlesAsync("BNBBUSD", 144 + 60, "1m");
            var candleData = allCandleData?.TakeLast(60).ToList();
            logger.Info("Candle {0}", candleData?.FirstOrDefault().ToLoggableMin());

            var result = await eventPublisher.PublishAsync(new EventRequest()
            {
                QueueName = "candleData",
                Value = JsonConvert.SerializeObject(candleData)
            });

            var wmaData = await weightedMovingAverageCalculator.Calculate(allCandleData, candleData?.Count, "wmaData");
            var smmaData = await weightedMovingAverageCalculator.Calculate(allCandleData?.Skip(139).ToList(), candleData?.Count, "smmaData");

            //1. Update Orders from Binance
            var openOrders = await orderClient?.FindOpenOrders();
            var activeTrades = await tradingClient?.FindActiveTrades();
            // var currentCandle = candleData?.LastOrDefault();
            // var activeIntersection = (await intersectionClient.FindIntersectionsByStatus(IntersectionStatus.Active))?.FirstOrDefault();

            var intersections = intersectionClient.ExtractFromChart(candleData, wmaData?.ToArray(), smmaData?.ToArray(), candleData?.Count);

            // 2. Process intersections
            // - add NEW intersections
            // - update NEW -> OLD intersections
            Intersection newestIntersection = null;
            if (intersections != null)
                foreach (var intersection in intersections)
                {
                    var existingIntersection = await intersectionClient.FindByIntersectionDetails(intersection);

                    if (existingIntersection != null)
                    {
                        existingIntersection.Point.X = intersection.Point.X;
                        ProcessIntersection(existingIntersection, intersection.Point.X);

                        var updatedIntersection = await intersectionClient.UpdateAsync(existingIntersection);
                        if (updatedIntersection.Status == IntersectionStatus.New)
                        {
                            newestIntersection = updatedIntersection;
                        }
                    }
                    else
                    {
                        ProcessIntersection(intersection, intersection.Point.X);

                        intersection.Status = IntersectionStatus.New;
                        var inserted = await intersectionClient.InsertAsync(intersection);
                        if (inserted)
                        {
                            newestIntersection = intersection;
                        }
                    }
                }

            var tradingSettings = await tradingSettingsClient.GetAsync();
            if (tradingSettings?.TradingStatus == TradingStatus.Active)
            {
                // 3. try to buy
                // bug creates order each frame because it processes the interesection again
                if ((activeTrades == null || !activeTrades.Any()) && (openOrders == null || !openOrders.Any()))
                {
                    // DONT REPROCESS
                    if (newestIntersection?.Status == IntersectionStatus.New && newestIntersection?.Type == IntersectionType.Upward)
                    {
                        var price = newestIntersection.Point.Y;
                        var spend = tradingSettings.SpendLimit;
                        var order = orderClient.CreateOrder(new Order()
                        {
                            Spend = spend,
                            Price = price,
                            Amount = spend / price,
                            ProfitPercentage = tradingSettings.ProfitPercentage,
                            StopLossPercentage = tradingSettings.StopLossPercentage,
                            Type = OrderType.Buy
                        });
                        newestIntersection.Status = IntersectionStatus.Active;
                        await intersectionClient.UpdateAsync(newestIntersection);
                    }
                }
            }

            await eventPublisher.PublishAsync(new EventRequest
            {
                QueueName = "tradingInfo",
                Value = JsonConvert.SerializeObject(tradingSettings)
            });
        }

        private void ProcessIntersection(Intersection intersection, decimal currentX)
        {
            intersection.Point.X = currentX;

            if (intersection.Status == IntersectionStatus.New && intersection.Point.X < IntersectionOldThreshold)
            {
                intersection.Status = IntersectionStatus.Old;
            }
            else if (intersection.Status == IntersectionStatus.Old && intersection.Point.X <= IntersectionFinishedThreshold)
            {
                intersection.Status = IntersectionStatus.Finished;
            }
        }
    }
}