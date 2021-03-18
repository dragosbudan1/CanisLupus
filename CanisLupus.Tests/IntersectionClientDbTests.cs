using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CanisLupus.Common.Database;
using CanisLupus.Common.Models;
using CanisLupus.Worker.Algorithms;
using CanisLupus.Worker.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace CanisLupus.Tests
{
    public class IntersectionClientDbTests
    {
        private MongoDbClient dbClient;
        private IIntersectionClient SUT;
        private Intersection intersection;

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
            SUT = new IntersectionClient(new Mock<IEventPublisher>().Object,
                dbClient);
        }

        [TearDown]
        public async Task TearDown()
        {
            var collection = dbClient.GetCollection<Intersection>(IntersectionClient.IntersectionsCollectionName);
            Expression<Func<Intersection, bool>> filter = m => (m.Id != null);
            await collection.DeleteManyAsync(filter);
        }

        [Test]
        public async Task FindIntersectionTest()
        {
            intersection = new Intersection()
            {
                Point = new Vector2
                {
                    X = 0.123456123456m,
                    Y = 0.123456123456m,
                },
                Status = IntersectionStatus.Active,
                Type = IntersectionType.Undefined,
                Symbol = "BTCUSDT"
            };

            var otherSymbolIntersection = new Intersection()
            {
                Point = new Vector2
                {
                    X = 0.123456123456m,
                    Y = 0.123456123456m,
                },
                Status = IntersectionStatus.Active,
                Type = IntersectionType.Undefined,
                Symbol = "ADAUSDT"
            };

            var otherInsertResult = await SUT.InsertAsync(otherSymbolIntersection); 
            var insertResult = await SUT.InsertAsync(intersection);
            var existingIntersection = await SUT.FindByIntersectionDetails(intersection);

            Assert.IsNotNull(insertResult);
            Assert.IsNotNull(existingIntersection);
            Assert.AreEqual(existingIntersection.Type, intersection.Type);
            Assert.AreEqual(existingIntersection.Point.Y, intersection.Point.Y);
            Assert.AreEqual(existingIntersection.Symbol, intersection.Symbol);
        }

        [Test]
        public async Task InsertIntersectionTest()
        {
            intersection = new Intersection()
            {
                Point = new Vector2
                {
                    X = 0.123456123456m,
                    Y = 0.123456123456m,
                },
                Status = IntersectionStatus.Active,
                Type = IntersectionType.Undefined,
                Symbol = "BTCUSDT"
            };

        
            var insertResult = await SUT.InsertAsync(intersection);
         
            Assert.IsTrue(insertResult);
        }

        [Test]
        public async Task UpdateIntersectionTest()
        {
            intersection = new Intersection()
            {
                Point = new Vector2
                {
                    X = 0.123456123456m,
                    Y = 0.123456123456m,
                },
                Status = IntersectionStatus.Active,
                Type = IntersectionType.Undefined,
                Symbol = "BTCUSDT"
            };

            var updatedIntersection = intersection;
            updatedIntersection.Status = IntersectionStatus.Old;
            updatedIntersection.Point.X = 12;

            var insertResult = await SUT.InsertAsync(intersection);
            var result = await SUT.UpdateAsync(updatedIntersection);

            Assert.IsNotNull(insertResult);
            Assert.IsNotNull(result);
            Assert.AreEqual(result.Type, updatedIntersection.Type);
            Assert.AreEqual(result.Status, updatedIntersection.Status);
            Assert.AreEqual(result.Point.X, updatedIntersection.Point.X);
            Assert.AreEqual(result.Symbol, updatedIntersection.Symbol);
        }
    }
}