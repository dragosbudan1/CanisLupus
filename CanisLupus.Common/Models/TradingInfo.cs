using System.Collections.Generic;

namespace CanisLupus.Common.Models
{
    public class TradingInfo
    {
        public Wallet Wallet { get; set; }
        public List<Intersection> Intersections { get; set; }
        public List<Order> OpenOrders { get; set; }
    }
}