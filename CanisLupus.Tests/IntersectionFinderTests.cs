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

        [Test]
        public void TestSameDoubleIntersectionFiltering()
        {
             List<CandleRawData> candleData = null;
            Vector2[] allWmaData =
            {
                new Vector2 {X = 0, Y = 0.07948387644636015m},
                new Vector2 {X = 0, Y = 0.07949130917624521m},
                new Vector2 {X = 0, Y = 0.07949814540229885m},
                new Vector2 {X = 0, Y = 0.07950753591954023m}
            };
            Vector2[] allSmmaData =
            {
                new Vector2 { X = 0, Y = 0.07946116m },
                new Vector2 { X = 0, Y = 0.07949786666666667m },
                new Vector2 { X = 0, Y = 0.07951684666666667m},
                new Vector2 { X = 0, Y = 0.07959097333333333m }
        }; ;

            var result = finder.ExtractFromChart(candleData, allWmaData, allSmmaData);

            Assert.NotNull(result);
            Assert.IsNotEmpty(result);
            Assert.AreEqual(result.Count, 1);
            Assert.AreEqual(result[0].Type, IntersectionType.Upward);
        }
    }
}


// 9:
// bottom: 0.07955
// openTime: "2021-02-09T11:59:00+00:00"
// orientation: -1
// smma: 0.07946116
// top: 0.0795521
// wma: 0.07948387644636015
// __proto__: Object
// 40:
// bottom: 0.079519
// openTime: "2021-02-09T12:00:00+00:00"
// orientation: -1
// smma: 0.07949786666666667
// top: 0.0795563
// wma: 0.07949130917624521
// __proto__: Object
// 41:
// bottom: 0.0795323
// openTime: "2021-02-09T12:01:00+00:00"
// orientation: 1
// smma: 0.07951684666666667
// top: 0.0797176
// wma: 0.07949814540229885
// __proto__: Object
// 42:
// bottom: 0.0797533
// openTime: "2021-02-09T12:02:00+00:00"
// orientation: 1
// smma: 0.07959097333333333
// top: 0.0799405
// wma: 0.07950753591954023