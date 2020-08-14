using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AStar;

public class main : MonoBehaviour
{
    public GameObject CubeWallPrefab;

    string[] map = new string[]
    {
        "+-----------------+",
        "|                 |",
        "| Xxxxxxxxxxxxx   |",
        "|  x              |",
        "|x  x             |",
        "|x x              |",
        "|  x A X          |",
        "| XXXXXX  X       |",
        "|    x  XX        |",
        "|   Bx X          |",
        "|xxxxx  XXXXX     |",
        "|                 |",
        "|                 |",
        "+-----------------+",
    };

    // Start is called before the first frame update
    void Start()
    {
        var plane    = GameObject.Find("Plane");
        var bounds   = plane.GetComponent<Renderer>().bounds;
        var position = plane.GetComponent<Transform>().position;
        var rotation = plane.GetComponent<Transform>().rotation;

        //gameObject.GetComponent<Transform>().localScale = scale;
        //gameObject.GetComponent<Transform>().SetPositionAndRotation(position, rotation);

        Vector3 origin = new Vector3(bounds.min.x, 0, bounds.min.z);
        for (int i=0; i<16; ++i) {
            var startpos = new Vector3(
                origin.x + (i & 7) * 3, 0,
                origin.z + (i % 8) * 3
            );
            Instantiate(CubeWallPrefab, startpos, Quaternion.identity, plane.GetComponent<Transform>());
        }

        var curpos = new int2();
        var pathstate = new Yieldable.PathState();

        foreach (var pos in Yieldable.FindPath(map, pathstate)) {
            curpos = pos;
            //Debug.Log($"NavPos: {curpos.x} x {curpos.y}");
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
