using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CanisLupus.Worker.Algorithms;
using CanisLupus.Worker.Events;
using CanisLupus.Worker.Models;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace CanisLupus.Tests
{
    public class IntersectionFinderTests
    {
        IIntersectionFinder finder;

        [SetUp]
        public void Setup()
        {
            finder = new IntersectionFinder(new Mock<ILogger<IntersectionFinder>>().Object,
                new Mock<IEventPublisher>().Object);


        }

        [Test]
        public void TestWma()
        {
            List<CandleRawData> candleData = null;
            Vector2[] allWmaData = { new Vector2 { X = 0, Y = 0.064223f }, new Vector2 { X = 0, Y = 0.064123f } };
            Vector2[] allSmmaData = { new Vector2 { X = 0, Y = 0.064123f }, new Vector2 { X = 0, Y = 0.064323f } }; ;

            var result = finder.Find(candleData, allWmaData, allSmmaData);

            Assert.NotNull(result);
            Assert.IsNotEmpty(result);
            Assert.AreEqual(result.Count, 1);
            Assert.AreEqual(result.FirstOrDefault().Type, IntersectionType.Upward);
        }

        [Test]
        public void TestWma2()
        {
            List<CandleRawData> candleData = null;
            Vector2[] allWmaData = { new Vector2 { X = 0, Y = 0.07201624661684036f }, new Vector2 { X = 0, Y = 0.07202435284852982f }, new Vector2 { X = 0, Y = 0.0720202699303627f } };
            Vector2[] allSmmaData = { new Vector2 { X = 0, Y = 0.07254194468259811f }, new Vector2 { X = 0, Y = 0.07200126349925995f }, new Vector2 { X = 0, Y = 0.07132037729024887f } }; ;

            var result = finder.Find(candleData, allWmaData, allSmmaData);

            Assert.NotNull(result);
            Assert.IsNotEmpty(result);
            Assert.AreEqual(result.Count, 1);
            Assert.AreEqual(result.FirstOrDefault().Type, IntersectionType.Downward);
        }


        [Test]
        public void TestWma3()
        {
            List<CandleRawData> candleData = null;
            Vector2[] allWmaData =
            {
                new Vector2 {X = 0, Y = 0.07258408516645432f},
                new Vector2 {X = 0, Y = 0.07260379195213318f},
                new Vector2 {X = 0, Y = 0.07263562828302383f},
                new Vector2 {X = 0, Y = 0.07265979796648026f}
            };
            Vector2[] allSmmaData =
            {
                new Vector2 { X = 0, Y = 0.07261385023593903f },
                 new Vector2 { X = 0, Y = 0.07257463783025742f },
                 new Vector2 { X = 0, Y = 0.0728851929306984f },
                 new Vector2 { X = 0, Y = 0.0729970633983612f }
        }; ;

            var result = finder.Find(candleData, allWmaData, allSmmaData);

            Assert.NotNull(result);
            Assert.IsNotEmpty(result);
            Assert.AreEqual(result.Count, 2);
            Assert.AreEqual(result[0].Type, IntersectionType.Downward);
            Assert.AreEqual(result[1].Type, IntersectionType.Upward);
        }
    }
}