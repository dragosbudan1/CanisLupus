using System;
using System.Numerics;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CanisLupus.Common.Models
{
    public enum IntersectionType
    {
        Upward = 0,
        Downward,
        Undefined
    }

    public enum IntersectionStatus
    {
        Old = 0,
        New,
        Active,
        Finished
    }
    public class Intersection
    {
        public Vector2 Point { get; set; }
        public IntersectionType Type { get; set; }
        public IntersectionStatus? Status { get; set; }

        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id {get; set;}
    }

    public class IntersectionDb
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id {get; set;}
        
        [BsonElement("x")]
        public decimal X { get; set; }

        [BsonElement("y")]
        public decimal Y { get; set; }

        [BsonElement("type")]
        public string Type { get; set; }

        [BsonElement("status")]
        public string Status { get; set; }

        [BsonElement("date")]
        public BsonDateTime Date { get; set; }
    }
}