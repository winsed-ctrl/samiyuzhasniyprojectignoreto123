using System.IO;

namespace GromCore.Laser.Logic.Battle.Level.Factory
{
    using GromCore.Laser.Logic.Data;
    using GromCore.Laser.Titan.Json;
    using GromCore.Laser.Titan.Util;
    using System.Text;

    public static class TileMapFactory
    {
        public static TileMap CreateTileMap(string mapName)
        {
            PrintAllTiles(mapName);
            string[] map = MapLoader.InitWithMapFromDataTable(null, DataTables.Get(19), mapName);
            return new TileMap(map[0].Length,
                                    map.Length,
                                    string.Concat(map));

        }
        public static TileMap CreatePlayerMap(byte[] compressed)
        {
            ZLibHelper.DecompressInMySQLFormat(compressed, out byte[] output);
            string[] MapData = Encoding.UTF8.GetString(output, 0, output.Length).Split("\n");
            string Data = "";
            int.TryParse(MapData[2],out int Height);
            int.TryParse(MapData[1],out int Width);
            Data =String.Concat(MapData.Skip(3).Take(Height));
            // Console.WriteLine($"MapNme '{mapName}':");
            // Console.WriteLine($"Length: {map[0].Length}x{map.Length}");
            // Console.WriteLine("----------------------------------");

            for (int y = 0; y < MapData.Length; y++)
            {
                for (int x = 0; x < MapData[y].Length; x++)
                {
                    Console.Write(MapData[y][x] + " ");
                }
                // Console.WriteLine();
            }
            //LogicJSONObject obj = jsonObject.GetJSONObject(mapName);
            //if (obj == null) obj = jsonObject.GetJSONObject("Wanted_9");
            return new TileMap(Width, Height, Data); ;
        }

        public static void PrintAllTiles(string mapName)
        {
            return;
            try
            {
                string[] map = MapLoader.InitWithMapFromDataTable(null, DataTables.Get(19), mapName);

                if (map == null || map.Length == 0)
                {
                    // Console.WriteLine("Map not found or map is null!");
                    return;
                }

                // Console.WriteLine($"MapNme '{mapName}':");
                // Console.WriteLine($"Length: {map[0].Length}x{map.Length}");
                // Console.WriteLine("----------------------------------");

                for (int y = 0; y < map.Length; y++)
                {
                    for (int x = 0; x < map[y].Length; x++)
                    {
                        Console.Write(map[y][x] + " ");
                    }
                    // Console.WriteLine();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
            }
        }
    }
}
