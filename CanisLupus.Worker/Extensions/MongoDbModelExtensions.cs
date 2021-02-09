using System;
using CanisLupus.Common.Models;

namespace CanisLupus.Worker.Extensions
{
    public static class MongoDbModelExtensions
    {
        public static IntersectionDb ToIntersectionDb(this Intersection intersection)
        {
            return new IntersectionDb
            {
                Date = DateTime.UtcNow,
                Status = intersection.Status.ToString(),
                Type = intersection.Type.ToString(),
                X = (decimal)intersection.Point.X,
                Y = (decimal)intersection.Point.Y
            };
        } 
    }
}