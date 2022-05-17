using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using RcherNZ.AccidentalNoise;

namespace MapGen
{
    public class Generator
    {
        Random rnd = new Random();
        Random rnd2;
        int Seed;
        int Width = 512;
        int Height = 512;
        int TerrainOctaves = 6;
        double TerrainFrequency = 1.25;

        //height Map
        float DeepWater = 0.2f;
        float ShallowWater = 0.4f;
        float Sand = 0.5f;
        float Grass = 0.7f;
        float Forest = 0.8f;
        float Rock = 0.9f;

        //Heat Map
        int HeatOctaves = 4;
        double HeatFrequency = 3.0;
        float ColdestValue = 0.05f;
        float ColderValue = 0.18f;
        float ColdValue = 0.4f;
        float WarmValue = 0.6f;
        float WarmerValue = 0.8f;

        //Moisture
        int MoistureOctaves = 4;
        double MoistureFrequency = 3.0;
        float DryerValue = 0.27f;
        float DryValue = 0.4f;
        float WetValue = 0.6f;
        float WetterValue = 0.8f;
        float WettestValue = 0.9f;

        //Rivers
        int RiverCount = 40;
        float MinRiverHeight = 0.6f;
        int MaxRiverAttempts = 1000;
        int MinRiverTurns = 18;
        int MinRiverLength = 20;
        int MaxRiverIntersections = 2;

        ImplicitFractal HeightMap;
        ImplicitCombiner HeatMap;
        ImplicitFractal MoistureMap;

        MapData HeightData;
        MapData HeatData;
        MapData MoistureData;

        Tile[,] Tiles;

        List<TileGroup> Waters = new List<TileGroup>();
        List<TileGroup> Lands = new List<TileGroup>();

        List<River> Rivers = new List<River>();
        List<RiverGroup> RiverGroups = new List<RiverGroup>();

        public BiomeType[,] BiomeTable = new BiomeType[6, 6] {   
		//COLDEST        //COLDER          //COLD                  //HOT                          //HOTTER                       //HOTTEST
		{ BiomeType.Ice, BiomeType.Tundra, BiomeType.Grassland,    BiomeType.Desert,              BiomeType.Desert,              BiomeType.Desert },              //DRYEST
		{ BiomeType.Ice, BiomeType.Tundra, BiomeType.Grassland,    BiomeType.Desert,              BiomeType.Desert,              BiomeType.Desert },              //DRYER
		{ BiomeType.Ice, BiomeType.Tundra, BiomeType.Woodland,     BiomeType.Woodland,            BiomeType.Savanna,             BiomeType.Savanna },             //DRY
		{ BiomeType.Ice, BiomeType.Tundra, BiomeType.BorealForest, BiomeType.Woodland,            BiomeType.Savanna,             BiomeType.Savanna },             //WET
		{ BiomeType.Ice, BiomeType.Tundra, BiomeType.BorealForest, BiomeType.SeasonalForest,      BiomeType.TropicalRainforest,  BiomeType.TropicalRainforest },  //WETTER
		{ BiomeType.Ice, BiomeType.Tundra, BiomeType.BorealForest, BiomeType.TemperateRainforest, BiomeType.TropicalRainforest,  BiomeType.TropicalRainforest }   //WETTEST
	};

