using System;
using System.Threading.Tasks;
using CanisLupus.Common.Database;
using CanisLupus.Common.Models;
using CanisLupus.Worker.Algorithms;
using CanisLupus.Worker.Events;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace CanisLupus.Tests
{
    public class MongoDbTests
    {
        private MongoDbClient dbClient;
        private IIntersectionClient SUT;

        [SetUp]
        public void Setup()
        {
            dbClient = new MongoDbClient();
            SUT = new IntersectionClient(new Mock<ILogger<IntersectionClient>>().Object,
                new Mock<IEventPublisher>().Object,
                dbClient);
        }

        [Test]
        public async Task DbOperationsTests()
        {
            var intersection = new Intersection()
            {
                Point = new Vector2
                {
                    X = 0.123456123456m,
                    Y = 0.123456123456m,
                },
                Status = IntersectionStatus.Active,
                Type = IntersectionType.Undefined,
            };

            var intersectionDb = new IntersectionDb()
            {
                    X = 0.123456123456m,
                    Y = 0.123456123456m,
           
                Status = IntersectionStatus.Active.ToString(),
                Type = IntersectionType.Undefined.ToString(),
            };

            var insertResult = await SUT.InsertAsync(intersection);

            await dbClient.InsertAsync(intersectionDb, "Intersections");

            Assert.IsNotNull(insertResult);
        }
    }
}