using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using CanisLupus.Worker.Models;
using Microsoft.Extensions.Logging;

namespace CanisLupus.Worker.Algorithms
{
    public interface ISwingPointsGenerator
    {
        Task<List<Vector2>> GeneratePoints(List<CandleRawData> data);
    }

    public class SwingPointsGenerator : ISwingPointsGenerator
    {
        private readonly ILogger<SwingPointsGenerator> logger;
        public SwingPointsGenerator(ILogger<SwingPointsGenerator> logger)
        {
            this.logger = logger;
        }

        public Task<List<Vector2>> GeneratePoints(List<CandleRawData> data)
        {
            var orderedData = MapToTimeOrderedVector2(data);

            //take 5 point
            // 0 and 4 are feet
            // find mid between 1, 2, 3
            // angle between 0, mid, 4

            var minPointsNeeded = 5;

            var swingPoints = new List<Vector2>();
            for (int i = 0; i < orderedData.Count - 1; i++)
            {
                if (i > minPointsNeeded)
                {
                    var dataSet = orderedData.SkipWhile(x => x.X < i - minPointsNeeded)
                                            .TakeWhile(x => x.X < i)
                                            .ToArray();
                    var midPoint = new Vector2((dataSet[1].X + dataSet[2].X + dataSet[3].X) / 3, (dataSet[1].Y + dataSet[2].Y + dataSet[3].Y) / 3);
                    var angle = findAngle(dataSet[0], midPoint, dataSet[4]);

                    logger.LogInformation("DataSet: {data1}, {midpoint}, {data3} - {angle}", dataSet[0], midPoint, dataSet[4], angle);

                    if (angle < 120)
                    {
                        swingPoints.AddRange(dataSet);
                    }
                }

            }

            return Task.FromResult(swingPoints);
        }

        public static List<Vector2> MapToTimeOrderedVector2(List<CandleRawData> data)
        {
            var orderedData = data.OrderBy(x => x.OpenTime);
            var vectors = new List<Vector2>();
            int time = 0;
            foreach (var dataPoint in orderedData)
            {
                vectors.Add(new Vector2(time, (float)dataPoint.Top));
                time++;
            }
            return vectors;
        }

        // Center point is p1; angle returned in radians
        private double findAngle(Vector2 p0, Vector2 p1, Vector2 p2)
        {
            var a = Math.Pow(p1.X - p0.X, 2) + Math.Pow(p1.Y - p0.Y, 2);
            var b = Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2);
            var c = Math.Pow(p2.X - p0.X, 2) + Math.Pow(p2.Y - p0.Y, 2);
            return Math.Acos((a + b - c) / Math.Sqrt(4 * a * b));
        }
    }
}