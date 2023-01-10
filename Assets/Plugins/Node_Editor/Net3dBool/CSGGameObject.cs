using System.Collections.Generic;
using Net3dBool;
using UnityEngine;

public class CSGGameObject
{

    public static IEnumerator<float> GenerateMesh(GameObject go, Material ObjMaterial, Solid mesh)
    {

        var verts = mesh.getVertices();
        int mlen = verts.Length;
        yield return 0.9f;
        var colors = mesh.getColors();
        int clen = colors.Length;

        MeshFilter mf = go.GetComponent<MeshFilter>();
        if (mf == null)
            mf = go.AddComponent<MeshFilter>();

        Mesh tmesh = new Mesh();
        Vector3[] vertices = new Vector3[mlen];
        for (int i = 0; i < mlen; i++)
        {
            Net3dBool.Point3d p = verts[i];
            vertices[i] = new Vector3((float)p.x, (float)p.y, (float)p.z);
            if (i % 1 == 0)
                yield return (i) / (mlen + clen);
        }
        tmesh.vertices = vertices;
        tmesh.triangles = mesh.getIndices();
        Color[] clrs = new Color[clen];
        for (int j = 0; j < clen; j++)
        {
            Net3dBool.Color3f c = colors[j];
            clrs[j] = new Color((float)c.r, (float)c.g, (float)c.b);
            if (j % 1 == 0)
                yield return (mlen + j) / (mlen + clen);
        }
        tmesh.colors = clrs;
        tmesh.RecalculateNormals();
        yield return 1.05f;
        mf.mesh = tmesh;

        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        if (mr == null) mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterials = new Material[1];
        mr.sharedMaterials[0] = ObjMaterial;
        mr.sharedMaterial = ObjMaterial;
    }
}
