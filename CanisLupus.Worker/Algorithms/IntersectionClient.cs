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

namespace CanisLupus.Worker.Algorithms
{
    public interface IIntersectionClient
    {
        List<Intersection> ExtractFromChart(List<CandleRawData> candleData, Vector2[] allWmaData, Vector2[] allSmmaData, int? dataSetCount = null);
        Task<bool> InsertAsync(Intersection intersection);
        Task<Intersection> FindByIntersectionDetails(Intersection intersection);
        Task<bool> Update(Intersection intersection);
    }

    public class IntersectionClient : IIntersectionClient
    {
        private readonly ILogger<IntersectionClient> logger;
        private readonly IEventPublisher eventPublisher;
        private readonly IDbClient dbClient;

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
                    intersectionList.Add(new Intersection
                    {
                        Type = GetIntersectionType(smaNext, smaCurrent),
                        Point = new Vector2(i, diffList.Min(x => x.Y))
                    });
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
            var intersectionDb = intersection.ToIntersectionDb(); 
            var result = await dbClient.InsertAsync(intersection, "Intersections");

            return result != null;
        }

        public Task<Intersection> FindByIntersectionDetails(Intersection intersection)
        {
            throw new NotImplementedException();
        }


        public Task<bool> Update(Intersection intersection)
        {
            throw new NotImplementedException();
        }

        private IntersectionType GetIntersectionType(Vector2 smaNext, Vector2 smaCurrent)
        {
            var sign = Math.Sign(smaNext.Y - smaCurrent.Y);

            switch(sign)
            {
                case(-1):
                    return IntersectionType.Downward;
                case(1):
                    return IntersectionType.Upward;
                case(0):
                default:
                    return IntersectionType.Undefined;
            }
        }
    }
}