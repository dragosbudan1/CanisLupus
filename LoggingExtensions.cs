namespace CanisLupus
{
    public static class LoggingExtensions
    {
        public static object ToLoggable(this SymbolCandle symbolCandle)
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
    }
}