using System.Collections.Generic;
using System.Threading.Tasks;
using CanisLupus.Worker;
using CanisLupus.Worker.Algorithms;
using CanisLupus.Worker.Events;
using CanisLupus.Worker.Exchange;
using CanisLupus.Common.Models;
using CanisLupus.Worker.Trader;
using Moq;
using NUnit.Framework;
using CanisLupus.Worker.Account;

namespace CanisLupus.Tests
{
    public class MarketHandlerTests
    {
        private List<Intersection> intersections = new List<Intersection>();
        private IMarketMakerHandler SUT;

        private Mock<IBinanceClient> mockBinanceClient;
        private Mock<IEventPublisher> mockEventPublisher;
        private Mock<IIntersectionClient> mockIntersectionClient;
        private Mock<IWeightedMovingAverageCalculator> mockWMACalculator;
        private Mock<IOrderClient> mockOrderClient;
        private Mock<ITradingSettingsService> mockTradingSettingsClient;
        private Mock<ITradingClient> mockTradingClient;

        [SetUp]
        public void Setup()
        {
            mockBinanceClient = new Mock<IBinanceClient>();
            mockEventPublisher = new Mock<IEventPublisher>();
            mockIntersectionClient = new Mock<IIntersectionClient>();
            mockTradingClient = new Mock<ITradingClient>();
            mockWMACalculator = new Mock<IWeightedMovingAverageCalculator>();
            mockOrderClient = new Mock<IOrderClient>();
            mockTradingSettingsClient = new Mock<ITradingSettingsService>();

            SUT = new MarketMakerHandler(
                mockBinanceClient.Object,
                mockEventPublisher.Object,
                mockWMACalculator.Object,
                mockIntersectionClient.Object,
                mockTradingClient.Object,
                mockOrderClient.Object,
                new Mock<IWalletClient>().Object,
                mockTradingSettingsClient.Object
            );
        }

        [TearDown]
        public void Teardown()
        {
        }

        [Test]
        public async Task TestAddNewIntersection()
        {
            var newestIntersection = new Intersection
            {
                Id = null,
                Point = new Vector2(57, 0.0745123m),
                Status = null,
                Type = IntersectionType.Upward
            };

            var intersections = new List<Intersection>
            {
                newestIntersection
            };

            mockIntersectionClient.Setup(x => x.ExtractFromChart(It.IsAny<Vector2[]>(), It.IsAny<Vector2[]>(), It.IsAny<string>(), It.IsAny<int?>()))
                .Returns(intersections);


            var tradingSettings = new TradingSettings()
            {
                Symbol = "DOGEUSDT"
            };

            await SUT.ExecuteAsync(tradingSettings);

            mockIntersectionClient.Verify(x => x.InsertAsync(It.Is<Intersection>(s =>
                s.Point == newestIntersection.Point &&
                s.Type == s.Type && s.Id == null &&
                 s.Status == IntersectionStatus.New
                 )), Times.Once);
        }

        [Test]
        public async Task TestUpdateNewToOldIntersection()
        {

            var dbGeneratedId = "id123";
            var existingIntersection = new Intersection
            {
                Id = dbGeneratedId,
                Point = new Vector2(54, 0.0745123m),
                Status = null,
                Type = IntersectionType.Upward
            };

            var intersections = new List<Intersection>
            {
                existingIntersection
            };

            mockIntersectionClient.Setup(x => x.ExtractFromChart(It.IsAny<Vector2[]>(), It.IsAny<Vector2[]>(), It.IsAny<string>(), It.IsAny<int?>()))
                .Returns(intersections);

            var updatedIntersection = existingIntersection;
            updatedIntersection.Id = dbGeneratedId;
            updatedIntersection.Status = IntersectionStatus.Old;

            mockIntersectionClient.Setup(x => x.FindByIntersectionDetails(It.Is<Intersection>(s =>
                s.Point == existingIntersection.Point &&
                s.Type == existingIntersection.Type)))
                    .ReturnsAsync(existingIntersection);

            mockIntersectionClient.Setup(x => x.UpdateAsync(It.Is<Intersection>(s => s.Id == dbGeneratedId)))
                .ReturnsAsync(updatedIntersection);
            

            var tradingSettings = new TradingSettings()
            {
                Symbol = "DOGEUSDT"
            };

            await SUT.ExecuteAsync(tradingSettings);

            mockIntersectionClient.Verify(x => x.InsertAsync(It.IsAny<Intersection>()), Times.Never);
            mockIntersectionClient.Verify(x => x.UpdateAsync(It.Is<Intersection>(s =>
                s.Id == dbGeneratedId &&
                s.Point == existingIntersection.Point &&
                s.Type == existingIntersection.Type &&
                s.Status == IntersectionStatus.Old)), Times.Once);
        }

