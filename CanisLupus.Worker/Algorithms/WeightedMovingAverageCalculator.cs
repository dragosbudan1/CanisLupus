using System.Collections.Generic;
using System.Threading.Tasks;
using CanisLupus.Common.Models;
using CanisLupus.Worker.Events;
using Newtonsoft.Json;

namespace CanisLupus.Worker.Algorithms
{
    public interface IWeightedMovingAverageCalculator
    {
        Task<List<Vector2>> Calculate(List<CandleRawData> data, int? dataSetCount);
    }

    public class WeightedMovingAverageCalculator : IWeightedMovingAverageCalculator
    {
        public WeightedMovingAverageCalculator()
        {
        }

        public async Task<List<Vector2>> Calculate(List<CandleRawData> data, int? dataSetCount = 0)
        {
            var weightsCount = data.Count - dataSetCount;
            var weights = new List<decimal>();
            for(int w = 0; w < weightsCount; w++)
            {
                weights.Add(w + 1);
            }

            var wmaResults = new List<Vector2>();
            for(int i = 0; i < data.Count - weightsCount; i++)
            {   
                decimal top = 0;
                decimal bottom = 0;
                for(int j = 0; j < weights.Count; j++)
                {
                    top += data[j + i].Close * weights[j];
                    bottom += weights[j];
                }
                var wma = top / bottom;
                wmaResults.Add(new Vector2(x: i, y: wma));
            }

            return wmaResults;
        }
    }
}