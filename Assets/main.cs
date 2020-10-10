using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.IO;
using UnityEngine;
using AStar;
using Unity.Mathematics;

public static partial class PartialExtensions {
    public static double GetUnixTimeSecs(this System.DateTime date) {
        return new System.DateTimeOffset(date).ToUnixTimeMilliseconds() / 1000.0;
    }
}

public class FloorZone {
    public GameObject       xformObj;
    public GameObject       floor;
    public int              map_id;     // indexes GlobalPool.maps[]
    public List<GameObject> walls = new List<GameObject>();

    public string[] Map => GlobalPool.maps[map_id]?.rows;

    public static FloorZone Current => GlobalPool.CurrentFloor;
}

public class MapData {
    public string[] rows;
    public int2 Size => new int2(rows[0].Length, rows.Length);

    public MapData(string[] data) {
        rows = data;
    }

    public char this[int x, int y] {
        get => rows[y][x];
        //set => rows[y][x] = value;        // enable this after switching from string[]
    }
}

public static class GlobalPool {
    public static Yieldable.PathState yieldablePathState = new Yieldable.PathState();
    public static List<int2> yPathWaypoints = new List<int2>(48);
    public static MapData[] maps = new MapData[16];

    public static void AppendWaypoints(IEnumerable<int2> yieldable_path) {
        YPath.AppendWaypoints(yieldable_path, ref yPathWaypoints);
    }

    public static FloorZone[] floors = new FloorZone[4];

    public static int currentFloorId;
    public static FloorZone CurrentFloor => floors[currentFloorId];
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
        while (backtrack.x !=0 && backtrack.y != 0) {
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

    public float   cubeWallHeight = 5.0f;

    [Range(0.4f, 1.2f)]
    [Tooltip("Adjust size of each wall block, smaller values create gaps between tiles for more retro look")]
    public float wallSizeScale = 0.9f;
    private float3 visibleWallSizeScale = Vector3.negativeInfinity;    // -1 to force re-init

    public float3 origin;      // specifies top left corner, for friendly gameboard coordinate logicss

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
    

    public float3 MapGridScale            => new float3(tileSizeUnits, 1, tileSizeUnits);
    public float3 MapGridTransToOrigin    => new float3(tileSizeUnits, 1, tileSizeUnits);
    public float3 MapGridTransToOriginInv => new float3(1.0f/tileSizeUnits, 1, 1.0f/tileSizeUnits);

    public float2 MapGridSize2            => new float2(tileSizeUnits,    tileSizeUnits);
    public float3 MapGridSize3            => new float3(tileSizeUnits, 0, tileSizeUnits);
    public float3 CubeWallHeight3         => new float3(0, cubeWallHeight, 0);

    public static GameObject tileSelector;

    private string[] curmap;

    public float3 TranslateGridCoordToWorld(int2 coord) {
        var vec = new float3(coord.x + 0.5f, 0, coord.y + 0.5f) * MapGridTransToOrigin;
        return origin + vec;
    }

    public float2 TranslateWorldCoordToGrid(float3 world) {
        var vec = world - origin;
        var gridunits = vec * MapGridTransToOriginInv;
        return new float2(gridunits.x, gridunits.z);
    }

    void SetupAvatars()
    {
        if (curmap == null) return;

        var start  = AsciiMap.Find(curmap, 'A');
        var target = AsciiMap.Find(curmap, 'B');

        var stickatar = GameObject.Find("Stickatar");
        var targatar  = GameObject.Find("Targatar");

        stickatar.transform.parent = GlobalPool.floors[0].xformObj.transform;
        targatar .transform.parent = GlobalPool.floors[0].xformObj.transform;
        
        stickatar.transform.localPosition = TranslateGridCoordToWorld(start);
        targatar .transform.localPosition = TranslateGridCoordToWorld(target);
    }

    bool IsMapChanged(MapData dest, IList<string> src) {
        if (dest == null || dest.rows == null) return true;
        return IsMapChanged(dest.rows, src);
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
                newmap = new List<string>();
            }
            else {
                newmap.Add(line);
            }
        }

        for (int i=0; i<4; ++i) {
            if (i >= newmaps.Count) break;
            if (newmaps[i] == null) continue;
            if (IsMapChanged(GlobalPool.maps[i], newmaps[i])) {
                Debug.Log($"Applying new data for map panel {i}");
                GlobalPool.maps[i] = new MapData(newmaps[i].ToArray());
            }
        }

        for (int i=0; i<4; ++i) {
            DestroyMap(i);
            BuildMap(i);
        }

        SetFloorOrientations();
        SetActiveFloor(0);

        // hack - eventually we need better logic to determine global map.
        curmap = GlobalPool.maps[0].rows;
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


