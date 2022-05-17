using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapGen
{
    public static class TextureGenerator
    {
        //Height Map Colors
        private static Color DeepColor = Color.FromArgb(15, 30, 80);
        private static Color ShallowColor = Color.FromArgb(15, 40, 90);
        private static Color RiverColor = Color.FromArgb(30, 120, 200);
        private static Color SandColor = Color.FromArgb(198, 190, 31);
        private static Color GrassColor = Color.FromArgb(50, 220, 20);
        private static Color ForestColor = Color.FromArgb(16, 160, 0);
        private static Color RockColor = Color.Gray;
        private static Color SnowColor = Color.White;

        //biome map
        private static Color Ice = Color.White;
        private static Color Desert = Color.FromArgb(237,219,118);
        private static Color Savanna = Color.FromArgb(178,209,91);
        private static Color TropicalRainforest = Color.FromArgb(66, 123, 25);
        private static Color Tundra = Color.FromArgb(96, 131, 112);
        private static Color TemperateRainforest = Color.FromArgb(29, 73, 40);
        private static Color Grassland = Color.FromArgb(164, 225, 99);
        private static Color SeasonalForest = Color.FromArgb(73, 100, 35);
        private static Color BorealForest = Color.FromArgb(95, 115, 62);
        private static Color Woodland = Color.FromArgb(139, 175, 90);

        private static Color IceWater = Color.FromArgb(210, 255, 252);
        private static Color ColdWater = Color.FromArgb(119, 156, 213);
        private static Color RiverWater = Color.FromArgb(65, 110, 179);

        // Heat Map Colors
        private static Color Coldest = Color.FloralWhite;
        private static Color Colder = Color.LightSkyBlue;
        private static Color Cold = Color.LightSeaGreen;
        private static Color Warm = Color.LightGoldenrodYellow;
        private static Color Warmer = Color.OrangeRed;
        private static Color Warmest = Color.Red;

        //Moisture map
        private static Color Dryest = Color.DarkRed;
        private static Color Dryer = Color.OrangeRed;
        private static Color Dry = Color.Yellow;
        private static Color Wet = Color.LimeGreen;
        private static Color Wetter = Color.LightBlue;
        private static Color Wettest = Color.DarkBlue;

        public static Bitmap GetBiomeMapTexture(int width, int height, Tile[,] tiles)
        {
            Bitmap texture = new Bitmap(width, height);
            Color color = new Color();

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    BiomeType value = tiles[x, y].BiomeType;

                    switch (value)
                    {
                        case BiomeType.Ice:
                            color = Ice;
                            break;
                        case BiomeType.BorealForest:
                            color = BorealForest;
                            break;
                        case BiomeType.Desert:
                            color = Desert;
                            break;
                        case BiomeType.Grassland:
                            color = Grassland;
                            break;
                        case BiomeType.SeasonalForest:
                            color = SeasonalForest;
                            break;
                        case BiomeType.Tundra:
                            color = Tundra;
                            break;
                        case BiomeType.Savanna:
                            color = Savanna;
                            break;
                        case BiomeType.TemperateRainforest:
                            color = TemperateRainforest;
                            break;
                        case BiomeType.TropicalRainforest:
                            color = TropicalRainforest;
                            break;
                        case BiomeType.Woodland:
                            color = Woodland;
                            break;
                    }
                    texture.SetPixel(x, y, color);

                    // Water tiles
                    if (tiles[x, y].HeightType == HeightType.DeepWater)
                    {
                        float heatValue = tiles[x, y].HeatValue;
                        if (tiles[x, y].HeatType == HeatType.Coldest || tiles[x, y].HeatType == HeatType.Colder)
                        {
                            texture.SetPixel(x, y, ColdWater);
                        }
                        else
                        {
                            texture.SetPixel(x, y, DeepColor);
                        }
                    }
                    else if (tiles[x, y].HeightType == HeightType.ShallowWater)
                    {
                        float heatValue = tiles[x, y].HeatValue;
                        if (tiles[x, y].HeatType == HeatType.Coldest || tiles[x, y].HeatType == HeatType.Colder)
                        {
                            texture.SetPixel(x, y, IceWater);
                        }
                        else
                        {
                            texture.SetPixel(x, y, ShallowColor);
                        }
                    }

                    // draw rivers
                    if (tiles[x, y].HeightType == HeightType.River)
                    {
                        float heatValue = tiles[x, y].HeatValue;

                        if (tiles[x, y].HeatType == HeatType.Coldest)
                            texture.SetPixel(x, y, IceWater);
                        else if (tiles[x, y].HeatType == HeatType.Colder)
                            texture.SetPixel(x, y, ColdWater);
                        else if (tiles[x, y].HeatType == HeatType.Cold)
                            texture.SetPixel(x, y, RiverWater);
                        else
                            texture.SetPixel(x, y, ShallowColor);
                    }


                    // add a outline
                    if (tiles[x, y].HeightType >= HeightType.Shore && tiles[x, y].HeightType != HeightType.River)
                    {
                        if (tiles[x, y].BiomeBitmask != 15)
                        {
                            texture.SetPixel(x, y, Color.Black);
                        }
                    }
                }
            }

            return texture;
        }
        public static Bitmap GetHeightMapTexture(int width, int height, Tile[,] tiles)
        {
            Bitmap texture = new Bitmap(width, height);
            Color color = new Color();

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    double value = tiles[x, y].HeightValue;

                    //Set color
                    switch (tiles[x, y].HeightType)
                    {
                        case HeightType.DeepWater:
                            color = DeepColor;
                            break;
                        case HeightType.ShallowWater:
                            color = ShallowColor;
                            break;
                        case HeightType.Sand:
                            color = SandColor;
                            break;
                        case HeightType.Grass:
                            color = GrassColor;
                            break;
                        case HeightType.Forest:
                            color = ForestColor; 
                            break;
                        case HeightType.Rock:
                            color = RockColor;
                            break;
                        case HeightType.Snow:
                            color = SnowColor; 
                            break;
                        case HeightType.River:
                            color = RiverColor;
                            break;
                    }

                    texture.SetPixel(x ,y, color);

                    if (tiles[x, y].Bitmask !=15)
                    {
                        texture.SetPixel(x, y, Color.Black);
                    }
                }
            }

            return texture;
        }

        public static Bitmap GetMoistureMapTexture(int width, int height, Tile[,] tiles)
        {
            Bitmap texture = new Bitmap (width, height);
            Color color = new Color();

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    Tile t = tiles[x, y];

                    //Set Color
                    switch (t.MoistureType)
                    {
                        case MoistureType.Dryest:
                            color = Dryest;
                            break;
                        case MoistureType.Dryer:
                            color = Dryer;
                            break;
                        case MoistureType.Dry:
                            color= Dry;
                            break;
                        case MoistureType.Wet:
                            color = Wet;
                            break;
                        case MoistureType.Wetter:
                            color = Wetter;
                            break;
                        case MoistureType.Wettest:
                            color = Wettest;
                            break;
                    }

                    texture.SetPixel(x, y, color);

                }
            }
            return texture;
        }

        public static Bitmap GetHeatMapTexture(int width, int height, Tile[,] tiles)
        {
            Bitmap texture = new Bitmap(width,height);
            Color color = new Color();

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    //Set color 
                    switch (tiles[x, y].HeatType)
                    {
                        case HeatType.Coldest:
                            color = Coldest;
                            break;
                        case HeatType.Colder:
                            color = Colder;
                            break;
                        case HeatType.Cold:
                            color = Cold;
                            break;
                        case HeatType.Warm:
                            color = Warm;
                            break;
                        case HeatType.Warmer:
                            color = Warmer;
                            break;
                        case HeatType.Warmest:
                            color = Warmest;
                            break;
                    }
                    texture.SetPixel(x, y, color);

                    if (tiles[x, y].Bitmask != 15)
                    {
                        texture.SetPixel(x, y, Color.Black);
                    }
                }
            }

            return texture;
        }
    }
}
