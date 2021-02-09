using System.Collections.Generic;
using CanisLupus.Common.Models;
using NUnit.Framework;

namespace CanisLupus.Tests
{
    public class WeightedMovingAverageTests
    {
        List<CandleRawData> testData = new List<CandleRawData>(); 
        List<decimal> weights = new List<decimal>();

        [SetUp]
        public void Setup()
        {
            testData = new List<CandleRawData>
            {
                new CandleRawData { Close = 0.05614m },
                new CandleRawData { Close = 0.05514m },
                new CandleRawData { Close = 0.05414m },
                new CandleRawData { Close = 0.05214m },
                new CandleRawData { Close = 0.05114m },
                new CandleRawData { Close = 0.05014m },
                new CandleRawData { Close = 0.05314m },
                new CandleRawData { Close = 0.05274m },
                new CandleRawData { Close = 0.05684m },
                new CandleRawData { Close = 0.05190m },
            };

            weights = new List<decimal>
            {
                1m, 2m, 3m, 4m, 5m
            };
        }

        [Test]
        public void TestWma()
        {
            var wmaResults = new List<decimal>();
            for(int i = 0; i < testData.Count - 5; i++)
            {   
                decimal top = 0;
                decimal bottom = 0;
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