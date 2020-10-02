using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpinZoomListener : MonoBehaviour
{
    public enum ZoomViewAngle {
        LowPersp,
        HighPersp,
    }
    
#region Public Fields (Unity Editable)
    [Range(1.0f, 3.0f)]
    public int zoomLevel = 0;
    public Vector3 lookAtTargetPos;

    public float[] cameraHeights = new float[2] {
        -8.0f,
        -21.0f
    };

    public float[] cameraRadiuses = new float[2] {
        12.0f,
        24.0f
    };

    public float rotationSpeed = 120.0f;
    public GameObject gameBoard = null;
#endregion

    bool        lastMouseIsSpinning = false;
    Vector3     lastMouseSpinViewPos;
    Quaternion  spinOrient;
    float       spinOrientY = 45;

    float       curr_CameraHeight;
    float       curr_CameraRadius;
    float       targ_CameraHeight;
    float       targ_CameraRadius;

    int       cursel_Height = 0;
    int       cursel_Radius = 0;

    public float CurrentCameraHeight { get => curr_CameraHeight; }
    public float CurrentCameraRadius { get => curr_CameraRadius; }

    
    void ApplyOrientation() {
        // position the camera according to radius and height settings.

        var viewHeight = CurrentCameraHeight;
        var radius     = CurrentCameraRadius;
        gameObject.transform.position = new Vector3(radius, -viewHeight, 0);
        gameObject.transform.RotateAround(Vector3.zero, Vector3.up, spinOrientY);

        gameObject.transform.LookAt(lookAtTargetPos);
    }

    // Start is called before the first frame update
    void Start()
    {
        curr_CameraHeight = cameraHeights [cursel_Height];
        curr_CameraRadius = cameraRadiuses[cursel_Radius];

        targ_CameraHeight = cameraHeights [cursel_Height];
        targ_CameraRadius = cameraRadiuses[cursel_Radius];
        ApplyOrientation();
    }

    public void MoveTowards(ref float curr, ref float targ, float delta) {
        if (targ != curr) {
            curr = Mathf.MoveTowards(curr, targ, rotationSpeed * Time.deltaTime);
            if (Mathf.Abs(targ - curr) < 0.01f) {
                curr = targ;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        bool doSpin = false;
        if (Input.GetKeyDown("z")) {
            zoomLevel = (zoomLevel % 3) + 1; 
        }

        var cam = gameObject.GetComponent<Camera>();

        if (cam != null) {
            zoomLevel = Mathf.Clamp(zoomLevel, 1, 3);
            switch(zoomLevel) {
                case 1: cam.orthographicSize = 36; break;
                case 2: cam.orthographicSize = 26; break;
                case 3: cam.orthographicSize = 18; break;
            }
        }

        var xform = gameObject.transform;

        if (Input.GetKeyDown("x")) {
            cursel_Height = (cursel_Height + 1) % cameraHeights.Length;
            targ_CameraHeight = cameraHeights[cursel_Height];
        }
        
        if (Input.GetKeyDown("c")) {
            cursel_Radius = (cursel_Radius + 1) % cameraRadiuses.Length;
            targ_CameraRadius = cameraRadiuses[cursel_Radius];
        }

        MoveTowards(ref curr_CameraHeight, ref targ_CameraHeight, rotationSpeed);
        MoveTowards(ref curr_CameraRadius, ref targ_CameraRadius, rotationSpeed);

        if (Input.GetKey("left ctrl")) {
            if (Input.GetMouseButton(0)) {
                doSpin = true;
                if (!lastMouseIsSpinning) {
                    lastMouseIsSpinning = true;
                }
                else {
                    var delta = Input.mousePosition - lastMouseSpinViewPos;
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
