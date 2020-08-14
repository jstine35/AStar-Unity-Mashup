using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class CubeCreator : MonoBehaviour
{
    public Vector3 scale = new Vector3(1,6,1);

    // Start is called before the first frame update
    void Start()
    {
        //var plane    = GameObject.Find("Plane");
        //var bounds   = plane.GetComponent<Renderer>().bounds;
        //var position = plane.GetComponent<Transform>().position;
        //var rotation = plane.GetComponent<Transform>().rotation;
        //
        gameObject.GetComponent<Transform>().localScale = scale;
        //gameObject.GetComponent<Transform>().SetPositionAndRotation(position, rotation);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
