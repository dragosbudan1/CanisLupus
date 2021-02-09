using System.Collections.Generic;
using System.Linq;
using CanisLupus.Worker.Algorithms;
using CanisLupus.Worker.Events;
using CanisLupus.Common.Models;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using CanisLupus.Common.Database;

namespace CanisLupus.Tests
{
    public class IntersectionFinderTests
    {
        IIntersectionClient finder;

        [SetUp]
        public void Setup()
        {
            finder = new IntersectionClient(new Mock<ILogger<IntersectionClient>>().Object,
                new Mock<IEventPublisher>().Object, new Mock<IDbClient>().Object);


        }

        [Test]
        public void TestCrossIntersection()
        {
            List<CandleRawData> candleData = null;
            Vector2[] allWmaData = { new Vector2 { X = 0, Y = 0.064223m }, new Vector2 { X = 0, Y = 0.064123m } };
            Vector2[] allSmmaData = { new Vector2 { X = 0, Y = 0.064123m }, new Vector2 { X = 0, Y = 0.064323m } }; ;

            var result = finder.ExtractFromChart(candleData, allWmaData, allSmmaData);

            Assert.NotNull(result);
            Assert.IsNotEmpty(result);
            Assert.AreEqual(result.Count, 1);
            Assert.AreEqual(result.FirstOrDefault().Type, IntersectionType.Upward);
        }

        [Test]
        public void TestMeetingPointIntersection()
        {
            List<CandleRawData> candleData = null;
            Vector2[] allWmaData = { new Vector2 { X = 0, Y = 0.07201624661684036m }, new Vector2 { X = 0, Y = 0.07202435284852982m }, new Vector2 { X = 0, Y = 0.0720202699303627m } };
            Vector2[] allSmmaData = { new Vector2 { X = 0, Y = 0.07254194468259811m }, new Vector2 { X = 0, Y = 0.07200126349925995m }, new Vector2 { X = 0, Y = 0.07132037729024887m } }; ;

            var result = finder.ExtractFromChart(candleData, allWmaData, allSmmaData);

            Assert.NotNull(result);
            Assert.IsNotEmpty(result);
            Assert.AreEqual(result.Count, 1);
            Assert.AreEqual(result.FirstOrDefault().Type, IntersectionType.Downward);
        }


        [Test]
        public void TestDoubleIntersection()
        {
            List<CandleRawData> candleData = null;
            Vector2[] allWmaData =
            {
                new Vector2 {X = 0, Y = 0.07258408516645432m},
                new Vector2 {X = 0, Y = 0.07260379195213318m},
                new Vector2 {X = 0, Y = 0.07263562828302383m},
                new Vector2 {X = 0, Y = 0.07265979796648026m}
            };
            Vector2[] allSmmaData =
            {
                new Vector2 { X = 0, Y = 0.07261385023593903m },
                new Vector2 { X = 0, Y = 0.07257463783025742m },
                new Vector2 { X = 0, Y = 0.0728851929306984m},
                new Vector2 { X = 0, Y = 0.0729970633983612m }
        }; ;

            var result = finder.ExtractFromChart(candleData, allWmaData, allSmmaData);

            Assert.NotNull(result);
            Assert.IsNotEmpty(result);
            Assert.AreEqual(result.Count, 2);
            Assert.AreEqual(result[0].Type, IntersectionType.Downward);
            Assert.AreEqual(result[1].Type, IntersectionType.Upward);
        }
    }
}