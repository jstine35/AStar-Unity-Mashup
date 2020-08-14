using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AStar;

public class main : MonoBehaviour
{
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
        Debug.Log("This is a test...");
        var notsure = GameObject.Find("Plane");

        var curpos = new int2();
        var pathstate = new Yieldable.PathState();

        foreach (var pos in Yieldable.FindPath(map, pathstate)) {
            curpos = pos;
            Debug.Log($"NavPos: {curpos.x} x {curpos.y}");
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
