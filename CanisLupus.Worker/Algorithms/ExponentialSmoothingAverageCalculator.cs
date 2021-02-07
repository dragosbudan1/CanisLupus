using System.Collections.Generic;
using System.Numerics;
using CanisLupus.Worker.Models;

namespace CanisLupus.Worker.Algorithms
{
    public interface IExponentialSmoothingAverageCalculator
    {
        List<Vector2> Calculate(List<CandleRawData> data);
    }

    public class ExponentialSmoothingAverageCalculator : IExponentialSmoothingAverageCalculator
    {
        public List<Vector2> Calculate(List<CandleRawData> data)
        {
            throw new System.NotImplementedException();
        }
    }
}