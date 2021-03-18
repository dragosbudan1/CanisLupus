using System;
using System.Collections.Generic;
using System.Linq;
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
        Task ExecuteAsync(TradingSettings settings);
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
        private readonly ITradingSettingsService tradingSettingsClient;
        private const decimal IntersectionOldThreshold = 55;
        private const decimal IntersectionFinishedThreshold = 1;

        public MarketMakerHandler(IBinanceClient binanceClient,
                                  IEventPublisher candleDataPublisher,
                                  IWeightedMovingAverageCalculator weightedMovingAverageCalculator,
                                  IIntersectionClient intersectionFinder,
                                  ITradingClient tradingClient,
                                  IOrderClient orderClient,
                                  IWalletClient walletClient,
                                  ITradingSettingsService tradingSettingsClient)
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

        public async Task ExecuteAsync(TradingSettings tradingSettings)
        {
            //TODO validate symbol
            if(string.IsNullOrEmpty(tradingSettings?.Symbol))
            {
                logger.Error($"Symbol cannot be empty");
            }

            var allCandleData = await binanceClient.GetCandlesAsync(tradingSettings?.Symbol, 144 + 60, "1m");
            var candleData = allCandleData?.TakeLast(60).ToList();
            logger.Info("Candle {0}", candleData?.FirstOrDefault().ToLoggableMin());

            var wmaData = await weightedMovingAverageCalculator.Calculate(allCandleData, candleData?.Count);
            var smmaData = await weightedMovingAverageCalculator.Calculate(allCandleData?.Skip(139).ToList(), candleData?.Count);

            //1. Update Orders from Binance
            

            var openOrders = await orderClient?.FindOpenOrders(tradingSettings?.Symbol);

            var intersections = intersectionClient.ExtractFromChart(wmaData?.ToArray(), smmaData?.ToArray(), tradingSettings?.Symbol, candleData?.Count);

            await PublishData(candleData, smmaData, wmaData, intersections, tradingSettings?.Symbol);

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

            var activeTrades = await tradingClient.FindActiveTrades(tradingSettings?.Symbol);

            logger.Info($"MarketHandler: {tradingSettings?.TradingStatus}");
            if (tradingSettings?.TradingStatus == TradingStatus.Active)
            {
                logger.Info($"Running trading algo {tradingSettings.Symbol}");
                // 3. try to buy
                // bug creates order each frame because it processes the interesection again
                if ((openOrders == null || !openOrders.Any()))
                {
                    // DONT REPROCESS
                    if (newestIntersection?.Status == IntersectionStatus.New && newestIntersection?.Type == IntersectionType.Upward)
                    {
                        var price = newestIntersection.Point.Y;
                        var spend = tradingSettings.SpendLimit;
                        var order = await orderClient.CreateAsync(new Order()
                        {
                            SpendAmount = spend,
                            Price = price,
                            Quantity = spend / price,
                            ProfitPercentage = tradingSettings.ProfitPercentage,
                            StopLossPercentage = tradingSettings.StopLossPercentage,
                            Side = OrderSide.Buy,
                            Symbol = tradingSettings.Symbol
                        });
                        newestIntersection.Status = IntersectionStatus.Active;
                        await intersectionClient.UpdateAsync(newestIntersection);
                        // create new trade
                        await tradingClient.CreateActiveTrade(new Trade()
                        {
                            OrderId = order.Id,
                            Symbol = order.Symbol,
                            StartSpend = order.SpendAmount,
                            TradeStatus = TradeStatus.Active,
                            TradeType = TradeType.Buy
                        });
                    }
                }
            }
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

        private async Task PublishData(List<CandleRawData> candleRawData, List<Vector2> smaData, List<Vector2> wmaData, List<Intersection> intersectionList, string symbol = null)
        {
            if(string.IsNullOrEmpty(symbol))
            {
                logger.Error("Cannot publish ViewData for empty symbol");
                return;
            }
            List<string> messages = new List<string>();
            if (!intersectionList.Any())
            {
                // log no interesections found
                var message = $"{DateTime.UtcNow} {symbol} No intersections found between {candleRawData?.FirstOrDefault().OpenTime} - {candleRawData?.LastOrDefault().CloseTime}";
                logger.Info(message);
                messages.Add(message);
            }
            else
            {
                foreach (var item in intersectionList)
                {
                    var message = $"{DateTime.UtcNow} {symbol} Intersection found: {candleRawData?.ElementAt((int)item.Point.X)?.ToLoggableMin()}, sma: {item.Point.Y}, trend: {item.Type.ToString()}";
                    logger.Info(message);
                    messages.Add(message);
                }
            }

            var viewData = new ViewData
            {
                CandleData = candleRawData,
                SmaData = smaData,
                WmaData = wmaData,
                TradingLogs = messages               
            };

            await eventPublisher.PublishAsync(new EventRequest()
            {
                QueueName = $"viewData-{symbol}",
                Value = JsonConvert.SerializeObject(viewData)
            });
        }
    }
}