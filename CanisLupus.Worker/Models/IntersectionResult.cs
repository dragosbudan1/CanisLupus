using System.Numerics;

namespace CanisLupus.Worker.Models
{
    public enum IntersectionType
    {
        Upward = 0,
        Downward,
        Undefined
    }
    public class IntersectionResult
    {
        public Vector2 Point { get; set; }  
        public IntersectionType Type { get; set; }
    }
}