        public List<Bitmap> Start(int MapSeed = 0)
        {
            if (MapSeed == 0)
            { 
            Seed = rnd.Next(0, int.MaxValue);
            }
            else
            {
                Seed = MapSeed;
            }

            List<Bitmap> bitOutput = new List<Bitmap>();

            bitOutput.Add(new Bitmap(Width, Height)); //Height Map
            bitOutput.Add(new Bitmap(Width, Height)); //Heat Map
            bitOutput.Add(new Bitmap(Width, Height)); //Moisture Map
            bitOutput.Add(new Bitmap(Width, Height)); //Biome Map

            Initialize();
            GetData(HeightMap, ref HeightData);
            LoadTiles();
            
            UpdateNeighbors();

            GenerateRivers();
            BuildRiverGroups();
            DigRiverGroups();
            AdjustMoistureMap();

            UpdateBitmasks();
            FloodFill();

            GenerateBiomeMap();
            UpdateBiomeBitmask();


            bitOutput[0] = TextureGenerator.GetHeightMapTexture(Width, Height, Tiles);
            bitOutput[1] = TextureGenerator.GetHeatMapTexture(Width, Height, Tiles);
            bitOutput[2] = TextureGenerator.GetMoistureMapTexture(Width, Height, Tiles);
            bitOutput[3] = TextureGenerator.GetBiomeMapTexture(Width, Height, Tiles);

            return bitOutput;
        }

        private void Initialize()
        {
            rnd2 = new Random(Seed);
            //BasisType Gradient - Makes more solid larger landmass, Simplex is more chaotic, broken up.
            //FractalType HybridMulti is more smooth. RigedMilti makes large Pangia type worlds with lakes and no oceans. Billow is mostly ocean with small Islands. Multi is default.
            HeightMap = new ImplicitFractal(FractalType.Multi, BasisType.Simplex, InterpolationType.Quintic);
            HeightMap.Octaves = TerrainOctaves;
            HeightMap.Frequency = TerrainFrequency;
            HeightMap.Seed = Seed;

            // Initialize the Heat map
            ImplicitGradient gradient = new ImplicitGradient(1, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1);
            ImplicitFractal heatFractal = new ImplicitFractal(FractalType.Multi, BasisType.Simplex, InterpolationType.Quintic);
            heatFractal.Octaves = HeatOctaves;
            heatFractal.Frequency = HeatFrequency;
            heatFractal.Seed = Seed;

            HeatMap = new ImplicitCombiner(CombinerType.Multiply);
            HeatMap.AddSource(gradient);
            HeatMap.AddSource(heatFractal);

            //Initialize Moisture Map
            MoistureMap = new ImplicitFractal(FractalType.Multi, BasisType.Simplex, InterpolationType.Quintic);
            MoistureMap.Octaves = MoistureOctaves;
            MoistureMap.Frequency = MoistureFrequency;
            MoistureMap.Seed = Seed;


        }

