using System.Collections.Generic;

namespace CanisLupus.Common.Models
{
    public class ViewData
    {
        public List<CandleRawData> CandleData { get; set; }
        public List<Vector2> SmaData { get; set; }
        public List<Vector2> WmaData { get; set; }
        public List<string> TradingLogs { get; set; }
    }
}