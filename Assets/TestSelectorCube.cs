using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestSelectorCube : MonoBehaviour
{
    public  Color32    intensity = new Color32(255, 32, 0, 255);
    private Mesh       mesh;
    public  Mesh       org;
    private MeshFilter mf;
    private bool       isenabled = false;

    void OnEnable()
    {
        if (org != null)
        {
            load();
        }
    }

    void OnDisable()
    {
        if (isenabled)
        {
            mf.sharedMesh = org;
            mf.name       = org.name;
        }
    }

    void load()
    {
        mf            = transform.GetComponent<MeshFilter>();
        mesh          = Mesh.Instantiate(org) as Mesh;
        mf.sharedMesh = mesh;
        updatecolor();
        isenabled = true;
    }

    void OnValidate()
    {
        updatecolor();
    }

    void updatecolor()
    {
        if (isenabled)
        {
            Color32[] newColors = new Color32[mesh.vertices.Length];
            for (int i = 0; i < newColors.Length; i++)
            {
                newColors[i] = Color.Lerp(intensity, Color.black, mesh.vertices[i].y + 0.5f);
            }

            mesh.colors32 = newColors;
        }
        else if (mesh == null)
        {
            load();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
