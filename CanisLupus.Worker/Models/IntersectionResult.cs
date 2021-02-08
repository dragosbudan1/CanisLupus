using System;
using System.Numerics;

namespace CanisLupus.Worker.Models
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
    public class IntersectionResult
    {
        public Vector2 Point { get; set; }
        public IntersectionType Type { get; set; }
        public IntersectionStatus? Status { get; set; }
        public Guid? Id {get; set;}
    }
}