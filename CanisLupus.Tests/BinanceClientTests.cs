using System;
using System.Linq;
using System.Threading.Tasks;
using CanisLupus.Common.Models;
using CanisLupus.Worker.Exchange;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NUnit.Framework;

// dotnet test --filter "FullyQualifiedName=CanisLupus.Tests.BinanceClientTests.TestCanCreateBinanceOrder"

namespace CanisLupus.Tests
{
    public class BinanceClientTests
    {
        private IBinanceClient SUT;

        [SetUp]
        public void Setup()
        {
            var config = new ConfigurationBuilder()
               .AddJsonFile("appsettings.json")
               .Build();

            var settings = config.GetSection("BinanceSettings")
                .Get<BinanceSettings>();

            var dbSettings = Options.Create<BinanceSettings>(settings);
            SUT = new BinanceClient(dbSettings);
        }

        [TearDown]
        public async Task Teardown()
        {
            await SUT.CancelAllOrders("TRXBNB");
        }

        [Test]
        public async Task TestCanCreateBinanceBuyOrder()
        {
            var req = new BinanceOrderRequest()
            {
                Symbol = "TRXBNB",
                Price = 0.001m,
                Quantity = 100m,
                Side = OrderSide.Buy
            };

            var result = await SUT.CreateOrder(req);

            Assert.IsNotNull(result);
            Assert.AreEqual(result.Symbol, req.Symbol);
            Assert.AreEqual(result.Price, req.Price);
            Assert.AreEqual(result.OrigQty, req.Quantity);
            Assert.AreEqual(result.Side, req.Side.ToString().ToUpper());
            Assert.AreEqual(result.ClientOrderId, req.ClientOrderId);
        }

        [Test]
        public async Task TestCanGetOpenOrders()
        {
            var req = new BinanceOrderRequest()
            {
                Symbol = "TRXBNB",
                Price = 0.001m,
                Quantity = 100m,
                Side = OrderSide.Buy
            };

            var buyOrder = await SUT.CreateOrder(req);

            var openOrders = await SUT.GetOpenOrders("TRXBNB");

            Assert.IsNotNull(openOrders);
            Assert.AreEqual(openOrders.Count, 1);
            Assert.AreEqual(openOrders.FirstOrDefault().ClientOrderId, req.ClientOrderId);
        }

        [Test]
        public async Task TestCancelAllOrders()
        {
            var req = new BinanceOrderRequest()
            {
                Symbol = "TRXBNB",
                Price = 0.001m,
                Quantity = 100m,
                Side = OrderSide.Buy
            };

            var req2 = new BinanceOrderRequest()
            {
                Symbol = "TRXBNB",
                Price = 0.001m,
                Quantity = 100m,
                Side = OrderSide.Buy
            };

            var buyOrder = await SUT.CreateOrder(req);
            var buyOrder2 = await SUT.CreateOrder(req2);
            var cancelledOrders = await SUT.CancelAllOrders("TRXBNB");

            Assert.IsNotNull(cancelledOrders);
            Assert.AreEqual(cancelledOrders.Count, 2);
            Assert.AreEqual(cancelledOrders.FirstOrDefault().OrigClientOrderId, buyOrder.ClientOrderId);
            Assert.AreEqual(cancelledOrders.LastOrDefault().OrigClientOrderId, buyOrder2.ClientOrderId);

            foreach (var order in cancelledOrders)
            {
                Assert.AreEqual(order.Symbol, "TRXBNB");
                Assert.AreEqual(order.Status, "CANCELED");
            }
        }

        [Test]
        public async Task TestCanCancelOrder()
        {
            var req = new BinanceOrderRequest()
            {
                Symbol = "TRXBNB",
                Price = 0.001m,
                Quantity = 100m,
                Side = OrderSide.Buy
            };

            var buyOrder = await SUT.CreateOrder(req);
            var cancelledOrder = await SUT.CancelOrder(req.Symbol, req.ClientOrderId);

            Assert.IsNotNull(cancelledOrder);
            Assert.AreEqual(cancelledOrder.OrigClientOrderId, req.ClientOrderId);
            Assert.AreEqual(cancelledOrder.Status, "CANCELED");
        }

        [Test]
        public async Task TestCanGetOrderById()
        {
            var req = new BinanceOrderRequest()
            {
                Symbol = "TRXBNB",
                Price = 0.001m,
                Quantity = 100m,
                Side = OrderSide.Buy
            };

            var buyOrder = await SUT.CreateOrder(req);
            var order = await SUT.GetOrder(req.Symbol, req.ClientOrderId);

            Assert.IsNotNull(order);
            Assert.AreEqual(order.Status, "NEW");
            Assert.AreEqual(order.ClientOrderId, req.ClientOrderId);
        }

        // [Test]
        // public async Task TestGenerateHMAC()
        // {
        //     var reqParam = "symbol=BTCUSDT&side=SELL&type=LIMIT&timeInForce=GTC&quantity=0.01&price=9000&newClientOrderId=my_order_id_1&recvWindow=50000&timestamp={0}";
        //     var key = "pWPnIbsNYQh1jvOrd6Dx9Cm85y4BzpTHx6jV417sAs4CzA0Mtv6AulfmJUoHrQba";
        //     var signature = SUT.GenerateHMAC256(reqParam, key);
        // }
    }
}