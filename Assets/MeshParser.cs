using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public static class MeshParser
{
    public static Mesh ParseOBJ(string objString)
    {
        // Create a new mesh
        Mesh mesh = new Mesh();

        // Lists to store the vertex, normal, and UV data
        List<Vector3> objVertices = new List<Vector3>();
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> objNormals = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> objUvs = new List<Vector2>();
        List<Vector2> uvs = new List<Vector2>();

        // List to store the triangle indices
        List<int> triangles = new List<int>();
        int index = 0;

        // Split the objString into lines
        string[] lines = objString.Split('\n');

        // Process each line
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            // Check the line type
            if (line.StartsWith("v "))
            {
                // Process vertex data
                string[] vertexData = line.Split(' ');
                float x = float.Parse(vertexData[1], CultureInfo.InvariantCulture.NumberFormat);
                float y = float.Parse(vertexData[2], CultureInfo.InvariantCulture.NumberFormat);
                float z = float.Parse(vertexData[3], CultureInfo.InvariantCulture.NumberFormat);
                objVertices.Add(new Vector3(x, y, z) / 8);
            }
            else if (line.StartsWith("vn "))
            {
                // Process normal data
                string[] normalData = line.Split(' ');
                float x = float.Parse(normalData[1], CultureInfo.InvariantCulture.NumberFormat);
                float y = float.Parse(normalData[2], CultureInfo.InvariantCulture.NumberFormat);
                float z = float.Parse(normalData[3], CultureInfo.InvariantCulture.NumberFormat);
                objNormals.Add(new Vector3(x, y, z));
            }
            else if (line.StartsWith("vt "))
            {
                // Process UV data
                string[] uvData = line.Split(' ');
                float x = float.Parse(uvData[1], CultureInfo.InvariantCulture.NumberFormat);
                float y = float.Parse(uvData[2], CultureInfo.InvariantCulture.NumberFormat);
                objUvs.Add(new Vector2(x, y));
            }
            else if (line.StartsWith("f "))
            {
                // Process face data
                string[] faceData = line.Split(' ');

                for (int j = 1; j < faceData.Length; j++)
                {
                    string[] vertexData = (faceData[j] + "//").Split('/');

                    int vertexIndex = int.Parse(vertexData[0]) - 1;
                    vertices.Add(objVertices[vertexIndex]);

                    if (objUvs.Count > 0)
                    {
                        int uvIndex = int.Parse((vertexData[1] + vertexData[0]).Substring(0, 1)) - 1;
                        uvs.Add(objUvs[uvIndex]);
                    }

                    int normalIndex = int.Parse((vertexData[2] + vertexData[1] + vertexData[0]).Substring(0, 1)) - 1;
                    normals.Add(objNormals[normalIndex]);

                    triangles.Add(index++);
                }
            }
        }

        // Assign the vertex, normal, and UV data to the mesh
        mesh.vertices = vertices.ToArray();
        mesh.normals = normals.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateBounds();

        return mesh;
    }
}
