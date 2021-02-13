using System;

namespace CanisLupus.Common.Models
{
    public class Wallet
    {
        public string Id { get; set; }
        public string Currency => "USDT";
        public decimal Amount { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdateDate { get; set; }
    }
}