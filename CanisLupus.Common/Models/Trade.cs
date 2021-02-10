using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CanisLupus.Common.Models
{
    public class Trade
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id {get; set;}
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public TradeStatus TradeStatus { get; set; }
        public string OrderId { get; set; }
        public TradeType TradeType { get; set; }
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