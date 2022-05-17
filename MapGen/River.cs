using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapGen
{
    public enum Direction
    {
        Left,
        Right,
        Top,
        Bottom
    }
    public class River
    {
		public int Length;
		public List<Tile> Tiles;
		public int ID;

		public int Intersections;
		public float TurnCount;
		public Direction CurrentDirection;

		public River(int id)
		{
			ID = id;
			Tiles = new List<Tile>();
		}

		public void AddTile(Tile tile)
		{
			tile.SetRiverPath(this);
			Tiles.Add(tile);
		}
	}
	public class RiverGroup
	{
		public List<River> Rivers = new List<River>();
	}
}
