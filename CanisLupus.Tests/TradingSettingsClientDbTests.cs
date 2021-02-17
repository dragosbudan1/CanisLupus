using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CanisLupus.Common.Database;
using CanisLupus.Common.Models;
using CanisLupus.Worker.Account;
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
    public class TradingSettingsClientDbTests
    {
        private MongoDbClient dbClient;
        private ITradingSettingsClient SUT;
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
            SUT = new TradingSettingsClient(dbClient);
        }

        [TearDown]
        public async Task TearDown()
        {
            var collection = dbClient.GetCollection<TradingSettings>(TradingSettingsClient.TradingSettingsCollectionName);
            Expression<Func<TradingSettings, bool>> filter = m => (m.Id != null);
            await collection.DeleteManyAsync(filter);
        }

        [Test]
        public async Task CreateAccountSettings()
        {
            var tradingSettings = new TradingSettings();
            var result = await SUT.InsertOrUpdateAsync(tradingSettings);

            Assert.IsNotNull(result);
            Assert.AreEqual(result.TradingStatus, TradingStatus.Stopped);
        }

        [Test]
        public async Task GetAccountSettings()
        {
            var tradingSettings = new TradingSettings();
            await SUT.InsertOrUpdateAsync(tradingSettings);
            var result = await SUT.GetAsync();

            Assert.IsNotNull(result);
            Assert.AreEqual(result.TradingStatus, TradingStatus.Stopped);
            Assert.AreEqual(result.UserId, "dragos");

        }
    }
}