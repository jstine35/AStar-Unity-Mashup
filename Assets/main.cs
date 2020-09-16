using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;
using AStar;
using UnityEditor;

public static class Extensions {
    public static double GetUnixTimeSecs(this System.DateTime date) {
        return new System.DateTimeOffset(date).ToUnixTimeMilliseconds() / 1000.0;
    }
}

public static class GlobalPool {
    public static Yieldable.PathState yieldablePathState = new Yieldable.PathState();
    public static List<int2> yPathWaypoints = new List<int2>(48);
    public static string[] map = {
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

    public static void AppendWaypoints(IEnumerable<int2> yieldable_path) {
        YPath.AppendWaypoints(yieldable_path, ref yPathWaypoints);
    }
}

public static class YPath {
    //public static void BuildWaypointsList(IEnumerable<int2> yieldable_path) {
    //    AppendWaypoints(yieldable_path, ref GlobalPool.yPathWaypoints);
    //}

    public static void AppendWaypoints(IEnumerable<int2> yieldable_path) {
        AppendWaypoints(yieldable_path, ref GlobalPool.yPathWaypoints);
    }

    public static void AppendWaypoints(IEnumerable<int2> yieldable_path, ref List<int2> waypoints) {
        if (waypoints is null) {
            waypoints = new List<int2>(48);
        }
        var curpos = new int2();

        foreach (var pos in yieldable_path) {
            curpos = pos;
        }
        var backtrack = curpos;
        
        var pathstate = GlobalPool.yieldablePathState;
        while (!backtrack.equal0()) {
            var next = pathstate.internal_map[backtrack].Parent;
            waypoints.Add(backtrack);
            backtrack = next;
        }
    }
}

public class main : MonoBehaviour
{
    public GameObject cubeWallPrefab;
    public GameObject gameboardTransform;
    public GameObject tileSelectorPrefab;
        
    Vector3 pform_cubeWall;        // position transform for cubeWalls

    // Map can be any size. Size will be calculated from the input data.
    //
    // Pathing Assumptions and Map Requirements:
    //  - all rows are of equal length
    //  - all borders are always closed (non-traversable)
    //     - the path finder does not perform bounds checking
    //  - Only one 'A' (start) and 'B' (target) exist
    //     - if there are multiple, the nearest to 0,0 will be used
    //
    // TODO: verify these assumptions when analysing input map data
    
    [Tooltip("Size of each tile space in Unity Units")]
    public float   tileSizeUnits = 1.0f;

    [Range(0.4f, 1.2f)]
    [Tooltip("Adjust size of each wall block, smaller values create gaps between tiles for more retro look")]
    public float wallSizeScale = 0.9f;
    private Vector3 visibleWallSizeScale = Vector3.negativeInfinity;    // -1 to force re-init

    public Vector3 origin;

    // Toggles being treated as buttons.
    // These are reset to unchecked on each update.
    [Header("PseudoButtons")]
    [Tooltip("Rebuilds the map, useful if code changes to map generator have been made.")]
    public bool rebuildMap;

    [Tooltip("Restart the built-in path runner.")]
    public bool restartPathRunner;
    
    public Vector3 MapGridScale            => new Vector3(tileSizeUnits,  tileSizeUnits, 1);
    public Vector3 MapGridTransToOrigin    => new Vector3(tileSizeUnits,  tileSizeUnits, 1);
    public Vector3 MapGridTransToOriginInv => new Vector3(1.0f/tileSizeUnits, 1.0f/tileSizeUnits, 1);
    
    public static GameObject tileSelector;

    private string[] map = GlobalPool.map;

    public Vector3 TranslateGridCoordToWorld(int2 coord) {
        var vec = pform_cubeWall + Vector3.Scale(new Vector3(coord.x + 0.5f, coord.y + 0.5f, 0), MapGridTransToOrigin);
        return origin + new Vector3(vec.x, vec.y, 0);  
    }

    public Vector3 TranslateWorldCoordToGrid(Vector3 world)
    {
        var vec = world - origin;
        var gridunits =  Vector3.Scale(vec, MapGridTransToOriginInv);
        return gridunits; // - new Vector3(0.5f, 0.5f);
    }

