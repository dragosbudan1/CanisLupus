using System;

namespace CanisLupus.Common.Models
{
    public class Vector2
    {
        public Vector2()
        {
        }

        public Vector2(decimal x, decimal y)
        {
            X = x;
            Y = y;
        }

        public decimal X { get; set; }
        public decimal Y { get; set; }

        public static Vector2 Lerp(Vector2 wmaCurrent, Vector2 wmaNext, decimal v)
        {
            System.Numerics.Vector2 v1 = new System.Numerics.Vector2((float)wmaCurrent.X, (float)wmaCurrent.Y);
            System.Numerics.Vector2 v2 = new System.Numerics.Vector2((float)wmaNext.X, (float)wmaNext.Y);
        
            var returnVector = (System.Numerics.Vector2.Lerp(v1, v2, (float)v));

            return new Vector2((decimal)returnVector.X, (decimal)returnVector.Y);
        }
    }
}