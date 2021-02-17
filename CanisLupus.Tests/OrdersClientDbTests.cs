using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CanisLupus.Common.Database;
using CanisLupus.Common.Models;
using CanisLupus.Worker.Algorithms;
using CanisLupus.Worker.Events;
using CanisLupus.Worker.Trader;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
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

            var settings = config.GetSection("DbSettings")
                .Get<DbSettings>();

            var dbSettings = Options.Create<DbSettings>(settings);

            dbClient = new MongoDbClient(dbSettings);
            SUT = new OrderClient(dbClient);
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
                Amount = 12.456m,
                Price = 12.456m,
                ProfitPercentage = 2,
                Type = OrderType.Buy,
                Spend = 1000
            };

            var orderResult = await SUT.CreateOrder(order);
            var resultId = await SUT.UpdateOrderAsync(orderResult.Id, OrderStatus.Cancelled);
            var result = await SUT.FindOrderById(resultId);

            Assert.IsNotNull(result);
            Assert.AreEqual(result.Id, order.Id);
            Assert.AreEqual(result.Status, OrderStatus.Cancelled);
        }

        [Test]
        public async Task FillOrdersTest()
        {
            var order = new Order()
            {
                Amount = 12.456m,
                Price = 12.456m,
                ProfitPercentage = 2,
                Type = OrderType.Buy,
                Spend = 1000
            };

            var orderResult = await SUT.CreateOrder(order);
            var resultId = await SUT.UpdateOrderAsync(orderResult.Id, OrderStatus.Filled);
            var result = await SUT.FindOrderById(resultId);

            Assert.IsNotNull(result);
            Assert.AreEqual(result.Id, order.Id);
            Assert.AreEqual(result.Status, OrderStatus.Filled);
        }

        [Test]
        public async Task InsertOrderTest()
        {
            var order = new Order()
            {
                Amount = 12.456m,
                Price = 12.456m,
                ProfitPercentage = 2,
                Type = OrderType.Buy,
                Spend = 1000,
                StopLossPercentage = 10
            };

            var result = await SUT.CreateOrder(order);
            var findResult = await SUT.FindOrderById(result.Id);

            Assert.IsNotNull(findResult);
            Assert.AreEqual(findResult.Type, OrderType.Buy);
            Assert.AreEqual(findResult.Status, OrderStatus.New);
        }

        [Test]
        public async Task FindOpenOrdersTest()
        {
            var openOrder1 = new Order()
            {
                Amount = 12.456m,
                Price = 12.456m,
                ProfitPercentage = 2,
                Type = OrderType.Buy,
                Spend = 1000,
                StopLossPercentage = 10
            };

            var openOrder2 = new Order()
            {
                Amount = 12.456m,
                Price = 12.456m,
                ProfitPercentage = 2,
                Type = OrderType.Buy,
                Spend = 1000,
                StopLossPercentage = 10
            };

            var closedOrder = new Order()
            {
                Amount = 12.456m,
                Price = 12.456m,
                ProfitPercentage = 2,
                Type = OrderType.Buy,
                Spend = 1000,
                StopLossPercentage = 10
            };

            await SUT.CreateOrder(openOrder1);
            await SUT.CreateOrder(openOrder2);
            await SUT.CreateOrder(closedOrder);
            await SUT.UpdateOrderAsync(closedOrder.Id, OrderStatus.Cancelled);
            var orders = await SUT.FindOpenOrders();

            Assert.IsNotNull(orders);
            Assert.AreEqual(orders.Count, 2);
        }
    }
}