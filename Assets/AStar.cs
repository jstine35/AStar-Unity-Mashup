using System;
using System.Collections.Generic;
using System.Linq;

namespace AStar
{
    public struct int2 {
        public int x,y;

        public int2(int _x, int _y) {
            x = _x;
            y = _y;
        }

        public bool equal0() {
            return x == 0 && y == 0;
        }
    };

    public struct AwesomeTile {
        public int F;
        public int G;
        public int H;
        public int closed;
        public int2 Parent;
    }

    struct OpenListKey {
        public int F;
        public int2 xy;

        public OpenListKey(int2 _xy, int f) {
            F = f;
            xy = _xy;
        }
    };

    public class InternalMap {
        public AwesomeTile[,] m_tiles;

        public InternalMap(int2 size) {
            m_tiles = new AwesomeTile[size.y, size.x];
        }

        public ref AwesomeTile getTile(int2 pos) {
            return ref m_tiles[pos.y, pos.x];
        }

        public ref AwesomeTile this[int2 pos] {
            get {
                return ref m_tiles[pos.y, pos.x];
            }
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

    public class Yieldable {
        public class PathState {
            public InternalMap internal_map;
        };
        
        public static int ComputeHScore(int x, int y, int targetX, int targetY)
        {
            return Math.Abs(targetX - x) + Math.Abs(targetY - y);
        }

        public static IEnumerable<int2> WalkableAdjacentSquares(int x, int y)
        {
            yield return new int2(x, y - 1);
            yield return new int2(x, y + 1);
            yield return new int2(x - 1, y);
            yield return new int2(x + 1, y);
        }

        public static IEnumerable<int2> FindPath(string[] map, PathState pathstate) {
            int2 map_size = new int2 { x = map[0].Length, y = map.Length };
            int map_size_in_tiles = map_size.y * map_size.x;
            pathstate.internal_map = new InternalMap(map_size);

            var internalMap = pathstate.internal_map;
            var curpos = new int2();
            var start  = new int2();
            var target = new int2();
            var openList = new SortedList<OpenListKey, int>(map_size_in_tiles, new OpenListComparer());

            if (true) {
                int y = 0;
                int x = 0;
                foreach (var row in map) {
                    x = 0;
                    foreach (var tile in row) {
                        if (tile == 'A') {
                            start.x = x;
                            start.y = y;
                        }
                        if (tile == 'B') {
                            target.x = x;
                            target.y = y;
                        }
                        ++x;
                    }
                    ++y;
                }
            }

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
                    break;
                }

                // WalkableAdjacentSquares is pretty, but it causes heap alloc for the yieldable machine state.
                // could avoid it using an unrolled functional approach rather than a foreach...

                foreach(var adjpos in WalkableAdjacentSquares(curpos.x, curpos.y))
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