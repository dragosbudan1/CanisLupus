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
    public class WalletClientDbTests
    {
        private MongoDbClient dbClient;
        private IWalletClient SUT;

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
            SUT = new WalletClient(dbClient);
        }

        [TearDown]
        public async Task TearDown()
        {
            var collection = dbClient.GetCollection<Wallet>(WalletClient.WalletColectionName);
            Expression<Func<Wallet, bool>> filter = m => (m.Id != null);
            await collection.DeleteManyAsync(filter);
        }

        [Test]
        public async Task CreateWalletTest()
        {
            var result = await SUT.UpdateWallet("some_wallet", 1.0m);
            Assert.IsNotNull(result);
        }

        [Test]
        public async Task CreateDoubleWallet()
        {
            await SUT.UpdateWallet("some_wallet", 30);
            var result = await SUT.UpdateWallet("some_wallet", -40);
            Assert.IsNotNull(result);
        }
    }
}