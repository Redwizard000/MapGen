using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapGen
{
    public class MapData
    {
        public float[,] Data;
        public float Min { get; set; }
        public float Max { get; set; }

        public MapData(int width, int height)
        {
            Data = new float[width, height];
            Min = float.MaxValue;
            Max = float.MinValue;

        }
    }
}
