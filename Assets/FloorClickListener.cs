using System.Collections;
using System.Collections.Generic;
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
            var hitPoint = ray.GetPoint(enter);
            var main = GameObject.Find("SystemInit").GetComponent<main>();
            var coord = main.TranslateWorldCoordToGrid(hitPoint);
            
            var stickatar = GameObject.Find("Stickatar");
            
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
