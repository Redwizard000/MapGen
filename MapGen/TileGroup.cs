using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapGen
{
    public enum TileGroupType
    {
        Water,
        Land
    }
    public class TileGroup
    {
        public TileGroupType Type;
        public List<Tile> Tiles;

        public TileGroup()
        {
            Tiles = new List<Tile>();
        }
    }
}
