using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CanisLupus.Common.Database;
using CanisLupus.Common.Models;
using CanisLupus.Worker.Account;
using CanisLupus.Worker.Exchange;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace CanisLupus.Tests
{
    public class TradingSettingsServiceDbTests
    {
        private MongoDbClient dbClient;
        private ITradingSettingsService SUT;
        private Order order;

        private Mock<IBinanceClient> mockBinanceClient;

        [SetUp]
        public void Setup()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            var settings = config.GetSection("DbSettings")
                .Get<DbSettings>();

            var dbSettings = Options.Create<DbSettings>(settings);

            mockBinanceClient = new Mock<IBinanceClient>();
            mockBinanceClient.Setup(x => x.ValidateSymbolInfo(It.IsAny<string>()))
                .ReturnsAsync(true); 

            dbClient = new MongoDbClient(dbSettings);
            SUT = new TradingSettingsService(dbClient, mockBinanceClient.Object);
        }

        [TearDown]
        public async Task TearDown()
        {
            var collection = dbClient.GetCollection<TradingSettings>(TradingSettingsService.TradingSettingsCollectionName);
            Expression<Func<TradingSettings, bool>> filter = m => (m.Id != null);
            await collection.DeleteManyAsync(filter);
        }

        [Test]
        public async Task CreateAccountSettings()
        {
            var tradingSettings = new TradingSettings()
            {
                Symbol = "DOGEUSDT"
            };
            var result = await SUT.InsertOrUpdateAsync(tradingSettings);

            Assert.IsNotNull(result);
            Assert.AreEqual(result.TradingStatus, TradingStatus.Stopped);
            Assert.AreEqual(result.Symbol, tradingSettings.Symbol);
        }   

        [Test]
        public async Task GetAccountSettings()
        {
            var symbol = "DOGEUSDT";
            var tradingSettings = new TradingSettings()
            {
                Symbol = symbol
            };
            await SUT.InsertOrUpdateAsync(tradingSettings);

            var updatedSettings = tradingSettings;
            updatedSettings.ProfitPercentage = 10;
            await SUT.InsertOrUpdateAsync(updatedSettings);
            var result = await SUT.GetAsync(symbol);

            Assert.IsNotNull(result);
            Assert.AreEqual(result.TradingStatus, TradingStatus.Stopped);
            Assert.AreEqual(result.Id, tradingSettings.Id);
            Assert.AreEqual(result.Symbol, symbol);
            Assert.AreEqual(result.ProfitPercentage, updatedSettings.ProfitPercentage);
        }

        [Test]
        public async Task GetAllAccountSettings()
        {
            var symbol = "DOGEUSDT";
            var symbol2 = "ADAUSDT";
            var tradingSettings = new TradingSettings()
            {
                Symbol = symbol
            };
            await SUT.InsertOrUpdateAsync(tradingSettings);

            var updatedSettings = tradingSettings;
            updatedSettings.ProfitPercentage = 10;
            await SUT.InsertOrUpdateAsync(updatedSettings);
            var settings2 = new TradingSettings()
            {
                Symbol = symbol2
            };
            await SUT.InsertOrUpdateAsync(settings2);
            var result = await SUT.GetAllAsync();

            Assert.IsNotNull(result);
            Assert.AreEqual(result.Count, 2);
            Assert.AreEqual(result[0].TradingStatus, TradingStatus.Stopped);
            Assert.AreEqual(result[0].Id, tradingSettings.Id);
            Assert.AreEqual(result[0].Symbol, symbol);
            Assert.AreEqual(result[0].ProfitPercentage, updatedSettings.ProfitPercentage);
            Assert.AreEqual(result[1].TradingStatus, TradingStatus.Stopped);
            Assert.AreEqual(result[1].Id, settings2.Id);
            Assert.AreEqual(result[1].Symbol, symbol2);
        }

        [Test]
        public async Task TestDeleteSettings()
        {
            var symbol = "DOGEUSDT";
            var symbol2 = "ADAUSDT";
            var tradingSettings = new TradingSettings()
            {
                Symbol = symbol
            };
            await SUT.InsertOrUpdateAsync(tradingSettings);

            var updatedSettings = tradingSettings;
            updatedSettings.ProfitPercentage = 10;
            await SUT.InsertOrUpdateAsync(updatedSettings);
            var settings2 = new TradingSettings()
            {
                Symbol = symbol2
            };
            await SUT.InsertOrUpdateAsync(settings2);
            var resultDelete = await SUT.DeleteAsync(symbol2);
            var result = await SUT.GetAllAsync();

            Assert.IsNotNull(result);
            Assert.IsTrue(resultDelete);
            Assert.AreEqual(result.Count, 1);
            Assert.AreEqual(result[0].TradingStatus, TradingStatus.Stopped);
            Assert.AreEqual(result[0].Id, tradingSettings.Id);
            Assert.AreEqual(result[0].Symbol, symbol);
            Assert.AreEqual(result[0].ProfitPercentage, updatedSettings.ProfitPercentage);

        }
    }
}