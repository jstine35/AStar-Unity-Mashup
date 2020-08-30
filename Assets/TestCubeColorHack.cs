using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class TestCubeColorHack : MonoBehaviour
{
    public  Color32    hiColor = new Color32(255, 32, 0, 255);
    public  Color32    loColor = new Color32(255, 255, 32, 255);
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
                newColors[i] = Color.Lerp(loColor, hiColor, mesh.vertices[i].y + 0.5f);
            }

            mesh.colors32 = newColors;
        }
        else if (mesh == null)
        {
            load();
        }
    }

    void Update()
    {
    }
}
