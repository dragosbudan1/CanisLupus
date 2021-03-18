using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CanisLupus.Common.Database;
using CanisLupus.Common.Models;
using CanisLupus.Worker.Exchange;
using CanisLupus.Worker.Trader;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace CanisLupus.Tests
{
    public class OrderClientDbTests
    {
        private MongoDbClient dbClient;
        private IOrderClient SUT;
        private Order order;

        [SetUp]
        public void Setup()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            var dbSettings = Options.Create<DbSettings>(config.GetSection("DbSettings")
                .Get<DbSettings>());

            var binanceSettings = Options.Create<BinanceSettings>(config.GetSection("BinanceSettings")
                .Get<BinanceSettings>());

            var binanceClient = new BinanceClient(binanceSettings);

            dbClient = new MongoDbClient(dbSettings);
            SUT = new OrderClient(dbClient, binanceClient);
        }

        [TearDown]
        public async Task TearDown()
        {
            var collection = dbClient.GetCollection<Order>(OrderClient.OrdersCollectionName);
            Expression<Func<Order, bool>> filter = m => (m.Id != null);
            await collection.DeleteManyAsync(filter);
        }

        [Test]
        public async Task CancelOrdersTest()
        {
            var order = new Order()
            {
                Quantity = 1000m,
                Price = 0.0002m,
                ProfitPercentage = 2,
                Side = OrderSide.Buy,
                SpendAmount = 1000,
                StopLossPercentage = 10,
                Symbol = "TRXBNB",
            };
            var orderResult = await SUT.CreateAsync(order);
            var cancelled = await SUT.CancelAsync(order.Id);
            var result = await SUT.FindByIdAsync(order.Id);

            Assert.IsTrue(cancelled);
            Assert.IsNotNull(result);
            Assert.AreEqual(result.Id, order.Id);
            Assert.AreEqual(result.Status, OrderStatus.Cancelled);
        }

        [Test]
        public async Task InsertOrderTest()
        {
            var order = new Order()
            {
                Quantity = 1000m,
                Price = 0.0002m,
                ProfitPercentage = 2,
                Side = OrderSide.Buy,
                SpendAmount = 1000,
                StopLossPercentage = 10,
                Symbol = "TRXBNB",
            };

            var result = await SUT.CreateAsync(order);
            var findResult = await SUT.FindByIdAsync(result.Id);

            Assert.IsNotNull(findResult);
            Assert.AreEqual(findResult.Side, OrderSide.Buy);
            Assert.AreEqual(findResult.Status, OrderStatus.New);
        }

        [Test]
        public async Task FindOpenOrdersTest()
        {
            var order = new Order()
            {
                Quantity = 1000m,
                Price = 0.0002m,
                ProfitPercentage = 2,
                Side = OrderSide.Buy,
                SpendAmount = 1000,
                StopLossPercentage = 10,
                Symbol = "TRXBNB",
            };

            var order2 = new Order()
            {
                Quantity = 1000m,
                Price = 0.0002m,
                ProfitPercentage = 2,
                Side = OrderSide.Buy,
                SpendAmount = 1000,
                StopLossPercentage = 10,
                Symbol = "TRXBNB",
            };

            var order3 = new Order()
            {
                Quantity = 1000m,
                Price = 0.0002m,
                ProfitPercentage = 2,
                Side = OrderSide.Buy,
                SpendAmount = 1000,
                StopLossPercentage = 10,
                Symbol = "TRXBNB",
            };

            await SUT.CreateAsync(order);
            await SUT.CreateAsync(order2);
            await SUT.CreateAsync(order3);
            var cancelled = await SUT.CancelAsync(order.Id);
            var result = await SUT.FindOpenOrders(order.Symbol);

            Assert.IsNotNull(result);
            Assert.AreEqual(result.Count, 2);
            Assert.AreEqual(result[0].Id, order2.Id);
            Assert.AreEqual(result[1].Id, order3.Id);
        }
    }
}