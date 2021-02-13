using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CanisLupus.Common.Models
{
    public class Trade
    {
        public Trade()
        {
            Id = Guid.NewGuid().ToString();
        }

        public string Id { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string OrderId { get; set; }
        public string CloseOrderId { get; set; }
        public decimal StartSpend { get; set; }
        public decimal CloseSpend { get; set; }
        public decimal NetGain { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [BsonRepresentation(BsonType.String)]
        public TradeType TradeType { get; set; }
        
        [JsonConverter(typeof(StringEnumConverter))]
        [BsonRepresentation(BsonType.String)]
        public TradeStatus TradeStatus { get; set; }
    }

    public enum TradeType
    {
        Buy = 0,
        Sell
    }

    public enum TradeStatus
    {
        Active = 0,
        Finished
    }
}