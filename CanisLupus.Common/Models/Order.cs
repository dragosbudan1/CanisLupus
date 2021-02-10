using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CanisLupus.Common.Models
{
    public class Order
    {
        
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id {get; set;}
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public OrderType Type { get; set; }
        public decimal Amount { get; set; }
        public decimal Spend { get; set; }
        public decimal Price { get; set; }
        public decimal TargetPrice => Price + (Price * (ProfitPercentage / 100));
        public decimal ProfitPercentage { get; set; }
        public OrderStatus Status { get; set; }
    }

    public enum OrderStatus
    {
        Open = 0,
        Partial,
        Filled,
        Cancelled
    }
}