    void SetupAvatars()
    {
        var start  = AsciiMap.Find(map, 'A');
        var target = AsciiMap.Find(map, 'B');

        var stickatar = GameObject.Find("Stickatar");
        var targatar  = GameObject.Find("Targatar");

        stickatar.transform.parent = gameboardTransform.transform;
        targatar .transform.parent = gameboardTransform.transform;
        
        stickatar.transform.localPosition = TranslateGridCoordToWorld(start);
        targatar .transform.localPosition = TranslateGridCoordToWorld(target);

        //stickatar.transform.localRotation = gameboardTransform.transform.rotation;
        //targatar .transform.localRotation = gameboardTransform.transform.rotation;
        //stickatar.transform.localRotation = Quaternion.identity;
        //targatar .transform.localRotation = Quaternion.identity;
    }

    void Start()
    {
        tileSelector = GameObject.Instantiate(tileSelectorPrefab, gameboardTransform.transform);
        pform_cubeWall = new Vector3(0, 0, cubeWallPrefab.GetComponent<Renderer>().bounds.extents.z);

        BuildMap();
        SetupAvatars();
        RunDefinedCourse();
    }

    public GameObject[] GetLevelCubes()
    {
        return GameObject.FindGameObjectsWithTag("DynamicLevelObject");
    }

    public void DestroyMap()
    {
        foreach(var item in GetLevelCubes()) {
            GameObject.Destroy(item);
        }
    }

    public void LevelScaleCubes(float newScale) {
        var wallScale3 = new Vector3(newScale, newScale, cubeWallPrefab.transform.localScale.z);
        var cubeScale = Vector3.Scale(MapGridScale, wallScale3);
        if (visibleWallSizeScale == cubeScale) return;
        visibleWallSizeScale = cubeScale;
        foreach(var item in GetLevelCubes()) {
            item.transform.localScale = cubeScale;
        }
        tileSelector.transform.localScale = new Vector3(tileSizeUnits, tileSizeUnits * tileSelectorPrefab.transform.localScale.y, tileSizeUnits);
    }

    public void BuildMap()
    {
        int2 map_size = new int2 { x = map[0].Length, y = map.Length };

        // scale the plane so that each map unit is roughly 1 x 1 world units
        var planeUnitsAtScale1 = 1.0f;
        var planeScale = new Vector3(map_size.x * tileSizeUnits / planeUnitsAtScale1, map_size.y * tileSizeUnits / planeUnitsAtScale1, 1);

        var plane = GameObject.Find("FloorQuad");
        plane.transform.localScale = planeScale;
        origin = (planeScale * 0.5f) - planeScale;
        origin.z = 0;

        Debug.Log($"origin = {origin}");

        for (int y=0; y<map_size.y; ++y) {
            for (int x=0; x<map_size.x; ++x) {
                if (map[y][x] == 'A' || map[y][x] == 'B') continue;
                if (map[y][x] == ' ') continue;

                var startpos = TranslateGridCoordToWorld(new int2(x,y));
                startpos.z -= 2;
                //Quaternion.AngleAxis(90, Vector3.right) 
                var newcube  = Instantiate(cubeWallPrefab, gameboardTransform.transform);
                newcube.transform.localPosition = startpos;
                newcube.transform.localRotation = Quaternion.identity; // Quaternion.AngleAxis(90, Vector3.right);
                newcube.tag                     = "DynamicLevelObject";
            }
        }
        LevelScaleCubes(wallSizeScale);
    }

    public void RunDefinedCourse()
    {
        var curpos = new int2();
        var pathstate = new Yieldable.PathState();

        foreach (var pos in Yieldable.FindPath(map, pathstate)) {
            curpos = pos;
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

    void Update()
    {
        if (rebuildMap) {
            DestroyMap();
            BuildMap();
        }

        if (restartPathRunner) {
            SetupAvatars();
        }

        LevelScaleCubes(wallSizeScale);

        // reset button-like toggles to 0
        rebuildMap = false;
        restartPathRunner = false;
    }
}
