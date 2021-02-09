using System;
using System.Collections.Generic;
using System.Linq;
using CanisLupus.Worker.Events;
using CanisLupus.Worker.Extensions;
using CanisLupus.Common.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Threading.Tasks;
using CanisLupus.Common.Database;
using System.Linq.Expressions;
using MongoDB.Driver;

namespace CanisLupus.Worker.Algorithms
{
    public interface IIntersectionClient
    {
        List<Intersection> ExtractFromChart(List<CandleRawData> candleData, Vector2[] allWmaData, Vector2[] allSmmaData, int? dataSetCount = null);
        Task<bool> InsertAsync(Intersection intersection);
        Task<Intersection> FindByIntersectionDetails(Intersection intersection);
        Task<Intersection> UpdateAsync(Intersection intersection);
    }

    public class IntersectionClient : IIntersectionClient
    {
        private readonly ILogger<IntersectionClient> logger;
        private readonly IEventPublisher eventPublisher;
        private readonly IDbClient dbClient;
        public const string IntersectionsCollectionName = "Intersections";

        public IntersectionClient(ILogger<IntersectionClient> logger, IEventPublisher eventPublisher, IDbClient dbClient)
        {
            this.logger = logger;
            this.eventPublisher = eventPublisher;
            this.dbClient = dbClient;
        }

        public List<Intersection> ExtractFromChart(List<CandleRawData> candleData, Vector2[] wmaData, Vector2[] smmaData, int? dataSetCount = null)
        {

            if (dataSetCount.HasValue)
            {
                wmaData = wmaData.TakeLast(dataSetCount.Value).ToArray();
                smmaData = smmaData.TakeLast(dataSetCount.Value).ToArray();
            }
            // find intersection in current data sample


            var resolution = 100.0m;
            var intersectionList = new List<Intersection>();
            Console.WriteLine($"{wmaData.Length} {smmaData.Length}");
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
                    //Console.WriteLine(diff);
                    if (diff < 0.00001m)
                    {
                        Console.WriteLine($"Diff {diff} Wma: {wmaAvg} Smma: {smaAvg}");
                        diffList.Add(wmaAvg);
                        //break;
                    }
                }
                if (diffList.Any())
                {
                    var intersection = new Intersection
                    {
                        Type = GetIntersectionType(smaNext, smaCurrent),
                        Point = new Vector2(i, diffList.Min(x => x.Y))
                    };

                    var closePreviousIntersection = intersectionList.FirstOrDefault(x => x.Point.X == i - 1);

                    if (intersection.Type != closePreviousIntersection?.Type)
                    {
                        intersectionList.Add(intersection);
                    }
                }
            }

            List<string> messages = new List<string>();
            if (!intersectionList.Any())
            {
                // log no interesections found
                var message = $"{DateTime.UtcNow} No intersections found between {candleData?.FirstOrDefault().OpenTime} - {candleData?.LastOrDefault().CloseTime}";
                logger.LogInformation(message);
                messages.Add(message);
            }
            else
            {
                foreach (var item in intersectionList)
                {
                    var message = $"{DateTime.UtcNow} Intersection found: {candleData?.ElementAt((int)item.Point.X)?.ToLoggableMin()}, sma: {item.Point.Y}, trend: {item.Type.ToString()}";
                    logger.LogInformation(message);
                    messages.Add(message);
                }
            }

            eventPublisher.PublishAsync(new EventRequest { QueueName = "tradingLogs", Value = JsonConvert.SerializeObject(messages) });

            return intersectionList;
        }

        public async Task<bool> InsertAsync(Intersection intersection)
        {
            var result = await dbClient.InsertAsync(intersection, IntersectionsCollectionName);
            return result != null;
        }

        public async Task<Intersection> FindByIntersectionDetails(Intersection intersection)
        {
            var intersectionCollection = dbClient.GetCollection<Intersection>(IntersectionsCollectionName);
            Expression<Func<Intersection, bool>> filter = m => (m.Point.Y == intersection.Point.Y && m.Type == intersection.Type);
            var existingIntersection = (await intersectionCollection.FindAsync<Intersection>(filter)).FirstOrDefault();

            return existingIntersection;
        }


        public async Task<Intersection> UpdateAsync(Intersection intersection)
        {
            var collection = dbClient.GetCollection<Intersection>(IntersectionsCollectionName);
            Expression<Func<Intersection, bool>> filter = m => (m.Id == intersection.Id);

            var update = Builders<Intersection>.Update
                .Set(m => m.Status, intersection.Status)
                .Set(m => m.Point.X, intersection.Point.X)
                .Set(m => m.Type, intersection.Type);

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
    }
}