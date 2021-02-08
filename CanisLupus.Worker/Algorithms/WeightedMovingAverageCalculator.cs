using System.Collections.Generic;
using System.Numerics;
using CanisLupus.Worker.Models;

namespace CanisLupus.Worker.Algorithms
{
    public interface IWeightedMovingAverageCalculator
    {
        List<Vector2> Calculate(List<CandleRawData> data, int? dataSetCount);
    }

    public class WeightedMovingAverageCalculator : IWeightedMovingAverageCalculator
    {
        public List<Vector2> Calculate(List<CandleRawData> data, int? dataSetCount = null)
        {
            var weightsCount = data.Count - dataSetCount;
            var weights = new List<double>();
            for(int w = 0; w < weightsCount; w++)
            {
                weights.Add(w + 1);
            }

            var wmaResults = new List<Vector2>();
            for(int i = 0; i < data.Count - weightsCount; i++)
            {   
                double top = 0;
                double bottom = 0;
                for(int j = 0; j < weights.Count; j++)
                {
                    top += data[j + i].Close * weights[j];
                    bottom += weights[j];
                }
                var wma = top / bottom;
                wmaResults.Add(new Vector2(x: i, y: (float)wma));
            }

            return wmaResults;
        }
    }
}