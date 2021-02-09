using System;

namespace CanisLupus.Common.Models
{
    public class CandleRawData
    {
        public DateTime OpenTime { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public DateTime CloseTime { get; set; }
        public string QuoteAssetVolume { get; set; }
        public long NumberOfTrades { get; set; }
        public string Volume { get; set; }
        public decimal Bottom => Math.Min(Open, Close);
        public decimal Top => Math.Max(Open, Close);
        public int Orientation => Math.Sign(Close - Open);
        public decimal Middle => (Top + Bottom) / 2;
    }
}