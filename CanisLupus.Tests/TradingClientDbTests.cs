using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CanisLupus.Common.Database;
using CanisLupus.Common.Models;
using CanisLupus.Worker.Trader;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace CanisLupus.Tests
{
    public class TradingClientDbTests
    {
        private MongoDbClient dbClient;
        private ITradingClient SUT;
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
            SUT = new TradingClient(dbClient);
        }

        [TearDown]
        public async Task TearDown()
        {
            var collection = dbClient.GetCollection<Trade>(TradingClient.TradesCollectionName);
            Expression<Func<Trade, bool>> filter = m => (m.Id != null);
            await collection.DeleteManyAsync(filter);
        }

        [Test]
        public async Task CreateActiveTradeTests()
        {
            var trade = new Trade()
            {
                TradeType = TradeType.Buy,
                StartSpend = 200,
                Symbol = "BTCUSDT"
            };

            var result = await SUT.CreateActiveTrade(trade);

            Assert.IsNotNull(result);
            Assert.AreEqual(result.OrderId, trade.OrderId);
        }

        [Test]
        public async Task FindActiveTradesTest()
        {
            var trade = new Trade()
            {
                TradeType = TradeType.Buy,
                StartSpend = 200,
                Symbol = "BTCUSDT"
            };

            await SUT.CreateActiveTrade(trade);
            var result = await SUT.FindActiveTrades("BTCUSDT");

            Assert.IsNotNull(result);
            Assert.AreEqual(result.Count, 1);
            Assert.AreEqual(result[0].Symbol, "BTCUSDT");
        }

        [Test]
        public async Task CloseTradesTest()
        {
            var trade = new Trade()
            {
                TradeType = TradeType.Buy,
                StartSpend = 200,
                Symbol = "BTCUSDT"
            };

            await SUT.CreateActiveTrade(trade);

            var newTrade = (await SUT.FindActiveTrades("BTCUSDT")).FirstOrDefault();

            var order = new Order()
            {
                SpendAmount = 200,
                Side = OrderSide.Sell,
            };

            var result = await SUT.CloseTrade(trade.Id, order);

            Assert.IsNotNull(result);
        }


   
    }
}