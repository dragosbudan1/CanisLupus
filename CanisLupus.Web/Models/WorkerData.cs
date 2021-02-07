using System;

namespace CanisLupus.Web.Models
{
    public class WorkerData
    {
        public double Top { get; set; }
        public double Bottom { get; set; }
        public DateTime OpenTime { get; set; }
        public int Orientation { get; set; }
        public double Wma { get; set; }
        public double Smma { get; internal set; }
    }
}