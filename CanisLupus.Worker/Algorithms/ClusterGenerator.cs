using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CanisLupus.Common.Models;
using NLog;

namespace CanisLupus.Worker.Algorithms 
{
    public interface IClusterGenerator
    {
        Task<List<Vector2>> GenerateClusters(List<CandleRawData> data, ClusterType type);
    }

    public class ClusterGenerator : IClusterGenerator
    {
        private readonly ILogger logger;
        public ClusterGenerator()
        {
            this.logger = LogManager.GetCurrentClassLogger();
        }

        public Task<List<Vector2>> GenerateClusters(List<CandleRawData> data, ClusterType type)
        {
            List<Vector2> clusters = new List<Vector2>();
            var rawData = MapToTimeOrderedVector2(data, type);

            IEnumerable<Vector2> sampleData = null;

            switch(type)
            {
                case ClusterType.High:
                sampleData = rawData.OrderByDescending(x => x.Y).Take(10);
                break;

                case ClusterType.Low:
                sampleData = rawData.OrderBy(x => x.Y).Take(10);
                break;
            }

            sampleData = sampleData.OrderBy(x => x.X);

            clusters = sampleData.ToList();

            return Task.FromResult(clusters);
        }

        public static List<Vector2> MapToTimeOrderedVector2(List<CandleRawData> data, ClusterType type)
        {
            var orderedData = data.OrderBy(x => x.OpenTime);
            var vectors = new List<Vector2>();
            int time = 0;
            foreach (var dataPoint in orderedData)
            {
                vectors.Add(new Vector2(time, (type == ClusterType.High ? dataPoint.Top : dataPoint.Bottom)));
                time++;
            }
            return vectors;
        }
    }

    public enum ClusterType
    {
        High = 0,
        Low
    }
}