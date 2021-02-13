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
        private readonly IWalletClient walletClient;
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
                                  IWalletClient walletClient)
        {
            this.logger = LogManager.GetCurrentClassLogger();
            this.binanceClient = binanceClient;
            this.eventPublisher = candleDataPublisher;
            this.weightedMovingAverageCalculator = weightedMovingAverageCalculator;
            this.intersectionClient = intersectionFinder;
            this.tradingClient = tradingClient;
            this.orderClient = orderClient;
            this.walletClient = walletClient;
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

            //Trading Engine (process Orders)
            var openOrders = await orderClient?.FindOpenOrders();
            var activeTrades = await tradingClient?.FindActiveTrades();
            var currentCandle = candleData?.LastOrDefault();
            var activeIntersection = (await intersectionClient.FindIntersectionsByStatus(IntersectionStatus.Active)).FirstOrDefault();

            if (candleData != null && openOrders != null && openOrders.Any())
            {
                // ORDER IS NOT THE LATEST
                var openBuyOrder = openOrders.FirstOrDefault(x => x.Type == OrderType.Buy);
                var openSellOrder = openOrders.FirstOrDefault(x => x.Type == OrderType.Sell);

                if (openBuyOrder != null && openBuyOrder.Price >= currentCandle.Middle)
                {
                    await orderClient.UpdateOrderAsync(openBuyOrder.Id, OrderStatus.Filled);
                    await tradingClient.CreateActiveTrade(new Trade()
                    {
                        OrderId = openBuyOrder.Id,
                        TradeType = TradeType.Buy,
                        StartSpend = openBuyOrder.Spend
                    });
                    activeIntersection.Status = IntersectionStatus.Finished;
                    await intersectionClient.UpdateAsync(activeIntersection);
                    await walletClient.UpdateWallet("fake_wallet_id", -openBuyOrder.Spend);
                }

                if (openSellOrder != null && openSellOrder.Price <= currentCandle.Middle)
                {
                    await orderClient.UpdateOrderAsync(openSellOrder.Id, OrderStatus.Filled);
                    await tradingClient.CloseTrade(activeTrades?.FirstOrDefault().Id, openSellOrder);
                    activeIntersection.Status = IntersectionStatus.Finished;
                    await intersectionClient.UpdateAsync(activeIntersection);
                    await walletClient.UpdateWallet("fake_wallet_id", openSellOrder.Spend);
                }
            }

            var intersections = intersectionClient.ExtractFromChart(candleData, wmaData?.ToArray(), smmaData?.ToArray(), candleData?.Count);

            // 1. Process intersections
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

            if (openOrders != null && openOrders.Any())
            {
                var openBuyOrder = openOrders.FirstOrDefault(x => x.Type == OrderType.Buy);
                var openSellOrder = openOrders.FirstOrDefault(x => x.Type == OrderType.Sell);

                if (openBuyOrder != null)
                {
                    if (newestIntersection?.Type == IntersectionType.Downward)
                    {
                        var cancelledOrder = await orderClient.UpdateOrderAsync(openBuyOrder.Id, OrderStatus.Cancelled);
                        var activeTrade = (await tradingClient.FindActiveTrades()).FirstOrDefault();
                        if(activeTrade.OrderId == openBuyOrder.Id)
                        {
                            await tradingClient.CloseTrade(openBuyOrder.Id, openBuyOrder);
                        }
                    }
                }

                if (openSellOrder != null)
                {
                    if (newestIntersection?.Type == IntersectionType.Upward)
                    {
                        var cancelledOrder = await orderClient.UpdateOrderAsync(openSellOrder.Id, OrderStatus.Cancelled);
                       
                    }
                }
            }

            // 3. Process Active trades
            if (activeTrades != null && activeTrades.Any())
            {
                var currentPrice = candleData.LastOrDefault().Middle;

                foreach (var trade in activeTrades)
                {
                    var linkedOrder = await orderClient.FindOrderById(trade.OrderId);

                    if (linkedOrder != null)
                    {
                        // sell for profit
                        if (linkedOrder.TargetPrice <= currentPrice && currentPrice > linkedOrder.Price)
                        {
                            var sellOrder = new Order()
                            {
                                Type = OrderType.Sell,
                                Price = linkedOrder.TargetPrice,
                                Amount = linkedOrder.Amount,
                                Spend = linkedOrder.Amount * linkedOrder.TargetPrice,
                                ProfitPercentage = linkedOrder.ProfitPercentage
                            };
                            await orderClient.CreateOrder(sellOrder);
                        }
                        else if (linkedOrder.StopLossPrice >= currentPrice && currentPrice < linkedOrder.Price)
                        {
                            var sellOrder = new Order()
                            {
                                Type = OrderType.Sell,
                                Price = linkedOrder.StopLossPrice,
                                Amount = linkedOrder.Amount,
                                Spend = linkedOrder.Amount * linkedOrder.StopLossPrice,
                                ProfitPercentage = linkedOrder.ProfitPercentage,
                                StopLossPercentage = linkedOrder.StopLossPercentage
                            };
                            await orderClient.CreateOrder(sellOrder);
                        }
                    }
                }
            }

            // 4. try to buy
            // bug creates order each frame because it processes the interesection again
            if ((activeTrades == null || !activeTrades.Any()) && (openOrders == null || !openOrders.Any()))
            {
                // DONT REPROCESS
                if (newestIntersection?.Status == IntersectionStatus.New && newestIntersection?.Type == IntersectionType.Upward)
                {
                    var price = newestIntersection.Point.Y;
                    var spend = 100;
                    var order = orderClient.CreateOrder(new Order()
                    {
                        Spend = spend,
                        Price = price,
                        Amount = spend / price,
                        ProfitPercentage = 2,
                        StopLossPercentage = 5,
                        Type = OrderType.Buy
                    });
                    newestIntersection.Status = IntersectionStatus.Active;
                    await intersectionClient.UpdateAsync(newestIntersection);
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