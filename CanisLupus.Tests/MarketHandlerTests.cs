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

        private Mock<ILogger<MarketMakerHandler>> mockLogger;
        private Mock<IBinanceClient> mockBinanceClient;
        private Mock<IEventPublisher> mockEventPublisher;
        private Mock<IIntersectionClient> mockIntersectionClient;
        private Mock<IWeightedMovingAverageCalculator> mockWMACalculator;
        private Mock<ITradingClient> mockTradingClient;

        [SetUp]
        public void Setup()
        {
            mockLogger = new Mock<ILogger<MarketMakerHandler>>();
            mockBinanceClient = new Mock<IBinanceClient>();
            mockEventPublisher = new Mock<IEventPublisher>();
            mockIntersectionClient = new Mock<IIntersectionClient>();
            mockTradingClient = new Mock<ITradingClient>();
            mockWMACalculator = new Mock<IWeightedMovingAverageCalculator>();

            SUT = new MarketMakerHandler(
                mockLogger.Object,
                mockBinanceClient.Object,
                mockEventPublisher.Object,
                mockWMACalculator.Object,
                mockIntersectionClient.Object,
                mockTradingClient.Object
            );
        }

        [TearDown]
        public void Teardown()
        {
        }

        [Test]
        public async Task TestAddNewIntersection()
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

            mockIntersectionClient.Verify(x => x.InsertAsync(It.Is<Intersection>(s =>
                s.Point == newIntersection.Point && s.Type == s.Type && s.Id == null && s.Status == IntersectionStatus.New)), Times.Once);
        }

        [Test]
        public async Task TestUpdateNewToOldIntersection()
        {
            var existingIntersection = new Intersection
            {
                Id = null,
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
            
            var dbGeneratedId = "id123";

            mockIntersectionClient.Setup(x => x.FindByIntersectionDetails(It.Is<Intersection>(s => 
                s.Point == existingIntersection.Point && 
                s.Type == existingIntersection.Type)))
                    .ReturnsAsync(new Intersection
                    {
                        Id = dbGeneratedId,
                        Point = existingIntersection.Point,
                        Type = existingIntersection.Type,
                        Status = IntersectionStatus.New
                    });

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
            var existingIntersection = new Intersection
            {
                Id = null,
                Point = new Vector2(1, 0.0745123m),
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
            
            var dbGeneratedId = "id123";

            mockIntersectionClient.Setup(x => x.FindByIntersectionDetails(It.Is<Intersection>(s => 
                s.Point == existingIntersection.Point && 
                s.Type == existingIntersection.Type)))
                    .ReturnsAsync(new Intersection
                    {
                        Id = dbGeneratedId,
                        Point = existingIntersection.Point,
                        Type = existingIntersection.Type,
                        Status = IntersectionStatus.Old
                    });

            await SUT.ExecuteAsync(new System.Threading.CancellationToken());

            mockIntersectionClient.Verify(x => x.InsertAsync(It.IsAny<Intersection>()), Times.Never);
            mockIntersectionClient.Verify(x => x.UpdateAsync(It.Is<Intersection>(s => 
                s.Id == dbGeneratedId &&
                s.Point == existingIntersection.Point &&
                s.Type == existingIntersection.Type &&
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

            // mockTradingClient.Verify(x => x.CreateBuyOrder(OrderType.Buy, It.IsAny<decimal>(), 100), Times.Once());
        }
    }


}