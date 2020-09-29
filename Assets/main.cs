using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.IO;
using UnityEngine;
using AStar;
using UnityEditor;

public static class Extensions {
    public static double GetUnixTimeSecs(this System.DateTime date) {
        return new System.DateTimeOffset(date).ToUnixTimeMilliseconds() / 1000.0;
    }
}

public class FloorZone {
    public GameObject       xformObj;
    public GameObject       floor;
    public List<GameObject> walls = new List<GameObject>();
}

public static class GlobalPool {
    public static Yieldable.PathState yieldablePathState = new Yieldable.PathState();
    public static List<int2> yPathWaypoints = new List<int2>(48);
    public static string[] map;

    public static void AppendWaypoints(IEnumerable<int2> yieldable_path) {
        YPath.AppendWaypoints(yieldable_path, ref yPathWaypoints);
    }

    public static FloorZone[] floors = new FloorZone[4];
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
    public GameObject floorTransform;
    public GameObject floorPrefab;
    public GameObject cubeWallPrefab;
    public GameObject tileSelectorPrefab;
    public GameObject gameWorldTransform;
        
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

    public Vector3 origin;      // specifies top left corner, for friendly gameboard coordinate logicss

    [Tooltip("Contains maps in plaintext format.")]
    public string MapFile  = "cube1.ascmap";

    private double mapFileLastWriteInSecs;

    // Toggles being treated as buttons.
    // These are reset to unchecked on each update.
    [Header("PseudoButtons")]
    [Tooltip("Rebuilds the map, useful if code changes to map generator have been made.")]
    public bool rebuildMap;

    [Tooltip("Restart the built-in path runner.")]
    public bool restartPathRunner;
    
    public Vector3 MapGridScale            => new Vector3(tileSizeUnits, 1, tileSizeUnits);
    public Vector3 MapGridTransToOrigin    => new Vector3(tileSizeUnits, 1, tileSizeUnits);
    public Vector3 MapGridTransToOriginInv => new Vector3(1.0f/tileSizeUnits, 1, 1.0f/tileSizeUnits);
    
    public static GameObject tileSelector;

    private string[] map = GlobalPool.map;

    public Vector3 TranslateGridCoordToWorld(int2 coord) {
        var vec = Vector3.Scale(new Vector3(coord.x + 0.5f, 0, coord.y + 0.5f), MapGridTransToOrigin);
        return origin + vec;
    }

    public Vector2 TranslateWorldCoordToGrid(Vector3 world) {
        var vec = world - origin;
        var gridunits = Vector3.Scale(vec, MapGridTransToOriginInv);
        return new Vector2(gridunits.x, gridunits.z);
    }

    void SetupAvatars()
    {
        var start  = AsciiMap.Find(map, 'A');
        var target = AsciiMap.Find(map, 'B');

        var stickatar = GameObject.Find("Stickatar");
        var targatar  = GameObject.Find("Targatar");

        stickatar.transform.parent = GlobalPool.floors[0].xformObj.transform;
        targatar .transform.parent = GlobalPool.floors[0].xformObj.transform;
        
        stickatar.transform.localPosition = TranslateGridCoordToWorld(start);
        targatar .transform.localPosition = TranslateGridCoordToWorld(target);
    }

    bool IsMapChanged(string[] dest, IList<string> src) {
        if (dest is null) return true;
        if (src is null) return false;
        if (dest.Length != src.Count) return true;
        for(int i=0; i<dest.Length; ++i) {
            if (dest[i] != src[i]) {
                return true;
            }
        }
        return false;
    }

    void ReloadMaps() {
        Debug.Log($"Scanning map data: {MapFile}");

        StreamReader reader = new StreamReader(MapFile);
        var newmaps = new List<List<string>>();
        var newmap = new List<string>();
        while (!reader.EndOfStream) {
            var line = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(line)) {
                newmaps.Add(newmap);
                newmap.Clear();
            }
            else {
                newmap.Add(line);
            }
        }

        if (IsMapChanged(GlobalPool.map, newmaps[0])) {
            Debug.Log($"Applying new data for map panel 1");
            GlobalPool.map = newmaps[0].ToArray();
            map = GlobalPool.map;
            DestroyMap(0);
            BuildMap(0, map);
        }

