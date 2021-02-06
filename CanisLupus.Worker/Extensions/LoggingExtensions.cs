using CanisLupus.Worker.Models;

namespace CanisLupus.Worker.Extensions
{
    public static class LoggingExtensions
    {
        public static object ToLoggable(this CandleRawData symbolCandle)
        {
            return new 
            {
                symbolCandle.Close,
                symbolCandle.CloseTime,
                symbolCandle.High,
                symbolCandle.Low,
                symbolCandle.NumberOfTrades,
                symbolCandle.Open,
                symbolCandle.OpenTime,
                symbolCandle.QuoteAssetVolume
            };
        }

        public static object ToLoggableMin(this CandleRawData symbolCandle)
        {
            return new 
            {
                symbolCandle.OpenTime,
                symbolCandle.Bottom,
                symbolCandle.Top,
                symbolCandle.NumberOfTrades,
                symbolCandle.Orientation,
                symbolCandle.Close,
                symbolCandle.Open
            };
        }
    }
}