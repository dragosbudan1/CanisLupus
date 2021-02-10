using System.Collections.Generic;
using System.Threading.Tasks;
using CanisLupus.Worker;
using CanisLupus.Worker.Algorithms;
using CanisLupus.Worker.Events;
using CanisLupus.Worker.Exchange;
using CanisLupus.Common.Models;
using CanisLupus.Worker.Trader;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;


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

            SUT = new MarketMakerHandler(
                mockBinanceClient.Object,
                mockEventPublisher.Object,
                mockWMACalculator.Object,
                mockIntersectionClient.Object,
                mockTradingClient.Object,
                mockOrderClient.Object
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

            mockIntersectionClient.Setup(x => x.ExtractFromChart(It.IsAny<List<CandleRawData>>(), It.IsAny<Vector2[]>(), It.IsAny<Vector2[]>(), It.IsAny<int?>()))
                .Returns(intersections);
            
            mockIntersectionClient.Setup(x => x.InsertAsync(It.Is<Intersection>(s => s.Point.Y == newestIntersection.Point.Y && s.Type == newestIntersection.Type)))
                .ReturnsAsync(true);

            await SUT.ExecuteAsync(new System.Threading.CancellationToken());

            mockIntersectionClient.Verify(x => x.InsertAsync(It.Is<Intersection>(s =>
                s.Point == newestIntersection.Point && s.Type == s.Type && s.Id == null && s.Status == IntersectionStatus.New)), Times.Once);
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

            mockIntersectionClient.Setup(x => x.ExtractFromChart(It.IsAny<List<CandleRawData>>(),
                It.IsAny<Vector2[]>(), It.IsAny<Vector2[]>(), It.IsAny<int?>()))
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

            await SUT.ExecuteAsync(new System.Threading.CancellationToken());

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

            mockIntersectionClient.Setup(x => x.ExtractFromChart(It.IsAny<List<CandleRawData>>(),
                It.IsAny<Vector2[]>(), It.IsAny<Vector2[]>(), It.IsAny<int?>()))
                .Returns(intersections);

            mockIntersectionClient.Setup(x => x.FindByIntersectionDetails(It.Is<Intersection>(s =>
                s.Point == existingIntersection.Point &&
                s.Type == existingIntersection.Type)))
                    .ReturnsAsync(existingIntersection);

            var updatedIntersection = existingIntersection;
            updatedIntersection.Status = IntersectionStatus.Finished;

            mockIntersectionClient.Setup(x => x.UpdateAsync(It.Is<Intersection>(s => s.Id == dbGeneratedId)))
                .ReturnsAsync(updatedIntersection);

            await SUT.ExecuteAsync(new System.Threading.CancellationToken());

            mockIntersectionClient.Verify(x => x.InsertAsync(It.IsAny<Intersection>()), Times.Never);
            mockIntersectionClient.Verify(x => x.UpdateAsync(It.Is<Intersection>(s =>
                s.Id == dbGeneratedId &&
                s.Point == updatedIntersection.Point &&
                s.Type == updatedIntersection.Type &&
                s.Status == IntersectionStatus.Finished)), Times.Once);
        }

        [Test]
        public async Task TestNewIntersectionAndNoOpenOrders()
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

            mockIntersectionClient.Setup(x => x.ExtractFromChart(It.IsAny<List<CandleRawData>>(), It.IsAny<Vector2[]>(), It.IsAny<Vector2[]>(), It.IsAny<int?>()))
                .Returns(intersections);

            await SUT.ExecuteAsync(new System.Threading.CancellationToken());

            mockIntersectionClient.Setup(x => x.ExtractFromChart(It.IsAny<List<CandleRawData>>(),
                It.IsAny<Vector2[]>(), It.IsAny<Vector2[]>(), It.IsAny<int?>()))
                .Returns(intersections);

            // mockTradingClient.Verify(x => x.CreateBuyOrder(OrderType.Buy, It.IsAny<decimal>(), 100), Times.Once());
        }

        [Test]
        public async Task TestCancelOpenBuyOrderWhenNewDownwardIntersection()
        {
            var dbGeneratedId = "id123";
            var openOrder = new Order()
            {
                Amount = 10.0m,
                Spend = 100.0m,
                Price = 0.10m,
                Status = OrderStatus.Open,
                Type = OrderType.Buy,
                Id = dbGeneratedId
            };


            var newestIntersection = new Intersection
            {
                Id = null,
                Point = new Vector2(58, 0.0745123m),
                Status = null,
                Type = IntersectionType.Downward
            };

            var intersections = new List<Intersection>() { newestIntersection };
            var orders = new List<Order>() { openOrder };

            mockIntersectionClient.Setup(x => x.ExtractFromChart(It.IsAny<List<CandleRawData>>(), It.IsAny<Vector2[]>(), It.IsAny<Vector2[]>(), It.IsAny<int?>()))
                .Returns(intersections);

            mockOrderClient.Setup(x => x.FindOpenOrders())
                .ReturnsAsync(orders);

            mockIntersectionClient.Setup(x => x.InsertAsync(It.Is<Intersection>(s => s.Point.Y == newestIntersection.Point.Y && s.Type == newestIntersection.Type)))
                .ReturnsAsync(true);

            await SUT.ExecuteAsync(new System.Threading.CancellationToken());

            mockOrderClient.Verify(x => x.CancelOrder(It.Is<Order>(m => m.Id == openOrder.Id)), Times.Once);
        }

        [Test]
        public async Task TestCancelOpenSellOrderWhenNewUpwardIntersection()
        {
            var dbGeneratedId = "id123";
            var openOrder = new Order()
            {
                Amount = 10.0m,
                Spend = 100.0m,
                Price = 0.10m,
                Status = OrderStatus.Open,
                Type = OrderType.Sell,
                Id = dbGeneratedId
            };


            var newestIntersection = new Intersection
            {
                Id = null,
                Point = new Vector2(58, 0.0745123m),
                Status = null,
                Type = IntersectionType.Upward
            };

            var intersections = new List<Intersection>() { newestIntersection };
            var orders = new List<Order>() { openOrder };

            mockIntersectionClient.Setup(x => x.ExtractFromChart(It.IsAny<List<CandleRawData>>(), It.IsAny<Vector2[]>(), It.IsAny<Vector2[]>(), It.IsAny<int?>()))
                .Returns(intersections);

            mockOrderClient.Setup(x => x.FindOpenOrders())
                .ReturnsAsync(orders);

            mockIntersectionClient.Setup(x => x.InsertAsync(It.Is<Intersection>(s => s.Point.Y == newestIntersection.Point.Y && s.Type == newestIntersection.Type)))
                .ReturnsAsync(true);

            await SUT.ExecuteAsync(new System.Threading.CancellationToken());

            mockOrderClient.Verify(x => x.CancelOrder(It.Is<Order>(m => m.Id == openOrder.Id)), Times.Once);
        }

        [Test]
        public async Task TestNoActiveTradesAndNoOpenBuyOrdersAndNewestIntersectionIsUpwardCreateBuyOrder()
        {
            mockOrderClient.Setup(x => x.FindOpenOrders())
                .ReturnsAsync(new List<Order>());

            mockTradingClient.Setup(x => x.FindActiveTrades())
                .ReturnsAsync(new List<Trade>());

            var newestIntersection = new Intersection
            {
                Id = null,
                Point = new Vector2(58, 0.0745123m),
                Status = null,
                Type = IntersectionType.Upward
            };

            var intersections = new List<Intersection>() { newestIntersection };

            mockIntersectionClient.Setup(x => x.InsertAsync(It.Is<Intersection>(s => s.Point.Y == newestIntersection.Point.Y && s.Type == newestIntersection.Type)))
                .ReturnsAsync(true);

            var price = newestIntersection.Point.Y;
            var spend = 100.0m;
            var newBuyOrder = new Order()
            {
                Type = OrderType.Buy,
                Price = price,
                Spend = spend,
                ProfitPercentage = 2,
                Amount = price * spend               
            };

            mockIntersectionClient.Setup(x => x.ExtractFromChart(It.IsAny<List<CandleRawData>>(), It.IsAny<Vector2[]>(), It.IsAny<Vector2[]>(), It.IsAny<int?>()))
                .Returns(intersections);

            await SUT.ExecuteAsync(new System.Threading.CancellationToken());

            mockOrderClient.Verify(x => x.CreateOrder(It.Is<Order>(o => 
                o.Price == newBuyOrder.Price &&
                o.Amount == newBuyOrder.Amount &&
                o.ProfitPercentage == newBuyOrder.ProfitPercentage &&
                o.Spend == newBuyOrder.Spend &&
                o.Type == newBuyOrder.Type)));
        }

        [Test]
        public async Task TestWhenActiveTradeAndProfitReached()
        {
            var price = 1.0m;
            var spend = 100;
            var linkedOrder = new Order()
            {
                Id = "1233",
                Amount = spend / price,
                Price = price,
                Spend = spend,
                ProfitPercentage = 2,
                Status = OrderStatus.Filled,
                Type = OrderType.Buy
            };

            var activeTrade = new Trade()
            {
                OrderId = linkedOrder.Id,
                Id = "122334",
                TradeStatus = TradeStatus.Active,
                TradeType = TradeType.Buy 
            };

            var activeTrades = new List<Trade>(){ activeTrade };

            mockTradingClient.Setup(x => x.FindActiveTrades())
                .ReturnsAsync(activeTrades);

            await SUT.ExecuteAsync(new System.Threading.CancellationToken());

            var sellOrder = new Order()
            {
                Type = OrderType.Sell,
                Price = linkedOrder.TargetPrice,
                Amount = linkedOrder.Amount,
                Spend = linkedOrder.Amount * linkedOrder.TargetPrice,
                Status = OrderStatus.Open
            };
            mockOrderClient.Verify(x => x.CreateOrder(It.Is<Order>(s => 
                s.Amount == sellOrder.Amount &&
                s.Price == sellOrder.Price &&
                s.Spend == sellOrder.Spend &&
                s.Status == sellOrder.Status)), Times.Once);
        }
    }


}