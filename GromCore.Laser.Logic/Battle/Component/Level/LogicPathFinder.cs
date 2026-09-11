using GromCore.Laser.Logic.Util;
using GromCore.Laser.Titan.Math;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace GromCore.Laser.Logic.Battle.Level
{
    public sealed class LogicPathFinder
    {
        private const int TileSize = 300;
        private const int HalfTile = TileSize / 2;
        private const int MaxPathLength = 50;
        private const int MaxLineOfSightDistance = 100;
        private const int MaxNodesToProcess = 2000;
        private const int MaxCacheSize = 1000;

        private readonly byte[,] _navGrid;
        private readonly int _width;
        private readonly int _height;
        private readonly bool _allowDiagonals;
        private readonly TileMap _tileMap;
        private readonly int _agentRadius;

        private readonly Dictionary<int, bool> _losCache = new(MaxCacheSize);
        private int _losCacheGeneration;

        private readonly Dictionary<int, int> _gScorePool = new(256);
        private readonly Dictionary<int, int> _cameFromPool = new(256);
        private readonly List<LogicVector2> _pathResult = new(MaxPathLength);

        public LogicPathFinder(TileMap tileMap, bool allowDiagonals = true, int agentRadiusWorld = 200)
        {
            _allowDiagonals = allowDiagonals;
            _tileMap = tileMap;
            _agentRadius = agentRadiusWorld;

            _width = Math.Max(1, tileMap.LogicWidth / TileSize);
            _height = Math.Max(1, tileMap.LogicHeight / TileSize);

            int clearanceTiles = Math.Max(1, (int)Math.Ceiling(agentRadiusWorld / (double)TileSize));
            _navGrid = BuildNavGrid(tileMap, clearanceTiles);
        }

        private byte[,] BuildNavGrid(TileMap tileMap, int clearance)
        {
            var grid = new byte[_width, _height];

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    var tile = tileMap.GetTile(x, y, true);
                    grid[x, y] = (byte)(tile?.Data?.BlocksMovement == true ? 0 : 1);
                }
            }

            if (clearance <= 0) return grid;

            var nav = new byte[_width, _height];
            for (int x = 0; x < _width; x++)
                for (int y = 0; y < _height; y++)
                    nav[x, y] = 1;

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    if (grid[x, y] == 0)
                    {
                        int minX = Math.Max(0, x - clearance);
                        int maxX = Math.Min(_width - 1, x + clearance);
                        int minY = Math.Max(0, y - clearance);
                        int maxY = Math.Min(_height - 1, y + clearance);

                        for (int xx = minX; xx <= maxX; xx++)
                            for (int yy = minY; yy <= maxY; yy++)
                                nav[xx, yy] = 0;
                    }
                }
            }

            return nav;
        }

        public List<LogicVector2> FindPath(LogicVector2 startPos, LogicVector2 goalPos)
        {
            int startX = ToTileX(startPos.X);
            int startY = ToTileY(startPos.Y);
            int goalX = ToTileX(goalPos.X);
            int goalY = ToTileY(goalPos.Y);

            if (!IsWalkable(startX, startY))
            {
                var nearest = FindNearestWalkable(startX, startY);
                if (nearest.Item1 < 0) return null;
                startX = nearest.Item1;
                startY = nearest.Item2;
            }

            if (!IsWalkable(goalX, goalY))
            {
                var nearest = FindNearestWalkable(goalX, goalY);
                if (nearest.Item1 < 0) return null;
                goalX = nearest.Item1;
                goalY = nearest.Item2;
            }

            if (startX == goalX && startY == goalY)
            {
                _pathResult.Clear();
                _pathResult.Add(ToWorld(goalX, goalY));
                return _pathResult;
            }

            _gScorePool.Clear();
            _cameFromPool.Clear();

            int startKey = PackKey(startX, startY);
            int goalKey = PackKey(goalX, goalY);

            var open = new PriorityQueue<int, int>();
            _gScorePool[startKey] = 0;
            open.Enqueue(startKey, 0);

            int nodesProcessed = 0;

            while (open.Count > 0)
            {
                if (++nodesProcessed > MaxNodesToProcess)
                    break;

                int currentKey = open.Dequeue();
                if (currentKey == goalKey)
                    return ReconstructPath(currentKey, goalX, goalY);

                int currentX = currentKey >> 16;
                int currentY = currentKey & 0xFFFF;
                int currentG = _gScorePool[currentKey];

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;

                        bool isDiagonal = dx != 0 && dy != 0;
                        if (!_allowDiagonals && isDiagonal) continue;

                        int nx = currentX + dx;
                        int ny = currentY + dy;

                        if (!IsWalkable(nx, ny)) continue;

                        if (isDiagonal)
                        {
                            if (!IsWalkable(currentX + dx, currentY)) continue;
                            if (!IsWalkable(currentX, currentY + dy)) continue;
                        }

                        int neighborKey = PackKey(nx, ny);
                        int stepCost = isDiagonal ? 14 : 10;
                        int tentativeG = currentG + stepCost;

                        if (!_gScorePool.TryGetValue(neighborKey, out int existingG) || tentativeG < existingG)
                        {
                            _gScorePool[neighborKey] = tentativeG;
                            _cameFromPool[neighborKey] = currentKey;

                            int h = Heuristic(nx, ny, goalX, goalY);
                            int f = tentativeG + h;
                            open.Enqueue(neighborKey, f);
                        }
                    }
                }
            }

            return null;
        }

        private (int, int) FindNearestWalkable(int x, int y)
        {
            for (int r = 1; r <= 5; r++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    for (int dy = -r; dy <= r; dy++)
                    {
                        if (Math.Abs(dx) != r && Math.Abs(dy) != r) continue;
                        int nx = x + dx;
                        int ny = y + dy;
                        if (IsWalkable(nx, ny))
                            return (nx, ny);
                    }
                }
            }
            return (-1, -1);
        }

        private List<LogicVector2> ReconstructPath(int endKey, int goalX, int goalY)
        {
            _pathResult.Clear();

            int key = endKey;
            int count = 0;

            while (_cameFromPool.ContainsKey(key) && count < MaxPathLength)
            {
                int x = key >> 16;
                int y = key & 0xFFFF;
                _pathResult.Add(ToWorld(x, y));
                key = _cameFromPool[key];
                count++;
            }

            int startX = key >> 16;
            int startY = key & 0xFFFF;
            _pathResult.Add(ToWorld(startX, startY));

            _pathResult.Reverse();
            return _pathResult;
        }

        public bool IsPassable(LogicVector2 pos)
        {
            return IsWalkable(ToTileX(pos.X), ToTileY(pos.Y));
        }

        public LogicVector2 GetNextVisibleNode(LogicVector2 startPos, List<LogicVector2> path)
        {
            if (path == null || path.Count == 0) return startPos;

            int startTileX = ToTileX(startPos.X);
            int startTileY = ToTileY(startPos.Y);

            int maxIndex = Math.Min(path.Count, MaxPathLength);

            for (int i = maxIndex - 1; i >= 1; i--)
            {
                int nodeTileX = ToTileX(path[i].X);
                int nodeTileY = ToTileY(path[i].Y);

                if (HasLineOfSight(startTileX, startTileY, nodeTileX, nodeTileY))
                    return path[i];
            }

            return path.Count > 1 ? path[1] : path[0];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int Heuristic(int ax, int ay, int bx, int by)
        {
            int dx = Math.Abs(ax - bx);
            int dy = Math.Abs(ay - by);
            return 10 * (_allowDiagonals ? Math.Max(dx, dy) : dx + dy);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsWalkable(int x, int y)
        {
            return (uint)x < (uint)_width && (uint)y < (uint)_height && _navGrid[x, y] == 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int PackKey(int x, int y) => (x << 16) | (y & 0xFFFF);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int ToTileX(int worldX) => Math.Clamp(worldX / TileSize, 0, _width - 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int ToTileY(int worldY) => Math.Clamp(worldY / TileSize, 0, _height - 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static LogicVector2 ToWorld(int tileX, int tileY) =>
            new LogicVector2(tileX * TileSize + HalfTile, tileY * TileSize + HalfTile);

        private bool HasLineOfSight(int x0, int y0, int x1, int y1)
        {
            if (!IsWalkable(x0, y0) || !IsWalkable(x1, y1))
                return false;

            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);

            if (dx > MaxLineOfSightDistance || dy > MaxLineOfSightDistance)
                return false;

            int cacheKey = PackKey(x0 ^ x1, y0 ^ y1) ^ (x0 << 8) ^ y0;

            if (_losCache.Count > MaxCacheSize)
            {
                _losCacheGeneration++;
                _losCache.Clear();
            }

            if (_losCache.TryGetValue(cacheKey, out bool cached))
                return cached;

            bool result = CheckLineOfSight(x0, y0, x1, y1, dx, dy);
            _losCache[cacheKey] = result;
            return result;
        }

        private bool CheckLineOfSight(int x0, int y0, int x1, int y1, int dx, int dy)
        {
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;

            int x = x0, y = y0;

            if (dx >= dy)
            {
                int err = dx / 2;
                for (int i = 0; i < dx; i++)
                {
                    int prevY = y;
                    x += sx;
                    err -= dy;
                    if (err < 0)
                    {
                        y += sy;
                        err += dx;
                        if (!IsWalkable(x - sx, y) || !IsWalkable(x, prevY))
                            return false;
                    }
                    if (!IsWalkable(x, y)) return false;
                }
            }
            else
            {
                int err = dy / 2;
                for (int i = 0; i < dy; i++)
                {
                    int prevX = x;
                    y += sy;
                    err -= dx;
                    if (err < 0)
                    {
                        x += sx;
                        err += dy;
                        if (!IsWalkable(x, y - sy) || !IsWalkable(prevX, y))
                            return false;
                    }
                    if (!IsWalkable(x, y)) return false;
                }
            }

            return true;
        }

        public void ClearCache()
        {
            _losCache.Clear();
            _losCacheGeneration = 0;
        }
    }
}