        [Test]
        public async Task TestUpdateOldToFinishedIntersection()
        {
            var dbGeneratedId = "id123";
            var existingIntersection = new Intersection
            {
                Id = dbGeneratedId,
                Point = new Vector2(1, 0.0745123m),
                Status = IntersectionStatus.Old,
                Type = IntersectionType.Upward
            };

            var intersections = new List<Intersection>
            {
                existingIntersection
            };

            mockIntersectionClient.Setup(x => x.ExtractFromChart(It.IsAny<Vector2[]>(), It.IsAny<Vector2[]>(), It.IsAny<string>(), It.IsAny<int?>()))
                .Returns(intersections);

            mockIntersectionClient.Setup(x => x.FindByIntersectionDetails(It.Is<Intersection>(s =>
                s.Point == existingIntersection.Point &&
                s.Type == existingIntersection.Type)))
                    .ReturnsAsync(existingIntersection);

            var updatedIntersection = existingIntersection;
            updatedIntersection.Status = IntersectionStatus.Finished;

            mockIntersectionClient.Setup(x => x.UpdateAsync(It.Is<Intersection>(s => s.Id == dbGeneratedId)))
                .ReturnsAsync(updatedIntersection);

            
            var tradingSettings = new TradingSettings()
            {
                Symbol = "DOGEUSDT"
            };

            await SUT.ExecuteAsync(tradingSettings);

            mockIntersectionClient.Verify(x => x.InsertAsync(It.IsAny<Intersection>()), Times.Never);
            mockIntersectionClient.Verify(x => x.UpdateAsync(It.Is<Intersection>(s =>
                s.Id == dbGeneratedId &&
                s.Point == updatedIntersection.Point &&
                s.Type == updatedIntersection.Type &&
                s.Status == IntersectionStatus.Finished)), Times.Once);
        }

        [Test]
        public async Task TestIntersectionsNotOnChartSwithcToFinished()
        {
            var dbGeneratedId = "id123";
            var existingIntersection = new Intersection
            {
                Id = dbGeneratedId,
                Point = new Vector2(1, 0.0745123m),
                Status = IntersectionStatus.Old,
                Type = IntersectionType.Upward
            };

            var intersections = new List<Intersection>
            {
                existingIntersection
            };

            mockIntersectionClient.Setup(x => x.ExtractFromChart(It.IsAny<Vector2[]>(), It.IsAny<Vector2[]>(), It.IsAny<string>(), It.IsAny<int?>()))
                .Returns(intersections);

            var tradingSettings = new TradingSettings()
            {
                Symbol = "DOGEUSDT"
            };

            await SUT.ExecuteAsync(tradingSettings);
        }

