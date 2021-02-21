using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CanisLupus.Common.Models
{
    public class Order
    {
        public Order()
        {
            Id = Guid.NewGuid().ToString();
        }

        public string Id { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public decimal Quantity { get; set; }
        public decimal SpendAmount { get; set; }
        public decimal Price { get; set; }
        public decimal TargetPrice => Price + (Price * (ProfitPercentage / 100));
        public decimal StopLossPrice => Price - (Price * (StopLossPercentage / 100));
        public decimal ProfitPercentage { get; set; }
        public decimal StopLossPercentage { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [BsonRepresentation(BsonType.String)]
        public OrderStatus Status { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [BsonRepresentation(BsonType.String)]
        public OrderSide Side { get; set; }
        public string Symbol { get; set; }
    }

    public enum OrderStatus
    {
        New = 0,
        Partial,
        Filled,
        Cancelled
    }
}