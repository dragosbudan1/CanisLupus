using CanisLupus.Common.Models;
using CanisLupus.Worker.Trader;

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
        

        public static object ToLoggable(this Order order)
        {
            return new {
                order.Type,
                order.Spend,
                order.Amount
            };
        }
    }
}