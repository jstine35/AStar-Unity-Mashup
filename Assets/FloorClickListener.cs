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

        var ray   = cam.ScreenPointToRay(Input.mousePosition);
        var plane = new Plane(gameObject.transform.up, gameObject.transform.position);

        if (plane.Raycast(ray, out var enter))
        {
            var main = GameObject.Find("SystemInit").GetComponent<main>();
            var coord = main.TranslateWorldCoordToGrid(ray.GetPoint(enter));
            
            var stickatar = GameObject.Find("Stickatar");
            var pathcom = stickatar.GetComponent<FollowPath>();
            
            // cast a ray straight down toward the gameboard.

            //Debug.DrawRay(stickatar.transform.position, -gameObject.transform.up * 20, Color.red, 100);
            Debug.DrawRay(stickatar.transform.position,  gameObject.transform.up * 20, Color.red, 100);
            var stickray = new Ray(stickatar.transform.position + (gameObject.transform.up*5), -gameObject.transform.up);
            if (plane.Raycast(stickray, out var stickrayintersect)) {
                var stickcoord = main.TranslateWorldCoordToGrid(stickray.GetPoint(stickrayintersect));
                var start  = new int2((int)Mathf.RoundToInt(stickcoord.x), (int)Mathf.RoundToInt(stickcoord.y));
                var target = new int2((int)coord.x, (int)coord.y);

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

    void Update()
    {
    }
}
