using System;
using System.Collections.Generic;
using System.Linq;
using CanisLupus.Worker.Events;
using CanisLupus.Worker.Extensions;
using CanisLupus.Common.Models;
using Newtonsoft.Json;
using System.Threading.Tasks;
using CanisLupus.Common.Database;
using System.Linq.Expressions;
using MongoDB.Driver;
using NLog;

namespace CanisLupus.Worker.Algorithms
{
    public interface IIntersectionClient
    {
        List<Intersection> ExtractFromChart(Vector2[] allWmaData, Vector2[] allSmmaData, string symbol, int? dataSetCount = null);
        Task<bool> InsertAsync(Intersection intersection);
        Task<Intersection> FindByIntersectionDetails(Intersection intersection);
        Task<Intersection> UpdateAsync(Intersection intersection);
        Task<List<Intersection>> FindIntersectionsByStatus(IntersectionStatus status);
    }

    public class IntersectionClient : IIntersectionClient
    {
        private readonly ILogger logger;
        private readonly IEventPublisher eventPublisher;
        private readonly IDbClient dbClient;
        public const string IntersectionsCollectionName = "Intersections";

        public IntersectionClient(IEventPublisher eventPublisher, IDbClient dbClient)
        {
            this.logger = LogManager.GetCurrentClassLogger();
            this.eventPublisher = eventPublisher;
            this.dbClient = dbClient;
        }

        public List<Intersection> ExtractFromChart(Vector2[] wmaData, Vector2[] smmaData, string symbol, int? dataSetCount = null)
        {

            if (dataSetCount.HasValue)
            {
                wmaData = wmaData.TakeLast(dataSetCount.Value).ToArray();
                smmaData = smmaData.TakeLast(dataSetCount.Value).ToArray();
            }
            // find intersection in current data sample
            var resolution = 100.0m;
            var intersectionList = new List<Intersection>();
            for (int i = 0; i < wmaData.Length - 1; i++)
            {
                var wmaCurrent = wmaData[i];
                var wmaNext = wmaData[i + 1];
                var smaCurrent = smmaData[i];
                var smaNext = smmaData[i + 1];
                var diffList = new List<Vector2>();
                for (int x = 0; x < resolution; x++)
                {
                    var wmaAvg = Vector2.Lerp(wmaCurrent, wmaNext, (decimal)(x + 1) / resolution);
                    var smaAvg = Vector2.Lerp(smaCurrent, smaNext, (decimal)(x + 1) / resolution);

                    var diff = Math.Abs((decimal)(wmaAvg.Y - smaAvg.Y));
                    if (diff < 0.00001m)
                    {
                        diffList.Add(wmaAvg);
                        //break;
                    }
                }
                if (diffList.Any())
                {
                    var intersection = new Intersection
                    {
                        Type = GetIntersectionType(smaNext, smaCurrent),
                        Point = new Vector2(i, diffList.Min(x => x.Y)),
                        Symbol = symbol
                    };

                    var closePreviousIntersection = intersectionList.FirstOrDefault(x => x.Point.X == i - 1);

                    if (intersection.Type != closePreviousIntersection?.Type)
                    {
                        intersectionList.Add(intersection);
                    }
                }
            }
            
            return intersectionList;
        }

        public async Task<bool> InsertAsync(Intersection intersection)
        {
            intersection.CreatedDate = DateTime.UtcNow;
            var result = await dbClient.InsertAsync(intersection, IntersectionsCollectionName);
            return result != null;
        }

        public async Task<Intersection> FindByIntersectionDetails(Intersection intersection)
        {
            var intersectionCollection = dbClient.GetCollection<Intersection>(IntersectionsCollectionName);
            Expression<Func<Intersection, bool>> filter = m => (m.Point.Y == intersection.Point.Y && m.Type == intersection.Type && m.Symbol == intersection.Symbol);
            var existingIntersections = (await intersectionCollection.FindAsync<Intersection>(filter));

            return existingIntersections?.FirstOrDefault();
        }


        public async Task<Intersection> UpdateAsync(Intersection intersection)
        {
            var collection = dbClient.GetCollection<Intersection>(IntersectionsCollectionName);
            Expression<Func<Intersection, bool>> filter = m => (m.Id == intersection.Id);

            var update = Builders<Intersection>.Update
                .Set(m => m.Status, intersection.Status)
                .Set(m => m.Point.X, intersection.Point.X)
                .Set(m => m.Type, intersection.Type)
                .Set(m => m.UpdatedDate, DateTime.Now);

            var updatedIntersection = await collection.FindOneAndUpdateAsync<Intersection>(filter, update);
            
            return updatedIntersection;
        }

        private IntersectionType GetIntersectionType(Vector2 smaNext, Vector2 smaCurrent)
        {
            var sign = Math.Sign(smaNext.Y - smaCurrent.Y);

            switch (sign)
            {
                case (-1):
                    return IntersectionType.Downward;
                case (1):
                    return IntersectionType.Upward;
                case (0):
                default:
                    return IntersectionType.Undefined;
            }
        }

        public async Task<List<Intersection>> FindIntersectionsByStatus(IntersectionStatus status)
        {
            var collection = dbClient.GetCollection<Intersection>(IntersectionsCollectionName);
            Expression<Func<Intersection, bool>> filter = m => (m.Status == status);

            var result = (await collection.FindAsync<Intersection>(filter)).ToList();

            return result;
            
        }
    }
}