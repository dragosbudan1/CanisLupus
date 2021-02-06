using System;

namespace CanisLupus.Worker.Models
{
    public class CandleRawData
    {
        public DateTime OpenTime { get; set; }
        public double Open { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double Close { get; set; }
        public DateTime CloseTime { get; set; }
        public string QuoteAssetVolume { get; set; }
        public long NumberOfTrades { get; set; }
        public string Volume { get; set; }
        public double Bottom => Math.Min(Open, Close);
        public double Top => Math.Max(Open, Close);
        public int Orientation => Math.Sign(Close - Open);
        public double Middle => (Top + Bottom) / 2;
    }
}