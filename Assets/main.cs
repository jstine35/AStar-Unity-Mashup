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

    static float   g_planeUnits   = 20.0f;
    static float   g_mapScale     = 0.25f;
    static float   g_mapSizeUnits = g_planeUnits * g_mapScale;
    static Vector3 g_gridScale    = new Vector3(g_mapSizeUnits, g_mapSizeUnits, 1);

    // for simplicitly, the origin is defined as a 'top-left' corner of a plane on the xy axes.
    // The complexity of the scene is essentially 2D, with Z only used to extrude the 2D
    // upwards.
    public Vector3 origin;

    public bool rebuildMap;
    public bool restartPath;

    public Vector3 TranslateGridCoordToWorld(int2 coord) {
        return origin + (Vector3.Scale(new Vector3(coord.x, coord.y, 0), g_gridScale));
    }

    public Vector3 TranslateGridCoordToWorld(Vector3 coord) {
        return origin + (Vector3.Scale(coord, g_gridScale));
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
        plane.transform.position = new Vector3(0, 0, 0);
        //plane.transform.rotation = Quaternion.Euler(0,0,90);

        int2 map_size = new int2 { x = map[0].Length, y = map.Length };

        var planeScale = new Vector3(map_size.x * g_mapScale, 1, map_size.y * g_mapScale);
        origin = new Vector3(planeScale.x / 2, planeScale.y / 2);

        plane.transform.localScale = planeScale;

        var cubeScale = Vector3.Scale(g_gridScale, CubeWallPrefab.transform.localScale);

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
