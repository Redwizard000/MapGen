using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapGen
{
    class clsPoint
    {
        public float x;
        public float y;

        public clsPoint()
        {

        }
        public clsPoint(float X, float Y)
        {
            x = X;
            y = Y;
        }

        public float magnitude()
        {
            return (float)Math.Sqrt(x * x + y * y);
        }
        public static clsPoint operator -(clsPoint a, clsPoint b) { return new clsPoint(a.x - b.x, a.y - b.y); }
        public static clsPoint operator /(clsPoint a, clsPoint b) { return new clsPoint(a.x / b.x, a.y / b.y); }
        public static clsPoint operator /(clsPoint a, float d) { return new clsPoint(a.x / d, a.y / d); }
    }
}
