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
        "|                 |",
        "|                 |",
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

    void SetupAvatars(Vector3 gridScale)
    {
        var start  = AsciiMap.Find(map, 'A');
        var target = AsciiMap.Find(map, 'B');

        var stickatar = GameObject.Find("Stickatar");
        var targatar  = GameObject.Find("Targatar");

        stickatar.GetComponent<Transform>().localPosition = new Vector3(
            (start.x * gridScale.x), 0,
            (start.y * gridScale.z)
        );

    }

    // Start is called before the first frame update
    void Start()
    {
        var plane    = GameObject.Find("Plane");
        //var bounds   = plane.GetComponent<Renderer>().bounds;
        var position = plane.GetComponent<Transform>().position;
        var rotation = plane.GetComponent<Transform>().rotation;

        Vector3 origin = position; //new Vector3(bounds.min.x, 0, bounds.min.z);
        int2 map_size = new int2 { x = map[0].Length, y = map.Length };

        var planeScale = new Vector3(map_size.x, 1, map_size.y);
        var planesprite = GameObject.Find("PlaneSprite");
        plane.GetComponent<Transform>().localScale = planeScale;

        var gridScale = new Vector3(20, 1, 20);
        var cubeScale = gridScale;
        cubeScale.Scale(CubeWallPrefab.GetComponent<Transform>().localScale);

        for (int y=0; y<map_size.y; ++y) {
            for (int x=0; x<map_size.x; ++x) {
                if (map[y][x] == 'A' || map[y][x] == 'B') continue;
                if (map[y][x] == ' ') continue;

                var startpos = new Vector3(
                    origin.x + (x * gridScale.x), 0,
                    origin.z + (y * gridScale.z)
                );
                var newcube = Instantiate(CubeWallPrefab, startpos, Quaternion.identity, plane.GetComponent<Transform>());
                newcube.GetComponent<Transform>().localScale = CubeWallPrefab.GetComponent<Transform>().localScale;
            }
        }

        SetupAvatars(gridScale);

        var curpos = new int2();
        var pathstate = new Yieldable.PathState();

        foreach (var pos in Yieldable.FindPath(map, pathstate)) {
            curpos = pos;
            //Debug.Log($"NavPos: {curpos.x} x {curpos.y}");
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
