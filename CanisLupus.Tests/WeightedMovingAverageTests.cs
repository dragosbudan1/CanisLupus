using System.Collections.Generic;
using CanisLupus.Worker.Models;
using NUnit.Framework;

namespace CanisLupus.Tests
{
    public class WeightedMovingAverageTests
    {
        List<CandleRawData> testData = new List<CandleRawData>(); 
        List<double> weights = new List<double>();

        [SetUp]
        public void Setup()
        {
            testData = new List<CandleRawData>
            {
                new CandleRawData { Close = 0.05614 },
                new CandleRawData { Close = 0.05514 },
                new CandleRawData { Close = 0.05414 },
                new CandleRawData { Close = 0.05214 },
                new CandleRawData { Close = 0.05114 },
                new CandleRawData { Close = 0.05014 },
                new CandleRawData { Close = 0.05314 },
                new CandleRawData { Close = 0.05274 },
                new CandleRawData { Close = 0.05684 },
                new CandleRawData { Close = 0.05190 },
            };

            weights = new List<double>
            {
                1, 2, 3, 4, 5
            };
        }

        [Test]
        public void TestWma()
        {
            var wmaResults = new List<double>();
            for(int i = 0; i < testData.Count - 5; i++)
            {   
                double top = 0;
                double bottom = 0;
                for(int j = 0; j < weights.Count; j++)
                {
                    top += testData[j + i].Close * weights[j];
                    bottom += weights[j];
                }
                var wma = top / bottom;
                wmaResults.Add(wma);
            }

            Assert.IsNotEmpty(wmaResults);
        }
    }


}