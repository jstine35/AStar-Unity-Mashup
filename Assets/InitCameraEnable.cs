using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InitCameraEnable : MonoBehaviour
{
    public bool Enabled = false;

    // Start is called before the first frame update
    void Start()
    {
        GetComponent<Camera>().enabled = Enabled;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
