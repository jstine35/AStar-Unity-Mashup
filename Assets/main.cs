using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AStar;

public class main : MonoBehaviour
{
    public GameObject CubeWallPrefab;

    // Map can be any size. Size will be calculated from the input data.
    //
    // Pathing Assumptions and Map Requirements:
    //  - all rows are of equal length
    //  - all borders are always closed (non-traversable)
    //     - the path finder does not perform bounds checking
    //  - Only one 'A' (start) and 'B' (target) exist
    //     - if there are multiple, the nearst to 0,0 will be used
    //
    // TODO: verify these assumptions when analysing input map data

    string[] map = new string[]
    {
        "+-----------------+",
        "|                 |",
        "|             X   |",
        "|            XX   |",
        "| Xxxxxxxxxxxxx   |",
        "|  x              |",
        "|x  x             |",
        "|x x         X    |",
        "|x x              |",
        "|  xA  X          |",
        "|  XXXXX  X   X   |",
        "| XXXXXX  X       |",
        "| XXXXXX  X       |",
        "|    x  XX     X  |",
        "|   Bx X          |",
        "|xxxxx  XXXXX     |",
        "|                 |",
        "|                 |",
        "|                 |",
        "|                 |",
        "+-----------------+",
    };

    public float   g_planeUnits   = 10.0f;          // unit size of the default unity plane at scale=1 (TODO: calculate this at run)
    public float   g_mapScale     = 0.25f;          // scalar to reduce size of map, to avoid excess unit coord size on very large maps

    public Vector3 origin;

    public bool rebuildMap;
    public bool restartPath;

    public float mapSizeUnits {
        get { return g_planeUnits * g_mapScale; }
    }

    public Vector3 mapGridScale {
        get { return new Vector3(mapSizeUnits, mapSizeUnits, 1); }
    }

    public Vector3 mapGridTransToOrigin {
        get { return new Vector3(-mapSizeUnits, -mapSizeUnits, 1); }
    }

    public Vector3 TranslateGridCoordToWorld(int2 coord) {
        return origin + (Vector3.Scale(new Vector3(coord.x, coord.y, 0), mapGridTransToOrigin));
    }

    public Vector3 TranslateGridCoordToWorld(Vector3 coord) {
        return origin + (Vector3.Scale(coord, mapGridTransToOrigin));
    }

    void SetupAvatars()
    {
        var start  = AsciiMap.Find(map, 'A');
        var target = AsciiMap.Find(map, 'B');

        var stickatar = GameObject.Find("Stickatar");
        var targatar  = GameObject.Find("Targatar");

        stickatar.transform.position = TranslateGridCoordToWorld(start);
        targatar .transform.position = TranslateGridCoordToWorld(target);

    }

    // Start is called before the first frame update
    void Start()
    {
        BuildMap();
        SetupAvatars();

        var curpos = new int2();
        var pathstate = new Yieldable.PathState();

        foreach (var pos in Yieldable.FindPath(map, pathstate)) {
            curpos = pos;
            //Debug.Log($"NavPos: {curpos.x} x {curpos.y}");
        }

        var stickatar = GameObject.Find("Stickatar");
        var stickpath = stickatar.GetComponent<FollowPath>();
        var waypoints = new List<int2>();

        var backtrack = curpos;
        while (!backtrack.equal0()) {
            var next = pathstate.internal_map[backtrack].Parent;
            waypoints.Add(backtrack);
            backtrack = next;
        }


        var startidx = waypoints.Count-1;
        var walkstart = waypoints[startidx];
        stickpath.waypoints.Add(TranslateGridCoordToWorld(walkstart));

        for (int i=startidx-1; i >= 0; --i) {
            var next = waypoints[i];
        #if false
            // cheap: strip out contigient tiles in a row or column.
            // Unfortunately this wreaks havoc on the lazy spline calculation.
            // (the spline is really only meant to bevel corners)
            if (i > 1 && next.x == waypoints[i-1].x && next.x == waypoints[i+1].x) {
                continue;
            }

            if (i > 1 && next.y == waypoints[i-1].y && next.y == waypoints[i+1].y) {
                continue;
            }
        #endif
            stickpath.waypoints.Add(TranslateGridCoordToWorld(next));
        }
        stickpath.ApplyWaypointList();
    }

    public void DestroyMap()
    {
        var list = GameObject.FindGameObjectsWithTag("DynamicLevelObject");
        foreach(var item in list) {
            GameObject.Destroy(item);
        }
    }

    public void BuildMap()
    {
        var plane = GameObject.Find("Plane");
        var ray = plane.transform.rotation * Vector3.up;
        //Debug.DrawLine(Vector3.zero, ray * 10, Color.red, 6);

        int2 map_size = new int2 { x = map[0].Length, y = map.Length };

        // scale the plane so that each map unit is roughly 1 x 1 world units
        var planeScale = new Vector3(map_size.x * g_mapScale, 1, map_size.y * g_mapScale);

        plane.transform.localScale = planeScale;
        var planeBounds = plane.GetComponent<Renderer>().bounds;
        origin = planeBounds.max;

        Debug.Log($"origin = {origin}");

        var cubeScale = Vector3.Scale(Vector3.Scale(mapGridScale, new Vector3(0.9f, 0.9f, 1)), CubeWallPrefab.transform.localScale);

        for (int y=0; y<map_size.y; ++y) {
            for (int x=0; x<map_size.x; ++x) {
                if (map[y][x] == 'A' || map[y][x] == 'B') continue;
                if (map[y][x] == ' ') continue;

                var startpos = TranslateGridCoordToWorld(new int2(x,y));
                var newcube = Instantiate(CubeWallPrefab, startpos, Quaternion.identity);
                newcube.transform.localScale = cubeScale;
                newcube.tag = "DynamicLevelObject";
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        var plane = GameObject.Find("Plane");
        var ray = plane.transform.rotation * Vector3.up;
        Debug.DrawLine(origin, origin + (ray * 20), Color.red);

        if (rebuildMap) {
            DestroyMap();
            BuildMap();
        }

        if (restartPath) {
        }
        rebuildMap = false;
        restartPath = false;
    }
}
