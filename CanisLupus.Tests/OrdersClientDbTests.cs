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

        // [TearDown]
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

            await SUT.CreateOrder(order);

            var openOrder = (await SUT.FindOpenOrders()).FirstOrDefault();

            var result = await SUT.CancelOrder(openOrder);

            Assert.IsNotNull(result);
            Assert.AreEqual(result.Id, openOrder.Id);
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
                Spend = 1000
            };

            var result = await SUT.CreateOrder(order);

            Assert.IsNotNull(result);
            Assert.AreEqual(result.Type, order.Type);
        }

        [Test]
        public async Task UpdateIntersectionTest()
        {
            
        }
    }
}