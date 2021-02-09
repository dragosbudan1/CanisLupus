using System;

namespace CanisLupus.Web.Models
{
    public class WorkerData
    {
        public decimal Top { get; set; }
        public decimal Bottom { get; set; }
        public DateTime OpenTime { get; set; }
        public int Orientation { get; set; }
        public decimal Wma { get; set; }
        public decimal Smma { get; internal set; }
    }
}