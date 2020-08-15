using System.Collections;
using System.Collections.Generic;
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
        "|  x A X          |",
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

    public Vector3 TranslateGridCoordToWorld(int2 coord, float y_hack) {
        var plane    = GameObject.Find("Plane");
        var origin   = plane.transform.position;

        return new Vector3(
            origin.x + (coord.x * g_gridScale.x),
            origin.y + y_hack,
            origin.z + (coord.y * g_gridScale.z)
        );
    }

    void SetupAvatars()
    {
        var start  = AsciiMap.Find(map, 'A');
        var target = AsciiMap.Find(map, 'B');

        var stickatar = GameObject.Find("Stickatar");
        var targatar  = GameObject.Find("Targatar");

        stickatar.transform.position = TranslateGridCoordToWorld(start,  stickatar.GetComponent<Transform>().localScale.y);
        targatar .transform.position = TranslateGridCoordToWorld(target, stickatar.GetComponent<Transform>().localScale.y);

    }

    // Start is called before the first frame update
    void Start()
    {
        var plane    = GameObject.Find("Plane");
        var position = plane.GetComponent<Transform>().position;
        var rotation = plane.GetComponent<Transform>().rotation;

        int2 map_size = new int2 { x = map[0].Length, y = map.Length };

        var planeScale = new Vector3(map_size.x * g_mapScale, 1, map_size.y * g_mapScale);
        var planesprite = GameObject.Find("PlaneSprite");
        plane.GetComponent<Transform>().localScale = planeScale;

        var cubeScale = g_gridScale;
        cubeScale.Scale(CubeWallPrefab.GetComponent<Transform>().localScale);

        for (int y=0; y<map_size.y; ++y) {
            for (int x=0; x<map_size.x; ++x) {
                if (map[y][x] == 'A' || map[y][x] == 'B') continue;
                if (map[y][x] == ' ') continue;

                var startpos = TranslateGridCoordToWorld(new int2(x,y), 0);
                var newcube = Instantiate(CubeWallPrefab, startpos, Quaternion.identity, plane.GetComponent<Transform>());
                newcube.GetComponent<Transform>().localScale = CubeWallPrefab.GetComponent<Transform>().localScale;
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

        var backtrack = curpos;
        int bci = 0;
        while (!backtrack.equal0()) {
            stickpath.waypoints.Add(TranslateGridCoordToWorld(backtrack, 0));
            backtrack = pathstate.internal_map[backtrack].Parent;
            ++bci;
        }
        stickpath.waypoints.Reverse();
        stickpath.ApplyWaypointList();
    }

    // Update is called once per frame
    void Update()
    {
    }
}