        DestroyMap(1);
        BuildMap(1, newmaps[1].ToArray());
    }

    void UpdateStaleAssets() {
        if (mapFileLastWriteInSecs != File.GetLastWriteTime(MapFile).GetUnixTimeSecs()) {
            Debug.Log($"Stale asset {MapFile} detected");
            mapFileLastWriteInSecs = File.GetLastWriteTime(MapFile).GetUnixTimeSecs();
            ReloadMaps();
        }
    }

    void Awake() {
        for(int i=0; i<GlobalPool.floors.Length; ++i) {
            GlobalPool.floors[i] = new FloorZone();
            GlobalPool.floors[i].xformObj = Instantiate(floorTransform, gameWorldTransform.transform);
        }
    }

    void Start()
    {
        tileSelector = GameObject.Instantiate(tileSelectorPrefab, GlobalPool.floors[0].xformObj.transform);

        ReloadMaps();
        SetupAvatars();
        //RunDefinedCourse();
    }

    public void DestroyMap(int idx) {
        Debug.Log($"Destroying level objects on floor {idx}");
        DestroyMap(GlobalPool.floors[idx]);
    }

    public void DestroyMap(FloorZone floor) {
        if (floor is null) return;

        foreach(var item in floor.walls) {
            GameObject.Destroy(item);
        }
        GameObject.Destroy(floor.floor);

        floor.floor = null;
        floor.walls.Clear();

        visibleWallSizeScale = Vector3.zero;
    }

    public void LevelScaleCubes(FloorZone floor, float newScale) {
        var wallScale3 = new Vector3(newScale, newScale, cubeWallPrefab.transform.localScale.z);
        var cubeScale = Vector3.Scale(MapGridScale, wallScale3);
        if (visibleWallSizeScale == cubeScale) return;
        visibleWallSizeScale = cubeScale;
        foreach(var item in floor.walls) {
            item.transform.localScale = cubeScale;
        }
        tileSelector.transform.localScale = new Vector3(tileSizeUnits, cubeWallPrefab.transform.localScale.z * tileSelectorPrefab.transform.localScale.y, tileSizeUnits);
    }

    Mesh cubeWallMesh;

    public void BuildCubeWallMesh(float wallHeight) {
        if (cubeWallMesh == null) {
            cubeWallMesh = cubeWallPrefab.GetComponent<MeshFilter>().sharedMesh;
        }

        var mesh = cubeWallMesh;
		var origvtx = mesh.vertices;
		var destvtx = new Vector3[origvtx.Length];

        Vector3 scale = new Vector3(wallSizeScale,1,wallSizeScale);
        Vector3 xform = new Vector3(0,wallHeight,0);

        for (int i = 0; i < origvtx.Length; i++) {
            var localscale = scale;
            if (origvtx[i].y <= 0) localscale.y = 1;
			destvtx[i] = Vector3.Scale(origvtx[i], scale);
        }

        mesh.vertices = destvtx;
        mesh.RecalculateNormals();
        cubeWallMesh = mesh;
    }

    // resulting floor mesh is built following Y-up convention.
    public void BuildFloor(FloorZone floorzone) {
        int2 map_size = new int2 { x = map[0].Length, y = map.Length };

        var planeUnitsAtScale1 = 1.0f;
        var planeSize = new Vector3(
            x: map_size.x * tileSizeUnits / planeUnitsAtScale1,
            y: 1,
            z: map_size.y * tileSizeUnits / planeUnitsAtScale1
        );

        origin = (planeSize * 0.5f) - planeSize;
        origin.z = 0;

        var plane = GameObject.Instantiate(floorPrefab, floorzone.xformObj.transform);
        floorzone.floor = plane;

        plane.transform.localScale = Vector3.one;
	
        var mesh = plane.GetComponent<MeshFilter>().mesh;
		var origvtx = mesh.vertices;
		var destvtx = new Vector3[origvtx.Length];
        float miny = float.MaxValue;

		foreach (var vert in origvtx) {
            miny = Mathf.Min(miny, vert.y);
        }

		for (int i = 0; i < origvtx.Length; i++) {
			destvtx[i] = Vector3.Scale(origvtx[i], planeSize) + new Vector3(0,miny,0);
        }

        mesh.vertices = destvtx;
        mesh.RecalculateNormals();

        plane.GetComponent<BoxCollider>().size   = planeSize;
        plane.GetComponent<BoxCollider>().center = new Vector3(0, miny, 0);
    }

    public void BuildMap(int floor_id, string[] map) {
        BuildMap(GlobalPool.floors[floor_id], map);
    }

    public void BuildMap(FloorZone floor, string[] map) {
        BuildFloor(floor);

        transform.position = new Vector3(0,0,origin.x);

        int2 map_size = new int2 { x = map[0].Length, y = map.Length };

        Debug.Log($"origin = {origin}");

        for (int y=0; y<map_size.y; ++y) {
            for (int x=0; x<map_size.x; ++x) {
                if (map[y][x] == 'A' || map[y][x] == 'B') continue;
                if (map[y][x] == ' ') continue;

                var startpos = TranslateGridCoordToWorld(new int2(x,y));
                //startpos.y -= cubeWallPrefab.transform.localScale.y * 0.50f;        // to workaround the origin of cubeWallPrefab being centered rather than at the foot/base of the model
                var newcube  = Instantiate(cubeWallPrefab, floor.xformObj.transform);
                floor.walls.Add(newcube);
                newcube.transform.localPosition = startpos;
                newcube.transform.localRotation = Quaternion.identity; // Quaternion.AngleAxis(90, Vector3.right);
                newcube.tag                     = "DynamicLevelObject";
            }
        }
        LevelScaleCubes(floor, wallSizeScale);
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


    private double lastAssetPollTime;

    void Update()
    {
        var nowtime = System.DateTime.UtcNow.GetUnixTimeSecs();
        if (nowtime > lastAssetPollTime + 2) {
            lastAssetPollTime = nowtime;
            UpdateStaleAssets();
        }

        if (rebuildMap) {
            DestroyMap(0);
            BuildMap(0, map);
        }

        if (restartPathRunner) {
            SetupAvatars();
        }

        LevelScaleCubes(GlobalPool.floors[0], wallSizeScale);

        // reset button-like toggles to 0
        rebuildMap = false;
        restartPathRunner = false;
    }
}
