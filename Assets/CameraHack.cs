using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraHack : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        var cam = gameObject.GetComponent<Camera>();

        // find the angle-axis between the camera position and the center of the world.
        // 
        // To find the angle to an arbitrary point, calculate the vector displacement to that point.
        //  (camera.pos - lookAt.pos) against forward.

        var posnorm = (gameObject.transform.position - new Vector3(0,10,0)).normalized;
        var dot = Vector3.Dot(posnorm, Vector3.forward);
        var cross = Vector3.Cross(posnorm, Vector3.forward);
        var acos = Mathf.Acos(dot);
        gameObject.transform.localRotation = Quaternion.AngleAxis(180 - (acos * Mathf.Rad2Deg), cross.normalized);

        //gameObject.transform.LookAt(Vector3.up);
        
    }
}
