using System.IO;
using System.Text;
using UnityEngine;

public static class MeshExporter
{
    public static string ToOBJ(Mesh mesh)
    {
        StringBuilder sb = new StringBuilder();

        // Write the vertex data
        foreach (Vector3 v in mesh.vertices)
        {
            sb.AppendFormat("v {0} {1} {2}\n", v.x, v.y, v.z);
        }
        sb.AppendLine();

        // Write the normal data
        foreach (Vector3 n in mesh.normals)
        {
            sb.AppendFormat("vn {0} {1} {2}\n", n.x, n.y, n.z);
        }
        sb.AppendLine();

        // Write the UV data
        foreach (Vector2 uv in mesh.uv)
        {
            sb.AppendFormat("vt {0} {1}\n", uv.x, uv.y);
        }
        sb.AppendLine();

        // Write the face data
        for (int i = 0; i < mesh.triangles.Length; i += 3)
        {
            sb.AppendFormat("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n",
                mesh.triangles[i] + 1, mesh.triangles[i + 1] + 1, mesh.triangles[i + 2] + 1);
        }

        return sb.ToString();
    }

}
