using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using CanisLupus.Worker;
using CanisLupus.Worker.Algorithms;
using CanisLupus.Worker.Events;
using CanisLupus.Worker.Exchange;
using CanisLupus.Worker.Models;
using CanisLupus.Worker.Trader;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace CanisLupus.Tests
{
    public class MarketHandlerTests
    {
        private List<IntersectionResult> intersections = new List<IntersectionResult>();
        private IMarketMakerHandler SUT;

        private Mock<ILogger<MarketMakerHandler>> mockLogger;
        private Mock<IBinanceClient> mockBinanceClient;
        private Mock<IEventPublisher> mockEventPublisher;
        private Mock<IIntersectionFinder> mockIntersectionFinder;
        private Mock<IWeightedMovingAverageCalculator> mockWMACalculator;
        private Mock<ITradingClient> mockTradingClient;

        [SetUp]
        public void Setup()
        {
            mockLogger = new Mock<ILogger<MarketMakerHandler>>();
            mockBinanceClient = new Mock<IBinanceClient>();
            mockEventPublisher = new Mock<IEventPublisher>();
            mockIntersectionFinder = new Mock<IIntersectionFinder>();
            mockTradingClient = new Mock<ITradingClient>();
            mockWMACalculator = new Mock<IWeightedMovingAverageCalculator>();

            var newIntersection = new IntersectionResult
            {
                Id = null,
                Point = new Vector2(57, 0.0745123f),
                Status = null,
                Type = IntersectionType.Upward
            };

            var intersections = new List<IntersectionResult>
            {
                newIntersection
            };

            mockIntersectionFinder.Setup(x => x.Find(It.IsAny<List<CandleRawData>>(), It.IsAny<Vector2[]>(), It.IsAny<Vector2[]>(), It.IsAny<int?>()))
                .Returns(intersections);

            mockTradingClient.Setup(x => x.CreateBuyOrder(OrderType.Buy, (decimal)newIntersection.Point.Y, 100.0m))
                .ReturnsAsync(new OrderResult{ Success = true});

            SUT = new MarketMakerHandler(
                mockLogger.Object,
                mockBinanceClient.Object,
                mockEventPublisher.Object,
                mockWMACalculator.Object,
                mockIntersectionFinder.Object,
                mockTradingClient.Object
            );
        }

        [Test]
        public async Task TestNewIntersectionAndNoOpenOrders()
        {
            await SUT.ExecuteAsync(new System.Threading.CancellationToken());

            mockTradingClient.Verify(x => x.CreateBuyOrder(OrderType.Buy, It.IsAny<decimal>(), 100), Times.Once());
        }
    }


}