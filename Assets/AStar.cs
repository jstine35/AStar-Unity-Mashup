using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using UnityEngine;
using Unity.Mathematics;

namespace AStar
{
    public struct AwesomeTile {
        public int F;
        public int G;
        public int H;
        public int closed;
        public int2 Parent;
    }

    public struct OpenListKey {
        public int F;
        public int2 xy;

        public OpenListKey(int2 _xy, int f) {
            F = f;
            xy = _xy;
        }
    };

    public static class AsciiMap {
        public static int2 Find(string[] map, char ch) {
            int2 map_size = new int2 { x = map[0].Length, y = map.Length };
            for (int y=0; y<map_size.y; ++y) {
                for (int x=0; x<map_size.x; ++x) {
                    if (map[y][x] == ch) {
                        return new int2(x,y);
                    }
                }
            }
            // return empty struct if nothing found
            return new int2();
        }

        // Draws a line between points and fails if any point lands on a non-traversable
        // tile. This was a failed experiment. It seems the better strategy is to use 8-way
        // A* pathfinding instead.
        public static bool Walkable(string[] map, int2 start, int2 end) {
            int dx = end.x - start.x;
            int dy = end.y - start.y;

            if (dx == 0 || dy == 0) return true;

            double delta = (double)dy / dx;
            double y = start.y;
            double sign_x = Math.Sign(dx);
            double sign_y = Math.Sign(dy);
            for(double x=start.x; x != end.x; x += sign_x, y += delta * sign_x) {
                int2 coord1  = new int2((int)x, (int)Math.Round(y));
                if (map[coord1.y][coord1.x] != ' ') return false;
            }
            return true;
        }
    }

    public class InternalMap {
        public AwesomeTile[,] m_tiles;

        public InternalMap(int2 size) {
            m_tiles = new AwesomeTile[size.y, size.x];
        }

        public ref AwesomeTile getTile(int2 pos) {
            return ref m_tiles[pos.y, pos.x];
        }

        public ref AwesomeTile this[int2 pos] => ref m_tiles[pos.y, pos.x];
        
        public void clear() {
            Array.Clear(m_tiles, 0, m_tiles.Length);
        }
    }

    class OpenListComparer : IComparer<OpenListKey>
    {
        public int Compare(OpenListKey l, OpenListKey r)
        {
            var result = l.F.CompareTo(r.F);
            //return result;

            // need to ensure no duplicate keys.
            // to do so, use x/y as a deterministic hash

            if (result != 0) return result;
            var lval = ((Int64)l.xy.y << 32) | (Int64)l.xy.x;
            var rval = ((Int64)r.xy.y << 32) | (Int64)r.xy.x;
            return lval.CompareTo(rval);
        }
    };

    public static class Yieldable {
        public class PathState {
            public InternalMap internal_map;
        };

        public static int ComputeHScore(int x, int y, int targetX, int targetY)
        {
            return Math.Abs(targetX - x) + Math.Abs(targetY - y);
        }

        public static IEnumerable<int2> WalkableAdjacentSquares(string[] map, int x, int y)
        {
            yield return new int2(x, y - 1);
            yield return new int2(x, y + 1);
            yield return new int2(x - 1, y);
            yield return new int2(x + 1, y);

            // Diagonals.
            if (map[y-1][x] == ' ' && map[y][x-1] == ' ') {
                yield return new int2(x - 1, y - 1);
            }

            if (map[y+1][x] == ' ' && map[y][x+1] == ' ') {
                yield return new int2(x + 1, y + 1);
            }

            if (map[y+1][x] == ' ' && map[y][x-1] == ' ') {
                yield return new int2(x - 1, y + 1);
            }

            if (map[y-1][x] == ' ' && map[y][x+1] == ' ') {
                yield return new int2(x + 1, y - 1);
            }
        }

        public static IEnumerable<int2> FindPath(string[] map, PathState pathstate) {
            var start  = AsciiMap.Find(map, 'A');
            var target = AsciiMap.Find(map, 'B');
            return FindPath(map, pathstate, start, target);
        }

        public static IEnumerable<int2> FindPath(string[] map, PathState pathstate, int2 start, int2 target) {
            int2 map_size = new int2 { x = map[0].Length, y = map.Length };
            int map_size_in_tiles = map_size.y * map_size.x;
            if (pathstate.internal_map is null) {
                pathstate.internal_map = new InternalMap(map_size);
            }
            else {
                pathstate.internal_map.clear();
            }

            var internalMap = pathstate.internal_map;
            var curpos = new int2();
            var openList = new SortedList<OpenListKey, int>(map_size_in_tiles, new OpenListComparer());

            // start by adding the original position to the open list
            openList.Add(new OpenListKey(start, 0), 0);

            int iter = 0;
            while (openList.Count > 0)
            {
                // get the square with the lowest F score
                var lowest = openList.First().Key;
                curpos  = lowest.xy;

                var current = internalMap[curpos];

                // add the current square to the closed list
                if (true) {
                    internalMap[curpos].closed = 1;
                }

                openList.Remove(lowest);

                // if we added the destination to the closed list, we've found a path
                if (internalMap[target].closed > 0) {
                    yield return curpos;
                    break;
                }

                // WalkableAdjacentSquares is pretty, but it causes heap alloc for the yieldable machine state.
                // could avoid it using an unrolled functional approach rather than a foreach...

                foreach(var adjpos in WalkableAdjacentSquares(map, curpos.x, curpos.y))
                {
                    if (map[adjpos.y][adjpos.x] != ' ' && map[adjpos.y][adjpos.x] != 'B') {
                        continue;
                    }

                    var adjsqu = internalMap[adjpos];
                    var parent = internalMap[current.Parent];
                    var current_g = parent.G + 1;

                    if (adjsqu.closed > 0) {
                        continue;
                    }

                    if (adjsqu.G > 0) {
                        continue;
                    }

                    // compute its score, set the parent
                    adjsqu.G = current_g;
                    adjsqu.H = ComputeHScore(adjpos.x, adjpos.y, target.x, target.y);
                    adjsqu.F = adjsqu.G + adjsqu.H;
                    adjsqu.Parent = curpos;

                    internalMap[adjpos] = adjsqu;

                    var key = new OpenListKey(new int2 {x = adjpos.x, y = adjpos.y}, adjsqu.F);
                    openList.Add(key,0);
                }
                ++iter;
                yield return curpos;
            }
        }
    }
}