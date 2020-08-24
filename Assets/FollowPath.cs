using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FollowPath : MonoBehaviour
{
    public float MoveSpeed = 6.0f;
    public List<Vector3> waypoints = new List<Vector3>();
    public Vector3 CurrentTarget = new Vector3();

    // Stops waypoint following
    public bool Suspended = false;

    // apply catmull-rom spline to waypoint list for smoother motion.
    public bool SplineMovement = true;

    // Parametric constant: 0.0 for the uniform spline, 0.5 for the centripetal spline, 1.0 for the chordal spline
    public float splineParametic  = 0.5f;

    Vector3 PastTarget   = new Vector3();
    Vector3 PrevTarget   = new Vector3();
    Vector3 FutureTarget = new Vector3();

    bool HasTarget = false;
    bool HasPastTarget = false;
    bool HasPrevTarget = false;
    bool HasFutureTarget = false;

    public void ApplyWaypointList() {
        HasTarget = false;      // will load up new waypoint on next update.
    }

    public void StopMovement() {
        Suspended = true;
        HasPastTarget = false;
        HasPrevTarget = false;
    }

    public void ClearAll() {
        waypoints.Clear();
        HasTarget = false;
        HasPastTarget = false;
        HasPrevTarget = false;
    }


    public Vector3 CatmulRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float dist_pct)
    {
        float t0 = 0.0f;
        float t1 = GetT(t0, p0, p1);
        float t2 = GetT(t1, p1, p2);
        float t3 = GetT(t2, p2, p3);

        // Pulled from wikipedia, modified to return a functional lerp rather than a series of segments. --jstine

        float t = Mathf.Lerp(t1, t2, dist_pct);

        Vector3 A1 = (t1-t)/(t1-t0)*p0 + (t-t0)/(t1-t0)*p1;
        Vector3 A2 = (t2-t)/(t2-t1)*p1 + (t-t1)/(t2-t1)*p2;
        Vector3 A3 = (t3-t)/(t3-t2)*p2 + (t-t2)/(t3-t2)*p3;
        Vector3 B1 = (t2-t)/(t2-t0)*A1 + (t-t0)/(t2-t0)*A2;
        Vector3 B2 = (t3-t)/(t3-t1)*A2 + (t-t1)/(t3-t1)*A3;
        Vector3 C = (t2-t)/(t2-t1)*B1 + (t-t1)/(t2-t1)*B2;

        return C;
    }

    float GetT(float t, Vector3 p0, Vector3 p1)
    {
        float a = Mathf.Pow((p1.x-p0.x), 2.0f) + Mathf.Pow((p1.y-p0.y), 2.0f) + Mathf.Pow((p1.z-p0.z), 2.0f);
        float b = Mathf.Pow(a, splineParametic * 0.5f);

        return (b + t);
    }

    Vector3 simulated_position;
    Vector3 starting_position;

    // Update is called once per frame
    void Update()
    {
        if (Suspended) return;

        if (!HasTarget) {
            HasFutureTarget = false;

            if (waypoints.Count > 0) {
                PastTarget = PrevTarget;
                PrevTarget = CurrentTarget;
                CurrentTarget = waypoints[0];
                if (waypoints.Count > 1) {
                    HasFutureTarget = true;
                    FutureTarget = waypoints[1];
                }
                waypoints.RemoveAt(0);
            }

            HasTarget = true;
            //Debug.Log($"Moving to Position: {CurrentTarget.x}, {CurrentTarget.z}");
            simulated_position = transform.position;
            starting_position  = transform.position;
        }

        // Movement smoothing.
        // simulate the linear movement from point to point in a straight line, and use that distance to
        // determine the current step along the curved path.
        //
        // This is a rushed implementation of non-segmented catmull-rom, which looks OK but results in non-
        // uniform movement speed across the curve. Online resources mutter ideals of derivatives, but those are
        // non-trivial and present other issues and estimations. I believe the easiest solution is to measure
        // the actual distance moved via catmull and compensate the straight-line simulation the next frame (or
        // could compensate on the current frame by adjusting and redoing the calculation). As long as we're within
        // some nominal movement speed over several frames, the illusion of consistency would be OK for the player.

        if (HasTarget) {
            float step = MoveSpeed * Time.deltaTime;

            simulated_position = Vector3.MoveTowards(simulated_position, CurrentTarget, step);

            if (SplineMovement && HasFutureTarget && HasPrevTarget && HasPastTarget) {
                float rem_dist     = Vector3.Distance(simulated_position, CurrentTarget);
                float total_dist   = Vector3.Distance(starting_position, CurrentTarget);
                float t = 1.0f - (rem_dist / total_dist);
                var curvepos = CatmulRom(PastTarget, PrevTarget, CurrentTarget, FutureTarget, t);
                transform.position = curvepos; //Vector3.MoveTowards(curvepos, CurrentTarget, step);
            }
            else {
                transform.position = simulated_position;
            }

            if (Vector3.Distance(transform.position, CurrentTarget) < 0.001f) {
                HasTarget = false;
                HasPastTarget = HasPrevTarget;
                HasPrevTarget = true;
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(CurrentTarget, 1.2f);

        Gizmos.color = Color.yellow;
        foreach(var waypoint in waypoints) {
            Gizmos.DrawSphere(waypoint, 0.4f);
        }
    }


    // Start is called before the first frame update
    void Start()
    {
    }
}