        [Test]
        public async Task TestNewIntersectionAndNoOpenOrdersAndNoActiveTrades()
        {
            var newIntersection = new Intersection
            {
                Id = null,
                Point = new Vector2(57, 0.0745123m),
                Status = null,
                Type = IntersectionType.Upward
            };

            var intersections = new List<Intersection>
            {
                newIntersection
            };

            var tradingsSettings = new TradingSettings
            {
                TradingStatus = TradingStatus.Active,
                SpendLimit = 10,
                ProfitPercentage = 2,
                StopLossPercentage = 5,
                TotalSpendLimit = 100,
                Symbol = "DOGEUSDT"
            };

            var newOrder = new Order()
            {
                Price = newIntersection.Point.Y,
                SpendAmount = tradingsSettings.SpendLimit,
                ProfitPercentage = tradingsSettings.ProfitPercentage,
                StopLossPercentage = tradingsSettings.StopLossPercentage,
                Quantity = tradingsSettings.SpendLimit / newIntersection.Point.Y,
                Side = OrderSide.Buy
            };

            mockOrderClient.Setup(x => x.CreateAsync(It.IsAny<Order>())).ReturnsAsync(newOrder);

            mockIntersectionClient.Setup(x => x.ExtractFromChart(It.IsAny<Vector2[]>(), It.IsAny<Vector2[]>(), It.IsAny<string>(), It.IsAny<int?>()))
                .Returns(intersections);

            mockIntersectionClient.Setup(x => x.InsertAsync(It.IsAny<Intersection>())).ReturnsAsync(true);

            mockTradingSettingsClient.Setup(x => x.GetAsync(tradingsSettings.Symbol)).ReturnsAsync(tradingsSettings);    

            await SUT.ExecuteAsync(tradingsSettings);

            mockIntersectionClient.Setup(x => x.ExtractFromChart(It.IsAny<Vector2[]>(), It.IsAny<Vector2[]>(), It.IsAny<string>(), It.IsAny<int?>()))
                .Returns(intersections);

            mockOrderClient.Verify(x => x.CreateAsync(It.Is<Order>(s =>
                s.Price == newIntersection.Point.Y &&
                s.SpendAmount == tradingsSettings.SpendLimit &&
                s.ProfitPercentage == tradingsSettings.ProfitPercentage &&
                s.StopLossPercentage == tradingsSettings.StopLossPercentage &&
                s.Quantity == tradingsSettings.SpendLimit / newIntersection.Point.Y &&
                s.Side == OrderSide.Buy)), Times.Once);

            mockTradingClient.Verify(x => x.CreateActiveTrade(It.Is<Trade>(m => 
                m.OrderId == newOrder.Id &&
                m.StartSpend == newOrder.SpendAmount &&
                m.Symbol == newOrder.Symbol)), Times.Once);
        }

        [Test]
        public async Task TestNewIntersectionAndOpenOrders()
        {
            var newIntersection = new Intersection
            {
                Id = null,
                Point = new Vector2(57, 0.0745123m),
                Status = null,
                Type = IntersectionType.Upward
            };

            var intersections = new List<Intersection>
            {
                newIntersection
            };

            var tradingsSettings = new TradingSettings
            {
                TradingStatus = TradingStatus.Active,
                SpendLimit = 10,
                ProfitPercentage = 2,
                StopLossPercentage = 5,
                TotalSpendLimit = 100,
                Symbol = "DOGEUSDT"
            };

            mockIntersectionClient.Setup(x => x.ExtractFromChart(It.IsAny<Vector2[]>(), It.IsAny<Vector2[]>(), It.IsAny<string>(), It.IsAny<int?>()))
                .Returns(intersections);

            mockIntersectionClient.Setup(x => x.InsertAsync(It.IsAny<Intersection>())).ReturnsAsync(true);

            mockTradingSettingsClient.Setup(x => x.GetAsync(tradingsSettings.Symbol)).ReturnsAsync(tradingsSettings);

            mockOrderClient.Setup(x => x.FindOpenOrders(tradingsSettings.Symbol)).ReturnsAsync(new List<Order>()
            {
                new Order { Status = OrderStatus.New }
            });  

            await SUT.ExecuteAsync(tradingsSettings);

            mockIntersectionClient.Setup(x => x.ExtractFromChart(It.IsAny<Vector2[]>(), It.IsAny<Vector2[]>(), It.IsAny<string>(), It.IsAny<int?>()))
                .Returns(intersections);

            mockOrderClient.Verify(x => x.CreateAsync(It.IsAny<Order>()), Times.Never);
        }
    }
}