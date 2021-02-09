namespace CanisLupus.Common.Models
{
    public class Order
    {
        public OrderType Type { get; set; }
        public decimal Amount { get; set; }
        public decimal Spend { get; set; }
        public decimal Price { get; set; }
    }
}