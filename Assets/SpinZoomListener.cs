using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public struct MoveTowardsPair {
    public float curr;
    public float targ;
    public float move_per_sec;

    public static implicit operator float(MoveTowardsPair pair) => pair.curr;

    public MoveTowardsPair(float initial) {
        curr = initial;
        targ = initial;
        move_per_sec = 0;
    }

    public void SetTarget(float newval) => targ = newval;

    public void SetTarget(float newval, float time_to_target) {
        move_per_sec = Mathf.Abs(newval - curr) / time_to_target;
        targ = newval;
    }

    public void SetImm(float newval) {
        curr = newval;
        targ = newval;
    }

    public void Update() {
        Debug.Assert(move_per_sec == 0, $"to use Update() API, SetTarget() call must include a time_to_target value");
        MoveBy(move_per_sec);
    }

    public void MoveBy(float delta) {
        if (targ == curr) return;
        curr = Mathf.MoveTowards(curr, targ, delta * Time.deltaTime);
        if (Mathf.Abs(targ - curr) < 0.01f) {
            curr = targ;
        }
    }

};


public class SpinZoomListener : MonoBehaviour
{
    public enum ZoomViewAngle {
        LowPersp,
        HighPersp,
    }
    
#region Public Fields (Unity Editable)
    public float3 lookAtTargetPos;

    public float[] viewAngles = new float[] {
        24,
        45
    };

    public float[] viewSizes = new float[] {
        18,
        26,
        36
    };

    public float rotationTime = 0.25f;
#endregion

    bool        lastMouseIsSpinning = false;
    float3      lastMouseSpinViewPos;
    float       spinOrientY = 45;

    MoveTowardsPair  ViewAngle;
    MoveTowardsPair  ViewSize;

    int       cursel_Angle = 0;
    int       cursel_Size = 0;

    void ApplyOrientation() {
        // position the camera according to view angle and view size.
        // view size becomes the radius, or right-triangle leg perpendicular to the Y axis.
        // Combined with angle, we can calculate the second leg, which is height.
        // legA = legB * tan(theta)  // some law of trig!

        var radius      = ViewSize;
        var height      = radius * Mathf.Tan(ViewAngle * Mathf.Deg2Rad);

        gameObject.transform.position = new float3(radius, height, 0) + lookAtTargetPos;
        gameObject.transform.RotateAround(Vector3.zero, Vector3.up, spinOrientY);

        gameObject.transform.LookAt(lookAtTargetPos);

        var cam = GetComponent<Camera>();
        cam.orthographicSize = ViewSize;
    }

    // Start is called before the first frame update
    void Start()
    {
        ViewAngle.SetImm(viewAngles [cursel_Angle] );
        ViewSize .SetImm(viewSizes  [cursel_Size ] );

        ApplyOrientation();
    }

    // Update is called once per frame
    void Update()
    {
        bool doSpin = false;
        if (Input.GetKeyDown("z")) {
            cursel_Size = (cursel_Size + 1) % viewSizes.Length;
            ViewSize.SetTarget(viewSizes[cursel_Size], rotationTime);
        }

        var xform = gameObject.transform;

        if (Input.GetKeyDown("x")) {
            cursel_Angle = (cursel_Angle + 1) % viewAngles.Length;
            ViewAngle.SetTarget(viewAngles[cursel_Angle], rotationTime);
        }
        
        ViewAngle.Update();
        ViewSize .Update();

        if (Input.GetKey("left ctrl")) {
            if (Input.GetMouseButton(0)) {
                doSpin = true;
                if (!lastMouseIsSpinning) {
                    lastMouseIsSpinning = true;
                }
                else {
                    var delta = (float3)Input.mousePosition - lastMouseSpinViewPos;
                    spinOrientY += delta.x / 6;
                }
                lastMouseSpinViewPos = Input.mousePosition;
            }
        }
        lastMouseIsSpinning = doSpin;
        ApplyOrientation();
    }
}

#if false
// TODO: realistically depends on input system package import.
//    (ok, could reinvent the wheel for input systems but don't care)
// Let's fix up basic architercture of camera operation first.
public class CameraPanHandler : MonoBehaviour
{
    void Update() {
        if (Input.GetKey("left ctrl")) {
        }
    }
}
#endif
