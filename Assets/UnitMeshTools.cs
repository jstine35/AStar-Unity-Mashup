using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;

public class UnitMesh {
    public static void Scale(Vector2 size2, float height) {
    }

    public static Mesh Scale(Mesh mesh, float x, float y, float z) {
        return Scale(mesh, new Vector3(x,y,z));
    }

    public static Mesh Scale(Mesh mesh, Vector3 size3) {
		var origvtx = mesh.vertices;
		var destvtx = new Vector3[origvtx.Length];

        // Y coordinates below zero are left as-is.
        // Within our gameplay environment, such areas of a mesh are only for attaching
        // objects to floor panels, eg. most Unit Meshes have some amount of geo that extends
        // below the floor to close up gaps imposed by rounding errors.

        for (int i = 0; i < origvtx.Length; i++) {
            var localscale = size3;
            if (origvtx[i].y <= 0) localscale.y = 1;
			destvtx[i] = Vector3.Scale(origvtx[i], localscale);
        }

        mesh.vertices = destvtx;
        mesh.RecalculateBounds();       // fixes bad culling logics
        mesh.RecalculateNormals();      // fixes bad lighting

        // tangent calcs are tricky and not always applicable, leaving disabled for now until
        // such time that an issue prompts us to address it.
        //mesh.RecalculateTangents();     // fixes bad reflections

        return mesh;
    }

    public static void ScaleGO(GameObject obj, Vector3 size3) {
        // note that prefabs don't have a valid 'mesh' property. You must use sharedMesh.
        // the .mesh property itself is just shorthand for Instantiate(sharedMesh) so the
        // generic solution is to use Instantiate() for all GOs.

        // it is expected that a caller of this function will implement their own form of mesh cache
        // in the case of meshes which are re-used across many instantiated prefabs or GOs.
        var meshFilter = obj.GetComponent<MeshFilter>();
        var mesh = Object.Instantiate(meshFilter.sharedMesh);
        meshFilter.mesh = Scale(mesh, size3);
    }
}