    void SetFloorOrientations() {
        // Sanity checks:
        //   top and bottom maps should be identical sizes.
        //   left and right maps should be identical sizes.
        //   top.y == right.y

        //    var size = new float2(map[0].Length, map.Length);

        var map = GlobalPool.floors[0].Map;
        var size = new float2(map[0].Length, map.Length);

        var centroid = float3.zero;
        var angle = 0;
        for(int i=0; i<GlobalPool.floors.Length; ++i) {
            if (GlobalPool.floors[i].xformObj == null) continue;
            var xform = GlobalPool.floors[i].xformObj.transform;
            xform.localPosition = Vector3.up * (size.x * MapGridSize2.x) / 2;
            xform.RotateAround(float3.zero, Vector3.forward, angle);
            xform.localRotation = Quaternion.AngleAxis(angle, Vector3.forward);
            angle -= 90;
        }
    }

    void SetActiveFloor(int floor_id) {
        for(int i=0; i<GlobalPool.floors.Length; ++i) {
            GlobalPool.floors[i].floor.GetComponent<FloorClickListener>().enabled = (i == floor_id);
        }
        // TODO: Animate camera to focus on new floor ?
    }

    void Start()
    {
        tileSelector = GameObject.Instantiate(tileSelectorPrefab, GlobalPool.floors[0].xformObj.transform);
        UnitMesh.ScaleGO(tileSelector, MapGridSize3 + (CubeWallHeight3 * 1.1f));

        ReloadMaps();
        SetupAvatars();
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
    }

    Mesh cubeWallUnitMesh;    
    Mesh cubeWallMesh;

    public void BuildCubeWallMesh(float2 size, float height) {
        if (cubeWallUnitMesh == null) {
            cubeWallUnitMesh = Instantiate(cubeWallPrefab.GetComponent<MeshFilter>().sharedMesh);
        }

        var scale3 = new float3(size.x * wallSizeScale, cubeWallHeight, size.y * wallSizeScale);
        if (math.all(visibleWallSizeScale == scale3)) return;
        visibleWallSizeScale = scale3;

        Debug.Log($"Rebuilding cubeWall scale = {visibleWallSizeScale}");

        cubeWallMesh = UnitMesh.Scale(cubeWallUnitMesh, scale3);
    }

    // resulting floor mesh is built following Y-up convention.
    public void BuildFloor(FloorZone floorzone) {
        var map = floorzone.Map;
        if (map == null) return;

        int2 map_size = new int2 { x = map[0].Length, y = map.Length };

        var planeUnitsAtScale1 = 1.0f;
        var planeSize = new float3(
            x: map_size.x * tileSizeUnits / planeUnitsAtScale1,
            y: 1,
            z: map_size.y * tileSizeUnits / planeUnitsAtScale1
        );

        origin = (planeSize * 0.5f) - planeSize;
        origin.y = 0;

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
			destvtx[i] = (origvtx[i] * planeSize) + new float3(0,miny,0);
        }

        mesh.vertices = destvtx;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        plane.GetComponent<BoxCollider>().size   = planeSize;
        plane.GetComponent<BoxCollider>().center = new float3(0, miny, 0);
    }

    public void BuildMap(int floor_id) {
        GlobalPool.floors[floor_id].map_id = floor_id;
        BuildMap(GlobalPool.floors[floor_id]);
    }

    public void BuildMap(int floor_id, int map_id) {
        GlobalPool.floors[floor_id].map_id = map_id;
        BuildMap(GlobalPool.floors[floor_id]);
    }

    public void BuildMap(FloorZone floor) {
        BuildCubeWallMesh(MapGridSize2, cubeWallHeight);
        BuildFloor(floor);

        transform.position = new float3(0,0,origin.x);
        var map = GlobalPool.maps[floor.map_id];
        int2 map_size = map.Size;

        Debug.Log($"origin = {origin}");

        for (int y=0; y<map_size.y; ++y) {
            for (int x=0; x<map_size.x; ++x) {
                if (map[x,y] == 'A' || map[x,y] == 'B') continue;
                if (map[x,y] == ' ') continue;

                // TODO: optimize - create raw gameobject and bind MeshFilter and BoxCollider procedurally...

                var startpos = TranslateGridCoordToWorld(new int2(x,y));
                var newcube  = Instantiate(cubeWallPrefab, floor.xformObj.transform);
                newcube.GetComponent<MeshFilter>().mesh = cubeWallMesh;
                floor.walls.Add(newcube);
                newcube.transform.localPosition = startpos;
                newcube.transform.localRotation = Quaternion.identity; // Quaternion.AngleAxis(90, float3.right);
                newcube.tag                     = "DynamicLevelObject";
            }
        }
    }

    public void RunDefinedCourse()
    {
        var curpos = new int2();
        var pathstate = new Yieldable.PathState();
        var map = GlobalPool.floors[0].Map;

        foreach (var pos in Yieldable.FindPath(map, pathstate)) {
            curpos = pos;
        }

        var stickatar = GameObject.Find("Stickatar");
        var stickpath = stickatar.GetComponent<FollowPath>();
        var waypoints = new List<int2>();

        var backtrack = curpos;
        while (backtrack.x !=0 && backtrack.y != 0) {
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
            visibleWallSizeScale = float3.zero;
            ReloadMaps();
        }

        if (restartPathRunner) {
            SetupAvatars();
        }

        // reset button-like toggles to 0
        rebuildMap = false;
        restartPathRunner = false;
    }
}