        private void GetData(ImplicitModuleBase module, ref MapData mapData)
        {
            mapData = new MapData(Width, Height);
            HeatData = new MapData(Width, Height);
            MoistureData = new MapData(Width, Height);

            // loop through each x,y point - get height value
            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {

                    // Noise range
                    float x1 = 0, x2 = 2;
                    float y1 = 0, y2 = 2;
                    float dx = x2 - x1;
                    float dy = y2 - y1;

                    // Sample noise at smaller intervals
                    float s = x / (float)Width;
                    float t = y / (float)Height;

                    // Calculate our 4D coordinates
                    double nx = x1 + Math.Cos(s * 2 * Math.PI) * dx / (2 * Math.PI);
                    double ny = y1 + Math.Cos(t * 2 * Math.PI) * dy / (2 * Math.PI);
                    double nz = x1 + Math.Sin(s * 2 * Math.PI) * dx / (2 * Math.PI);
                    double nw = y1 + Math.Sin(t * 2 * Math.PI) * dy / (2 * Math.PI);

                    float heightValue = (float)HeightMap.Get(nx, ny, nz, nw);
                    float heatValue = (float)HeatMap.Get(nx, ny, nz, nw);
                    float moistureValue = (float)MoistureMap.Get(nx, ny, nz, nw);

                    // keep track of the max and min values found
                    if (heightValue > mapData.Max) mapData.Max = heightValue;
                    if (heightValue < mapData.Min) mapData.Min = heightValue;

                    if (heatValue > HeatData.Max) HeatData.Max = heatValue;
                    if (heatValue < HeatData.Min) HeatData.Min = heatValue;

                    if (moistureValue > MoistureData.Max) MoistureData.Max = moistureValue;
                    if (moistureValue < MoistureData.Min) MoistureData.Min = moistureValue;

                    mapData.Data[x, y] = heightValue;
                    HeatData.Data[x, y] = heatValue;
                    MoistureData.Data[x, y] = moistureValue;

                }
            }
        }
        private void AdjustMoistureMap()
        {
            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {

                    Tile t = Tiles[x, y];
                    if (t.HeightType == HeightType.River)
                    {
                        AddMoisture(t, (int)60);
                    }
                }
            }
        }
        private void AddMoisture(Tile t, float amount)
        {
            MoistureData.Data[t.X, t.Y] += amount;
            t.MoistureValue += amount;
            if (t.MoistureValue > 1)
                t.MoistureValue = 1;

            //set moisture type
            if (t.MoistureValue < DryerValue) t.MoistureType = MoistureType.Dryest;
            else if (t.MoistureValue < DryValue) t.MoistureType = MoistureType.Dryer;
            else if (t.MoistureValue < WetValue) t.MoistureType = MoistureType.Dry;
            else if (t.MoistureValue < WetterValue) t.MoistureType = MoistureType.Wet;
            else if (t.MoistureValue < WettestValue) t.MoistureType = MoistureType.Wetter;
            else t.MoistureType = MoistureType.Wettest;
        }
        private void AddMoisture(Tile t, int radius)
        {
            int startx = MathHelper.Mod(t.X - radius, Width);
            int endx = MathHelper.Mod(t.X + radius, Width);
            clsPoint center = new clsPoint(t.X, t.Y);
            int curr = radius;

            while (curr > 0)
            {

                int x1 = MathHelper.Mod(t.X - curr, Width);
                int x2 = MathHelper.Mod(t.X + curr, Width);
                int y = t.Y;

                double radius2 = Math.Pow(Convert.ToDouble(x1) - center.x,2) + Math.Pow(Convert.ToDouble(y) - center.y,2);

                AddMoisture(Tiles[x1, y], Convert.ToInt32(0.025f / Convert.ToInt32(radius2)));

                for (int i = 0; i < curr; i++)
                {
                    float  magnatude = new clsPoint(x1, MathHelper.Mod(y + i + 1, Height)).magnitude();
                    AddMoisture(Tiles[x1, MathHelper.Mod(y + i + 1, Height)], 0.025f / (center - new clsPoint(x1, MathHelper.Mod(y + i + 1, Height))).magnitude());
                    AddMoisture(Tiles[x1, MathHelper.Mod(y - (i + 1), Height)], 0.025f / (center - new clsPoint(x1, MathHelper.Mod(y - (i + 1), Height))).magnitude());

                    AddMoisture(Tiles[x2, MathHelper.Mod(y + i + 1, Height)], 0.025f / (center - new clsPoint(x2, MathHelper.Mod(y + i + 1, Height))).magnitude());
                    AddMoisture(Tiles[x2, MathHelper.Mod(y - (i + 1), Height)],0.025f / (center - new clsPoint(x2, MathHelper.Mod(y - (i + 1), Height))).magnitude());
                }
                curr--;
            }
        }
        // Build a Tile array from our data
        private void LoadTiles()
        {
            Tiles = new Tile[Width, Height];

            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    Tile t = new Tile();
                    t.X = x;
                    t.Y = y;

                    float value = HeightData.Data[x, y];
                    value = (value - HeightData.Min) / (HeightData.Max - HeightData.Min);

                    t.HeightValue = value;

                    //HeightMap Analyze
                    if (value < DeepWater)
                    {
                        t.HeightType = HeightType.DeepWater;
                        t.Collidable = false;
                    }
                    else if (value < ShallowWater)
                    {
                        t.HeightType = HeightType.ShallowWater;
                        t.Collidable = false;
                    }
                    else if (value < Sand)
                    {
                        t.HeightType = HeightType.Sand;
                        t.Collidable = true;
                    }
                    else if (value < Grass)
                    {
                        t.HeightType = HeightType.Grass;
                        t.Collidable = true;
                    }
                    else if (value < Forest)
                    {
                        t.HeightType = HeightType.Forest;
                        t.Collidable = true;
                    }
                    else if (value < Rock)
                    {
                        t.HeightType = HeightType.Rock;
                        t.Collidable = true;
                    }
                    else
                    {
                        t.HeightType = HeightType.Snow;
                        t.Collidable = true;
                    }

                    //adjust moisture based on height
                    if (t.HeightType == HeightType.DeepWater)
                    {
                        MoistureData.Data[t.X, t.Y] += 8f * t.HeightValue;
                    }
                    else if (t.HeightType == HeightType.ShallowWater)
                    {
                        MoistureData.Data[t.X, t.Y] += 3f * t.HeightValue;
                    }
                    else if (t.HeightType == HeightType.Shore)
                    {
                        MoistureData.Data[t.X, t.Y] += 1f * t.HeightValue;
                    }
                    else if (t.HeightType == HeightType.Sand)
                    {
                        MoistureData.Data[t.X, t.Y] += 0.25f * t.HeightValue;
                    }

                    //Moisture Map Analyze  
                    float moistureValue = MoistureData.Data[x, y];
                    moistureValue = (moistureValue - MoistureData.Min) / (MoistureData.Max - MoistureData.Min);
                    t.MoistureValue = moistureValue;

                    //set moisture type
                    if (moistureValue < DryerValue) t.MoistureType = MoistureType.Dryest;
                    else if (moistureValue < DryValue) t.MoistureType = MoistureType.Dryer;
                    else if (moistureValue < WetValue) t.MoistureType = MoistureType.Dry;
                    else if (moistureValue < WetterValue) t.MoistureType = MoistureType.Wet;
                    else if (moistureValue < WettestValue) t.MoistureType = MoistureType.Wetter;
                    else t.MoistureType = MoistureType.Wettest;

                    // Adjust Heat Map based on Height - Higher == colder
                    if (t.HeightType == HeightType.Forest)
                    {
                        HeatData.Data[t.X, t.Y] -= 0.1f * t.HeightValue;
                    }
                    else if (t.HeightType == HeightType.Rock)
                    {
                        HeatData.Data[t.X, t.Y] -= 0.25f * t.HeightValue;
                    }
                    else if (t.HeightType == HeightType.Snow)
                    {
                        HeatData.Data[t.X, t.Y] -= 0.4f * t.HeightValue;
                    }
                    else
                    {
                        HeatData.Data[t.X, t.Y] += 0.01f * t.HeightValue;
                    }

                    // Set heat value
                    float heatValue = HeatData.Data[x, y];
                    heatValue = (heatValue - HeatData.Min) / (HeatData.Max - HeatData.Min);
                    t.HeatValue = heatValue;

                    // set heat type
                    if (heatValue < ColdestValue) t.HeatType = HeatType.Coldest;
                    else if (heatValue < ColderValue) t.HeatType = HeatType.Colder;
                    else if (heatValue < ColdValue) t.HeatType = HeatType.Cold;
                    else if (heatValue < WarmValue) t.HeatType = HeatType.Warm;
                    else if (heatValue < WarmerValue) t.HeatType = HeatType.Warmer;
                    else t.HeatType = HeatType.Warmest;

                    Tiles[x, y] = t;
                }
            }
        }
        private Tile GetTop(Tile t)
        {
            return Tiles[t.X, MathHelper.Mod(t.Y - 1, Height)];
        }
        private Tile GetBottom(Tile t)
        {
            return Tiles[t.X, MathHelper.Mod(t.Y + 1, Height)];
        }
        private Tile GetLeft(Tile t)
        {
            return Tiles[MathHelper.Mod(t.X - 1, Width), t.Y];
        }
        private Tile GetRight(Tile t)
        {
            return Tiles[MathHelper.Mod(t.X + 1, Width), t.Y];
        }
        private void UpdateNeighbors()
        {
            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    Tile t = Tiles[x, y];

                    t.Top = GetTop(t);
                    t.Bottom = GetBottom(t);
                    t.Left = GetLeft(t);
                    t.Right = GetRight(t);
                }
            }
        }
        private void UpdateBitmasks()
        {
            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    Tiles[x, y].UpdateBitmask();
                }
            }
        }
        private void FloodFill()
        {
            // Use a stack instead of recursion
            Stack<Tile> stack = new Stack<Tile>();

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {

                    Tile t = Tiles[x, y];

                    //Tile already flood filled, skip
                    if (t.FloodFilled) continue;

                    // Land
                    if (t.Collidable)
                    {
                        TileGroup group = new TileGroup();
                        group.Type = TileGroupType.Land;
                        stack.Push(t);

                        while (stack.Count > 0)
                        {
                            FloodFill(stack.Pop(), ref group, ref stack);
                        }

                        if (group.Tiles.Count > 0)
                            Lands.Add(group);
                    }
                    // Water
                    else
                    {
                        TileGroup group = new TileGroup();
                        group.Type = TileGroupType.Water;
                        stack.Push(t);

                        while (stack.Count > 0)
                        {
                            FloodFill(stack.Pop(), ref group, ref stack);
                        }

                        if (group.Tiles.Count > 0)
                            Waters.Add(group);
                    }
                }
            }
        }


        private void FloodFill(Tile tile, ref TileGroup tiles, ref Stack<Tile> stack)
        {
            // Validate
            if (tile.FloodFilled)
                return;
            if (tiles.Type == TileGroupType.Land && !tile.Collidable)
                return;
            if (tiles.Type == TileGroupType.Water && tile.Collidable)
                return;

            // Add to TileGroup
            tiles.Tiles.Add(tile);
            tile.FloodFilled = true;

            // floodfill into neighbors
            Tile t = GetTop(tile);
            if (!t.FloodFilled && tile.Collidable == t.Collidable)
                stack.Push(t);
            t = GetBottom(tile);
            if (!t.FloodFilled && tile.Collidable == t.Collidable)
                stack.Push(t);
            t = GetLeft(tile);
            if (!t.FloodFilled && tile.Collidable == t.Collidable)
                stack.Push(t);
            t = GetRight(tile);
            if (!t.FloodFilled && tile.Collidable == t.Collidable)
                stack.Push(t);
        }

        private void GenerateRivers()
        {
            int attempts = 0;
            int rivercount = RiverCount;
            Rivers = new List<River>();

            // Generate some rivers
            while (rivercount > 0 && attempts < MaxRiverAttempts)
            {

                // Get a random tile
                int x = rnd2.Next(0, Width);
                int y = rnd2.Next(0, Height);
                Tile tile = Tiles[x, y];

                // validate the tile
                if (!tile.Collidable) continue;
                if (tile.Rivers.Count > 0) continue;

                if (tile.HeightValue > MinRiverHeight)
                {
                    // Tile is good to start river from
                    River river = new River(rivercount);

                    // Figure out the direction this river will try to flow
                    river.CurrentDirection = tile.GetLowestNeighbor();

                    // Recursively find a path to water
                    FindPathToWater(tile, river.CurrentDirection, ref river);

                    // Validate the generated river 
                    if (river.TurnCount < MinRiverTurns || river.Tiles.Count < MinRiverLength || river.Intersections > MaxRiverIntersections)
                    {
                        //Validation failed - remove this river
                        for (int i = 0; i < river.Tiles.Count; i++)
                        {
                            Tile t = river.Tiles[i];
                            t.Rivers.Remove(river);
                        }
                    }
                    else if (river.Tiles.Count >= MinRiverLength)
                    {
                        //Validation passed - Add river to list
                        Rivers.Add(river);
                        tile.Rivers.Add(river);
                        rivercount--;
                    }
                }
                attempts++;
            }
        }

        private void FindPathToWater(Tile tile, Direction direction, ref River river)
        {
            if (tile.Rivers.Contains(river))
                return;

            // check if there is already a river on this tile
            if (tile.Rivers.Count > 0)
                river.Intersections++;

            river.AddTile(tile);

            // get neighbors
            Tile left = GetLeft(tile);
            Tile right = GetRight(tile);
            Tile top = GetTop(tile);
            Tile bottom = GetBottom(tile);

            float leftValue = int.MaxValue;
            float rightValue = int.MaxValue;
            float topValue = int.MaxValue;
            float bottomValue = int.MaxValue;

            // query height values of neighbors
            if (left.GetRiverNeighborCount(river) < 2 && !river.Tiles.Contains(left))
                leftValue = left.HeightValue;
            if (right.GetRiverNeighborCount(river) < 2 && !river.Tiles.Contains(right))
                rightValue = right.HeightValue;
            if (top.GetRiverNeighborCount(river) < 2 && !river.Tiles.Contains(top))
                topValue = top.HeightValue;
            if (bottom.GetRiverNeighborCount(river) < 2 && !river.Tiles.Contains(bottom))
                bottomValue = bottom.HeightValue;

            // if neighbor is existing river that is not this one, flow into it
            if (bottom.Rivers.Count == 0 && !bottom.Collidable)
                bottomValue = 0;
            if (top.Rivers.Count == 0 && !top.Collidable)
                topValue = 0;
            if (left.Rivers.Count == 0 && !left.Collidable)
                leftValue = 0;
            if (right.Rivers.Count == 0 && !right.Collidable)
                rightValue = 0;

            // override flow direction if a tile is significantly lower
            if (direction == Direction.Left)
                if (Math.Abs(rightValue - leftValue) < 0.1f)
                    rightValue = int.MaxValue;
            if (direction == Direction.Right)
                if (Math.Abs(rightValue - leftValue) < 0.1f)
                    leftValue = int.MaxValue;
            if (direction == Direction.Top)
                if (Math.Abs(topValue - bottomValue) < 0.1f)
                    bottomValue = int.MaxValue;
            if (direction == Direction.Bottom)
                if (Math.Abs(topValue - bottomValue) < 0.1f)
                    topValue = int.MaxValue;

            // find mininum
            float min = Math.Min(Math.Min(Math.Min(leftValue, rightValue), topValue), bottomValue);

            // if no minimum found - exit
            if (min == int.MaxValue)
                return;

            //Move to next neighbor
            if (min == leftValue)
            {
                if (left.Collidable)
                {
                    if (river.CurrentDirection != Direction.Left)
                    {
                        river.TurnCount++;
                        river.CurrentDirection = Direction.Left;
                    }
                    FindPathToWater(left, direction, ref river);
                }
            }
            else if (min == rightValue)
            {
                if (right.Collidable)
                {
                    if (river.CurrentDirection != Direction.Right)
                    {
                        river.TurnCount++;
                        river.CurrentDirection = Direction.Right;
                    }
                    FindPathToWater(right, direction, ref river);
                }
            }
            else if (min == bottomValue)
            {
                if (bottom.Collidable)
                {
                    if (river.CurrentDirection != Direction.Bottom)
                    {
                        river.TurnCount++;
                        river.CurrentDirection = Direction.Bottom;
                    }
                    FindPathToWater(bottom, direction, ref river);
                }
            }
            else if (min == topValue)
            {
                if (top.Collidable)
                {
                    if (river.CurrentDirection != Direction.Top)
                    {
                        river.TurnCount++;
                        river.CurrentDirection = Direction.Top;
                    }
                    FindPathToWater(top, direction, ref river);
                }
            }
        }
        private void BuildRiverGroups()
        {
            //loop each tile, checking if it belongs to multiple rivers
            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    Tile t = Tiles[x, y];

                    if (t.Rivers.Count > 1)
                    {
                        // multiple rivers == intersection
                        RiverGroup group = null;

                        // Does a rivergroup already exist for this group?
                        for (int n = 0; n < t.Rivers.Count; n++)
                        {
                            River tileriver = t.Rivers[n];
                            for (int i = 0; i < RiverGroups.Count; i++)
                            {
                                for (int j = 0; j < RiverGroups[i].Rivers.Count; j++)
                                {
                                    River river = RiverGroups[i].Rivers[j];
                                    if (river.ID == tileriver.ID)
                                    {
                                        group = RiverGroups[i];
                                    }
                                    if (group != null) break;
                                }
                                if (group != null) break;
                            }
                            if (group != null) break;
                        }

                        // existing group found -- add to it
                        if (group != null)
                        {
                            for (int n = 0; n < t.Rivers.Count; n++)
                            {
                                if (!group.Rivers.Contains(t.Rivers[n]))
                                    group.Rivers.Add(t.Rivers[n]);
                            }
                        }
                        else   //No existing group found - create a new one
                        {
                            group = new RiverGroup();
                            for (int n = 0; n < t.Rivers.Count; n++)
                            {
                                group.Rivers.Add(t.Rivers[n]);
                            }
                            RiverGroups.Add(group);
                        }
                    }
                }
            }
        }

        private void DigRiverGroups()
        {
            for (int i = 0; i < RiverGroups.Count; i++)
            {

                RiverGroup group = RiverGroups[i];
                River longest = null;

                //Find longest river in this group
                for (int j = 0; j < group.Rivers.Count; j++)
                {
                    River river = group.Rivers[j];
                    if (longest == null)
                        longest = river;
                    else if (longest.Tiles.Count < river.Tiles.Count)
                        longest = river;
                }

                if (longest != null)
                {
                    //Dig out longest path first
                    DigRiver(longest);

                    for (int j = 0; j < group.Rivers.Count; j++)
                    {
                        River river = group.Rivers[j];
                        if (river != longest)
                        {
                            DigRiver(river, longest);
                        }
                    }
                }
            }
        }
        private void DigRiver(River river, River parent)
        {
            int intersectionID = 0;
            int intersectionSize = 0;

            // determine point of intersection
            for (int i = 0; i < river.Tiles.Count; i++)
            {
                Tile t1 = river.Tiles[i];
                for (int j = 0; j < parent.Tiles.Count; j++)
                {
                    Tile t2 = parent.Tiles[j];
                    if (t1 == t2)
                    {
                        intersectionID = i;
                        intersectionSize = t2.RiverSize;
                    }
                }
            }

            int counter = 0;
            int intersectionCount = river.Tiles.Count - intersectionID;
            int size = rnd2.Next(intersectionSize, 5);
            river.Length = river.Tiles.Count;

            // randomize size change
            int two = river.Length / 2;
            int three = two / 2;
            int four = three / 2;
            int five = four / 2;

            int twomin = two / 3;
            int threemin = three / 3;
            int fourmin = four / 3;
            int fivemin = five / 3;

            // randomize length of each size
            int count1 = rnd2.Next(fivemin, five);
            if (size < 4)
            {
                count1 = 0;
            }
            int count2 = count1 + rnd2.Next(fourmin, four);
            if (size < 3)
            {
                count2 = 0;
                count1 = 0;
            }
            int count3 = count2 + rnd2.Next(threemin, three);
            if (size < 2)
            {
                count3 = 0;
                count2 = 0;
                count1 = 0;
            }
            int count4 = count3 + rnd2.Next(twomin, two);

            // Make sure we are not digging past the river path
            if (count4 > river.Length)
            {
                int extra = count4 - river.Length;
                while (extra > 0)
                {
                    if (count1 > 0) { count1--; count2--; count3--; count4--; extra--; }
                    else if (count2 > 0) { count2--; count3--; count4--; extra--; }
                    else if (count3 > 0) { count3--; count4--; extra--; }
                    else if (count4 > 0) { count4--; extra--; }
                }
            }

            // adjust size of river at intersection point
            if (intersectionSize == 1)
            {
                count4 = intersectionCount;
                count1 = 0;
                count2 = 0;
                count3 = 0;
            }
            else if (intersectionSize == 2)
            {
                count3 = intersectionCount;
                count1 = 0;
                count2 = 0;
            }
            else if (intersectionSize == 3)
            {
                count2 = intersectionCount;
                count1 = 0;
            }
            else if (intersectionSize == 4)
            {
                count1 = intersectionCount;
            }
            else
            {
                count1 = 0;
                count2 = 0;
                count3 = 0;
                count4 = 0;
            }

            // dig out the river
            for (int i = river.Tiles.Count - 1; i >= 0; i--)
            {

                Tile t = river.Tiles[i];

                if (counter < count1)
                {
                    t.DigRiver(river, 4);
                }
                else if (counter < count2)
                {
                    t.DigRiver(river, 3);
                }
                else if (counter < count3)
                {
                    t.DigRiver(river, 2);
                }
                else if (counter < count4)
                {
                    t.DigRiver(river, 1);
                }
                else
                {
                    t.DigRiver(river, 0);
                }
                counter++;
            }
        }

        // Dig river
        private void DigRiver(River river)
        {
            int counter = 0;

            // How wide are we digging this river?
            int size = rnd2.Next(1, 5);
            river.Length = river.Tiles.Count;

            // randomize size change
            int two = river.Length / 2;
            int three = two / 2;
            int four = three / 2;
            int five = four / 2;

            int twomin = two / 3;
            int threemin = three / 3;
            int fourmin = four / 3;
            int fivemin = five / 3;

            // randomize lenght of each size
            int count1 = rnd2.Next(fivemin, five);
            if (size < 4)
            {
                count1 = 0;
            }
            int count2 = count1 + rnd2.Next(fourmin, four);
            if (size < 3)
            {
                count2 = 0;
                count1 = 0;
            }
            int count3 = count2 + rnd2.Next(threemin, three);
            if (size < 2)
            {
                count3 = 0;
                count2 = 0;
                count1 = 0;
            }
            int count4 = count3 + rnd2.Next(twomin, two);

            // Make sure we are not digging past the river path
            if (count4 > river.Length)
            {
                int extra = count4 - river.Length;
                while (extra > 0)
                {
                    if (count1 > 0) { count1--; count2--; count3--; count4--; extra--; }
                    else if (count2 > 0) { count2--; count3--; count4--; extra--; }
                    else if (count3 > 0) { count3--; count4--; extra--; }
                    else if (count4 > 0) { count4--; extra--; }
                }
            }

            // Dig it out
            for (int i = river.Tiles.Count - 1; i >= 0; i--)
            {
                Tile t = river.Tiles[i];

                if (counter < count1)
                {
                    t.DigRiver(river, 4);
                }
                else if (counter < count2)
                {
                    t.DigRiver(river, 3);
                }
                else if (counter < count3)
                {
                    t.DigRiver(river, 2);
                }
                else if (counter < count4)
                {
                    t.DigRiver(river, 1);
                }
                else
                {
                    t.DigRiver(river, 0);
                }
                counter++;
            }
        }
        public BiomeType GetBiomeType(Tile tile)
        {
            return BiomeTable[(int)tile.MoistureType, (int)tile.HeatType];
        }

        private void UpdateBiomeBitmask()
        {
            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    Tiles[x, y].UpdateBiomeBitmask();
                }
            }
        }

        private void GenerateBiomeMap()
        {
            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {

                    if (!Tiles[x, y].Collidable) continue;

                    Tile t = Tiles[x, y];
                    t.BiomeType = GetBiomeType(t);
                }
            }
        }
    }
}
