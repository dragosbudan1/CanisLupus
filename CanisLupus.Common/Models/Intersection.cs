using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

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
        public Intersection()
        {
            Id = Guid.NewGuid().ToString();
        }

        public Vector2 Point { get; set; }
        public string Id { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [BsonRepresentation(BsonType.String)]
        public IntersectionType Type { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [BsonRepresentation(BsonType.String)]
        public IntersectionStatus? Status { get; set; }
        public string Symbol { get; set; }
    }
}