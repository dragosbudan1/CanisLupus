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
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}