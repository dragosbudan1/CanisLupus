using System;
using CanisLupus.Common.Models;

namespace CanisLupus.Worker.Exchange
{
    public class BinanceOrderRequest
    {
        public BinanceOrderRequest()
        {
            ClientOrderId = Guid.NewGuid().ToString();
        }
        public string ClientOrderId { get; set; }
        public string Symbol { get; set; }
        public decimal? Quantity { get; set; }
        public decimal? Price { get; set; }
        public OrderType Side { get; set; }
    }
}