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

    bool    lastMouseIsSpinning = false;
    Vector3 lastMouseSpinViewPos;
    Vector3 spinOrient;
    
    public enum ZoomViewAngle {
        LowPersp,
        HighPersp,
    }
    
    [Range(1.0f, 3.0f)]
    public int ZoomLevel;
    public ZoomViewAngle ZoomAngle;

    public float RotationSpeed = 120.0f;
    public Vector2 PerspectiveAnglesLo = new Vector2(50.0f, -36.0f);
    public Vector2 PerspectiveAnglesHi = new Vector2(30.0f, -18.0f);
    
    Vector2 persp_angles_curr;
    Vector2 persp_angles_targ;
    
    void Start()
    {
        switch(ZoomAngle) {
            case ZoomViewAngle.LowPersp : persp_angles_curr = new Vector2(30, -30) ; break;
            case ZoomViewAngle.HighPersp: persp_angles_curr = new Vector2(50, -40) ; break;
        }

        // TODO: attach rotation things directly to GameBoardTransform.
        var xform = GameObject.Find("GameBoardTransform");

        persp_angles_targ = persp_angles_curr;
        var angles = xform.transform.localRotation.eulerAngles;
        xform.transform.localRotation = Quaternion.Euler(persp_angles_curr.x, persp_angles_curr.y, angles.z);
    }

    void Update()
    {
        bool doSpin = false;
        if (Input.GetKeyDown("z")) {
            ZoomLevel = (ZoomLevel % 3) + 1; 
        }

        // TODO: attach rotation things directly to GameBoardTransform.
        var xform = GameObject.Find("GameBoardTransform");

        if (Input.GetKeyDown("x")) {
            switch(ZoomAngle) {
                case ZoomViewAngle.LowPersp : ZoomAngle = ZoomViewAngle.HighPersp; break;
                case ZoomViewAngle.HighPersp: ZoomAngle = ZoomViewAngle.LowPersp ; break;
            }
            
            switch(ZoomAngle) {
                case ZoomViewAngle.LowPersp  : persp_angles_targ = new Vector2(30, -30); break;
                case ZoomViewAngle.HighPersp : persp_angles_targ = new Vector2(50, -40); break;
            }
        }
        
        if (persp_angles_targ != persp_angles_curr) {
            persp_angles_curr = Vector2.MoveTowards(persp_angles_curr, persp_angles_targ, RotationSpeed * Time.deltaTime);
            var angles = xform.transform.localRotation.eulerAngles;
            xform.transform.localRotation = Quaternion.Euler(persp_angles_curr.x, persp_angles_curr.y, angles.z);
            
            if (Vector2.Distance(persp_angles_curr, persp_angles_targ) < 0.01f) {
                persp_angles_curr = persp_angles_targ;
            }
        }

        if (Input.GetKey("left ctrl")) {
            if (Input.GetMouseButton(0)) {
                doSpin = true;

                if (!lastMouseIsSpinning) {
                    lastMouseIsSpinning = true;
                    spinOrient = xform.transform.localRotation.eulerAngles;
                }
                else {
                    var delta = Input.mousePosition - lastMouseSpinViewPos;
                    spinOrient.z += delta.x / 6;
                    xform.transform.localRotation = Quaternion.Euler(spinOrient);
                }
                lastMouseSpinViewPos = Input.mousePosition;
            }
        }
        lastMouseIsSpinning = doSpin;
    
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
