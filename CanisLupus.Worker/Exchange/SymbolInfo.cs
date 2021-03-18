namespace CanisLupus.Worker.Exchange
{
    public class ExchangeInfo
    {
        public System.Collections.Generic.List<SymbolInfo> Symbols { get; set; }
    }

    public class SymbolInfo
    {
        public string Symbol { get; set; }
        public bool IsSpotTradingAllowed { get; set; }
    }
}