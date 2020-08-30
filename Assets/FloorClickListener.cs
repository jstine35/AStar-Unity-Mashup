using System.Collections;
using System.Collections.Generic;
using AStar;
using UnityEngine;

public class FloorClickListener : MonoBehaviour
{
    void OnMouseDown()
    {
        var cam = Camera.main; 
        if (cam is null) return;

        var msepos = Input.mousePosition;
        if(cam.orthographic) {
            msepos.z = 0;
        }

        var ray   = cam.ScreenPointToRay(msepos);
        var plane = new Plane(gameObject.transform.forward, gameObject.transform.position);

        if (plane.Raycast(ray, out var enter))
        {
            var main = GameObject.Find("SystemInit").GetComponent<main>();
            var inv = gameObject.transform.parent.InverseTransformPoint(ray.GetPoint(enter));
            var coord = main.TranslateWorldCoordToGrid(inv);
            
            var stickatar = GameObject.Find("Stickatar");
            var pathcom = stickatar.GetComponent<FollowPath>();

            // cast a ray straight down toward the gameboard.

            //Debug.DrawRay(stickatar.transform.position, -gameObject.transform.up * 20, Color.red, 100);
            //Debug.DrawRay(stickatar.transform.position - (gameObject.transform.forward*5), gameObject.transform.forward * 20, Color.red, 100);

            var stickray = new Ray(stickatar.transform.position - (gameObject.transform.forward*5), gameObject.transform.forward);
            if (plane.Raycast(stickray, out var stickrayintersect)) {
                var invpos = stickatar.transform.parent.InverseTransformPoint(stickray.GetPoint(stickrayintersect));
                var stickcoord = main.TranslateWorldCoordToGrid(invpos);
                var start  = new int2((int)(stickcoord.x), (int)(stickcoord.y));
                var target = new int2((int)(coord.x), (int)(coord.y));

                if (target.x < 0 || target.y < 0) return;
                if (target.y >= main.map.Length) return;
                if (target.x >= main.map[0].Length) return;

                var path_iter = Yieldable.FindPath(GlobalPool.map, GlobalPool.yieldablePathState, start, target);
                GlobalPool.yPathWaypoints.Clear();
                GlobalPool.AppendWaypoints(path_iter);
                
                var stickpath = stickatar.GetComponent<FollowPath>();
                stickpath.waypoints.Clear();

                var waypoints = GlobalPool.yPathWaypoints; 
                var startidx = waypoints.Count-1;
                var walkstart = waypoints[startidx];
                stickpath.waypoints.Add(main.TranslateGridCoordToWorld(walkstart));

                for (int i=startidx-1; i >= 0; --i) {
                    var next = waypoints[i];
                    stickpath.waypoints.Add(main.TranslateGridCoordToWorld(next));
                }
                stickpath.ApplyWaypointList();
            }
            
            //var start = pathcom.GetMapPos2D();
            //AStar.Yieldable.FindPath(main.map, GlobalPathState.state, start, target);                
            //stickatar.GetComponent<FollowPath>().ApplyWaypointList();
            
            // Move cube GameObject to the point clicked
            // m_Cube.transform.position = hitPoint;
        }
    }

    void Start()
    {
    }

#if false
    void OnDrawGizmos()
    {
        var cam = Camera.main; 
        if (cam is null) return;

        var ray   = cam.ScreenPointToRay(Input.mousePosition);
        var plane = new Plane(gameObject.transform.up, gameObject.transform.position);

        if (plane.Raycast(ray, out var enter))
        {
            var main = GameObject.Find("SystemInit").GetComponent<main>();
            var coord = main.TranslateWorldCoordToGrid(ray.GetPoint(enter));
            var target = new int2((int)(coord.x + 0.5f), (int)(coord.y + 0.5f));
            
            DbgPointF = coord;
            
            Gizmos.DrawCube(main.TranslateGridCoordToWorld(target), new Vector3(main.tileSizeUnits,main.tileSizeUnits,2));
        }
    }
#endif
    
    Vector3 dbg_Mouseish;
    Vector3 dbg_DbgPoint3;
    Vector3 dbg_DbgPointInv;
    Vector3 dbg_rayorigin;
    Vector3 dbg_raydir;

    void Update()
    {
        var cam = Camera.main; 
        if (cam is null) return;

        var msepos = Input.mousePosition;
        if(cam.orthographic) {
            msepos.z = 0;
        }

        var ray   = cam.ScreenPointToRay(msepos);
        var plane = new Plane(gameObject.transform.forward, gameObject.transform.position);
        
        //Debug.DrawLine(gameObject.transform.position, gameObject.transform.up * 50, Color.cyan); 
        //Debug.DrawRay(ray.origin, ray.direction*250, Color.cyan, 25);

        dbg_Mouseish = msepos;
        dbg_rayorigin = ray.origin;
        dbg_raydir = ray.direction;
        
        if (plane.Raycast(ray, out var enter))
        {
            var main = GameObject.Find("SystemInit").GetComponent<main>();
            var epoint = ray.GetPoint(enter);
            var inv = gameObject.transform.parent.InverseTransformPoint(epoint);
            var coord = main.TranslateWorldCoordToGrid(inv);
            var target = new int2((int)coord.x,(int) coord.y);

            dbg_Mouseish = coord; 
            dbg_DbgPoint3 = epoint;
            dbg_DbgPointInv = inv;
            
            if (target.x < 0 || target.y < 0) return;
            if (target.y >= main.map.Length) return;
            if (target.x >= main.map[0].Length) return;
            var meh = main.TranslateGridCoordToWorld(target);
            main.tileSelector.transform.localPosition = new Vector3(
                meh.x, meh.y, 0
            );
        }
    }
}
