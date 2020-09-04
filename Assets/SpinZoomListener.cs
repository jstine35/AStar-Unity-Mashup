using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpinZoomListener : MonoBehaviour
{
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

    // Start is called before the first frame update
    void Start()
    {
        switch(ZoomAngle) {
            case ZoomViewAngle.LowPersp : persp_angles_curr = new Vector2(30, -30) ; break;
            case ZoomViewAngle.HighPersp: persp_angles_curr = new Vector2(50, -40) ; break;
        }

        var xform = gameObject.transform;
        persp_angles_targ = persp_angles_curr;
        var angles = xform.localRotation.eulerAngles;
        xform.localRotation = Quaternion.Euler(persp_angles_curr.x, persp_angles_curr.y, angles.z);
    }

    // Update is called once per frame
    void Update()
    {
        bool doSpin = false;
        if (Input.GetKeyDown("z")) {
            ZoomLevel = (ZoomLevel % 3) + 1; 
        }

        var xform = gameObject.transform;

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
    }
}
