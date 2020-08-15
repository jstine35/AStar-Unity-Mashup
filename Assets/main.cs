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
        "| Ax              |",
        "|x  x             |",
        "|x x         X    |",
        "|x x              |",
        "|  x   X          |",
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
    static Vector3 g_gridScale    = new Vector3(g_mapSizeUnits, 1, g_mapSizeUnits);

    // for simplicitly, the origin is defined as a 'top-left' corner of a plane on the xz axes.
    // The complexity of the scene is essentially 2D, with Y only used to extrude the 2D
    // upwards. In retrospect, it would have been better to spin the plane into the xy axes and
    // use z for extruding, as it simplifies Vector2/Vector3 conversion.
    static Vector3 origin;


    public Vector3 TranslateGridCoordToWorld(int2 coord, float y_hack) {
        return new Vector3(
            origin.x + (coord.x * g_gridScale.x),
            origin.y + y_hack,
            origin.z + (coord.y * g_gridScale.z)
        );
    }

    public Vector3 TranslateGridCoordToWorld(Vector3 coord) {
        return new Vector3(
            origin.x + (coord.x * g_gridScale.x),
            origin.y,
            origin.z + (coord.y * g_gridScale.z)
        );
    }

    void SetupAvatars()
    {
        var start  = AsciiMap.Find(map, 'A');
        var target = AsciiMap.Find(map, 'B');

        var stickatar = GameObject.Find("Stickatar");
        var targatar  = GameObject.Find("Targatar");

        stickatar.transform.position = TranslateGridCoordToWorld(start,  stickatar.transform.localScale.y);
        targatar .transform.position = TranslateGridCoordToWorld(target, stickatar.transform.localScale.y);

    }

    // Start is called before the first frame update
    void Start()
    {
        var plane = GameObject.Find("Plane");
        origin    = plane.transform.position;

        int2 map_size = new int2 { x = map[0].Length, y = map.Length };

        var planeScale = new Vector3(map_size.x * g_mapScale, 1, map_size.y * g_mapScale);
        plane.transform.localScale = planeScale;

        var cubeScale = g_gridScale;
        cubeScale.Scale(CubeWallPrefab.transform.localScale);

        for (int y=0; y<map_size.y; ++y) {
            for (int x=0; x<map_size.x; ++x) {
                if (map[y][x] == 'A' || map[y][x] == 'B') continue;
                if (map[y][x] == ' ') continue;

                var startpos = TranslateGridCoordToWorld(new int2(x,y), 0);
                var newcube = Instantiate(CubeWallPrefab, startpos, Quaternion.identity, plane.transform);
                newcube.transform.localScale = CubeWallPrefab.transform.localScale;
            }
        }

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
        stickpath.waypoints.Add(TranslateGridCoordToWorld(walkstart,0));

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
            stickpath.waypoints.Add(TranslateGridCoordToWorld(next,0));
        }
        stickpath.ApplyWaypointList();
    }

    // Update is called once per frame
    void Update()
    {
    }
}
