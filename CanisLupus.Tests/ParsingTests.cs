using System;
using CanisLupus.Common.Models;
using CanisLupus.Worker.Exchange;
using NUnit.Framework;

namespace CanisLupus.Tests
{
    public class ParsingTests
    {
        [Test]
        public void TestParsingStatusNew()
        {
            var status = "NEW";

            var result = BinanceHelpers.MapToOrderStatus(status);

            Assert.AreEqual(result, OrderStatus.New);
        }

        [Test]
        public void TestParsingStatusCanceled()
        {
            var status = "CANCELED";

            var result = BinanceHelpers.MapToOrderStatus(status);

            Assert.AreEqual(result, OrderStatus.Cancelled);
        }

    }
}