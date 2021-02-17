using System;

namespace CanisLupus.Common.Models
{
    public enum TradingStatus
    {
        Stopped = 0,
        Inactive,
        Active
    }

    public class TradingSettings
    {
        public TradingSettings()
        {
            Id = Guid.NewGuid().ToString();
            UserId = "dragos";
        }

        public string Id { get; private set; }
        public TradingStatus TradingStatus { get; set; }
        public decimal SpendLimit { get; set; }
        public decimal TotalSpendLimit { get; set; }
        public decimal ProfitPercentage { get; set; }
        public decimal StopLossPercentage { get; set; }
        public string UserId { get; private set;}     
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}