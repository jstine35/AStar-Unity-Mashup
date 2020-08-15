using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FollowPath : MonoBehaviour
{
    public float MoveSpeed = 6.0f;
    public List<Vector3> waypoints = new List<Vector3>();
    public Vector3 CurrentTarget = new Vector3();
    public bool HasTarget = false;
    public bool Suspended = false;

    public void SetWaypointList(List<Vector3> list) {
        waypoints = list;
        HasTarget = false;      // will load up new waypoint on next update.
    }

    public void StopMovement() {
        Suspended = true;
    }

    public void ClearAll() {
        waypoints.Clear();
        HasTarget = false;
    }

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (Suspended) return;

        if (!HasTarget && waypoints.Count > 0) {
            CurrentTarget = waypoints.First();
            waypoints.RemoveAt(0);
            HasTarget = true;
            Debug.Log($"Moving to Position: {CurrentTarget.x}, {CurrentTarget.z}");
        }

        if (HasTarget) {
            float step = MoveSpeed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, CurrentTarget, step);
            if (Vector3.Distance(transform.position, CurrentTarget) < 0.001f) {
                HasTarget = false;
            }
        }
    }
}
