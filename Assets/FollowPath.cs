using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ExecuteInEditMode]
public class FollowPath : MonoBehaviour
{
    [Range(4, 32)]
    public float moveSpeed = 6.0f;

    [Tooltip("apply catmull-rom spline to waypoint list for smoother motion.")]
    public bool splineMovement = true;

    [Tooltip("Parametric constant: 0.0 for the uniform spline, 0.5 for the centripetal spline, 1.0 for the chordal spline")]
    [Range(0.0f, 1.0f)]
    public float splineParametric  = 0.5f;

    [Tooltip("Stops waypoint following")]
    public bool suspended = false;

    public Vector3 currentTarget = new Vector3();
    public List<Vector3> waypoints = new List<Vector3>();

    Vector3 pastTarget   = new Vector3();
    Vector3 prevTarget   = new Vector3();
    Vector3 futureTarget = new Vector3();

    bool hasTarget = false;
    bool hasPastTarget = false;
    bool hasPrevTarget = false;
    bool hasFutureTarget = false;

    public void ApplyWaypointList() {
        hasTarget = false;      // will load up new waypoint on next update.
    }

    public void StopMovement() {
        suspended = true;
        hasPastTarget = false;
        hasPrevTarget = false;
    }

    public void ClearAll() {
        waypoints.Clear();
        hasTarget = false;
        hasPastTarget = false;
        hasPrevTarget = false;
    }


    public Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float dist_pct)
    {
        var t0 = 0.0f;
        var t1 = GetT(t0, p0, p1);
        var t2 = GetT(t1, p1, p2);
        var t3 = GetT(t2, p2, p3);

        // Pulled from wikipedia, modified to return a functional lerp rather than a series of segments. --jstine

        var t = Mathf.Lerp(t1, t2, dist_pct);

        var A1 = (t1-t)/(t1-t0)*p0 + (t-t0)/(t1-t0)*p1;
        var A2 = (t2-t)/(t2-t1)*p1 + (t-t1)/(t2-t1)*p2;
        var A3 = (t3-t)/(t3-t2)*p2 + (t-t2)/(t3-t2)*p3;
        var B1 = (t2-t)/(t2-t0)*A1 + (t-t0)/(t2-t0)*A2;
        var B2 = (t3-t)/(t3-t1)*A2 + (t-t1)/(t3-t1)*A3;
        var C  = (t2-t)/(t2-t1)*B1 + (t-t1)/(t2-t1)*B2;

        return C;
    }

    float GetT(float t, Vector3 p0, Vector3 p1)
    {
        var a = Mathf.Pow((p1.x-p0.x), 2.0f) + Mathf.Pow((p1.y-p0.y), 2.0f) + Mathf.Pow((p1.z-p0.z), 2.0f);
        var b = Mathf.Pow(a, splineParametric * 0.5f);

        return (b + t);
    }

    Vector3 simulated_position;
    Vector3 starting_position;

    // Update is called once per frame
    void Update()
    {
        if (suspended) return;

        if (!hasTarget) {
            hasFutureTarget = false;

            if (waypoints.Count > 0) {
                pastTarget = prevTarget;
                prevTarget = currentTarget;
                currentTarget = waypoints[0];
                if (waypoints.Count > 1) {
                    hasFutureTarget = true;
                    futureTarget = waypoints[1];
                }
                waypoints.RemoveAt(0);
            }

            hasTarget = true;
            //Debug.Log($"Moving to Position: {currentTarget.x}, {currentTarget.z}");
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

        if (hasTarget) {
            var step = moveSpeed * Time.deltaTime;

            simulated_position = Vector3.MoveTowards(simulated_position, currentTarget, step);

            if (splineMovement && hasFutureTarget && hasPrevTarget && hasPastTarget) {
                var rem_dist     = Vector3.Distance(simulated_position, currentTarget);
                var total_dist   = Vector3.Distance(starting_position, currentTarget);
                var t = 1.0f - (rem_dist / total_dist);
                var curvepos = CatmullRom(pastTarget, prevTarget, currentTarget, futureTarget, t);
                transform.position = curvepos; //Vector3.MoveTowards(curvepos, currentTarget, step);
            }
            else {
                transform.position = simulated_position;
            }

            if (Vector3.Distance(transform.position, currentTarget) < 0.001f) {
                hasTarget = false;
                hasPastTarget = hasPrevTarget;
                hasPrevTarget = true;
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(currentTarget, 1.2f);

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
