using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


public static class Vector3Extension
{
    // override Vector3.Equals
    public static bool Equals(this Vector3 self, Vector3 obj)
    {
        return obj.x == self.x && obj.y == self.y && obj.z == self.z;
    }
}

class OcclusionFilter
{
    public bool Back;
    public bool Front;
    public bool Under;
    public bool Over;
    public bool Right;
    public bool Left;
}

public class TerrainGenerator : MonoBehaviour
{
    public int terrainSize = 10;
    public GameObject target;

    private bool[,,] voxels;
    private bool[,,] voxelsDrawn;
    private MeshCollider meshCollider;
    private MeshFilter meshFilter;
    private Mesh mesh;
    private List<Vector3> vertices;
    private List<int> triangles;
    private Dictionary<Vector3, int> vertexIndexMap;
    private Vector3 readyVoxels;
    private int drawnVoxelSize;
    public static bool queueRender = false;

    void Start()
    {
        voxels = new bool[terrainSize, terrainSize, terrainSize];
        voxelsDrawn = new bool[terrainSize, terrainSize, terrainSize];
        meshFilter = gameObject.AddComponent<MeshFilter>();
        mesh = new Mesh();
        mesh.name = "Terrain";
        meshFilter.mesh = mesh;
        meshCollider = gameObject.AddComponent<MeshCollider>();
        vertices = new List<Vector3>();
        triangles = new List<int>();
        vertexIndexMap = new Dictionary<Vector3, int>();
        readyVoxels = Vector3.zero;
        drawnVoxelSize = 0;
        StartCoroutine(BuildTerrain());
        StartCoroutine(DrawTerrain());
        StartCoroutine(RecalculateMesh());
    }

    void OnDestroy()
    {

        // Convert the mesh to an .obj file string
        string objString = MeshExporter.ToOBJ(mesh);

        // Save the .obj file string to a file
        string filePath = "MyMesh.obj";
        File.WriteAllText(filePath, objString);
    }

    private IEnumerator BuildTerrain()
    {
        Vector3 position = transform.position;

        yield return new WaitUntil(() => target != null);
        Vector3 pos = target.transform.position;
        readyVoxels = new Vector4(pos.x, 1, pos.z);
        var size = terrainSize;
        voxels = new bool[size, size, size];
        triangles = new List<int>();

        int x = (int)pos.x; int z = (int)pos.z; // Start at the middle of the 2D plane
        int direction = 0; //0: right, 1: down, 2: left, 3: up
        int steps = 1; int side_length = 1; // Number of steps to take in the current direction and the length of the current side
        int turns = 0; // Number of turns made so far
        while (true)
        {
            for (int i = 0; i < steps; i++)
            {
                yield return null;
                Vector3 new_pos = target.transform.position;
                if ((int)pos.x != (int)new_pos.x || (int)pos.z != (int)new_pos.z)
                {
                    pos = new_pos;
                    side_length = 1;
                    steps = side_length;
                    turns = 0;
                    x = (int)pos.x;
                    z = (int)pos.z;
                    readyVoxels = new Vector4(x, 0, z);
                    continue;
                }
                if ((x >= 0) && (x < size) && (z >= 0) && (z < size) && !voxelsDrawn[x, 1, z])
                {
                    float noise = size * Mathf.PerlinNoise((position.x + x) * 1f / size, (position.z + z) * 1f / size);
                    //noise += size/2f * (Mathf.PerlinNoise((position.x + x) * 2f / size, (position.z + z) * 2f / size)-.5f);
                    //noise += size/4f * (Mathf.PerlinNoise((position.x + x) * 4f / size, (position.z + z) * 4f / size)-.5f);
                    //noise += size / 8f * (Mathf.PerlinNoise((position.x + x) * 8f / size, (position.z + z) * 8f / size) - .5f);
                    noise += size / 16f * (Mathf.PerlinNoise((position.x + x) * 16f / size, (position.z + z) * 16f / size) - .5f);
                    for (int y = (int)Mathf.Clamp(noise, 0, size); y > 0; y--)
                    {
                        // Save the voxel
                        voxels[x, y, z] = voxels[x, y, z] || true;
                    }
                }
                if (direction == 0)
                {
                    z += 1;
                }
                else if (direction == 1)
                {
                    x += 1;
                }
                else if (direction == 2)
                {
                    z -= 1;
                }
                else if (direction == 3)
                {
                    x -= 1;
                }
            }
            turns += 1;
            if (turns % 2 == 0)
            {
                side_length += 1;
                readyVoxels.y = side_length;
            }
            direction = (direction + 1) % 4;
            steps = side_length;
        }
    }

    private IEnumerator DrawTerrain()
    {
        var size = terrainSize;
        int x = (int)readyVoxels.x; int z = (int)readyVoxels.z; // Start at the middle of the 2D plane
        int direction = 0; //0: right, 1: down, 2: left, 3: up
        int steps = 1; int side_length = 1; // Number of steps to take in the current direction and the length of the current side
        int turns = 0; // Number of turns made so far
        yield return new WaitUntil(() => readyVoxels.y > 4);
        while (true)
        {
            for (int i = 0; i < steps; i++)
            {
                if ((x >= 0) && (x < size) && (z >= 0) && (z < size))
                {
                    for (int y = 0; y < size; y++)
                    {
                        // Add the voxel to the mesh
                        var voxel = voxels[x, y, z];
                        if (voxel && !voxelsDrawn[x, y, z] &&
                            !(x > 0 && z > 0 && y > 0 &&
                            x < size - 1 && z < size - 1 && y < size - 1 &&
                            (voxels[x - 1, y, z] && voxels[x, y - 1, z] && voxels[x, y, z - 1]) &&
                            (voxels[x + 1, y, z] && voxels[x, y + 1, z] && voxels[x, y, z + 1])))
                        {
                            voxelsDrawn[x, y, z] = true;
                            TerrainGenerator.queueRender = true;
                            AddVoxelToMesh(x, y, z,
                            new OcclusionFilter()
                            {
                                Back = z > 0 && voxels[x, y, z - 1],
                                Front = z < size - 1 && voxels[x, y, z + 1],
                                Under = y > 0 && voxels[x, y - 1, z],
                                Over = y < size - 1 && voxels[x, y + 1, z],
                                Right = x < size - 1 && voxels[x + 1, y, z],
                                Left = x > 0 && voxels[x - 1, y, z]
                            });
                        }
                    }
                }
                if (direction == 0)
                {
                    z += 1;
                }
                else if (direction == 1)
                {
                    x += 1;
                }
                else if (direction == 2)
                {
                    z -= 1;
                }
                else if (direction == 3)
                {
                    x -= 1;
                }
            }
            turns += 1;
            if (turns % 2 == 0)
            {
                drawnVoxelSize = side_length;
                side_length += 1;
                if (readyVoxels.y <= side_length + 4)
                {
                    yield return new WaitUntil(() => readyVoxels.y > 4);
                    side_length = 1;
                    x = (int)readyVoxels.x;
                    z = (int)readyVoxels.z;
                }
                else
                {
                    yield return null;
                }
            }
            direction = (direction + 1) % 4;
            steps = side_length;
        }
    }

    private IEnumerator RecalculateMesh()
    {
        while (true)
        {
            if (TerrainGenerator.queueRender)
            {
                TerrainGenerator.queueRender = false;
                // Set the mesh data and calculate normals
                mesh.Clear();
                mesh.SetVertices(vertices);
                mesh.SetTriangles(triangles, 0);
                mesh.RecalculateNormals();
                MeshMerger.MergeAdjacentTriangles(mesh);

                meshCollider.sharedMesh = null;
                meshCollider.sharedMesh = mesh;
                yield return new WaitForEndOfFrame();
            }
            yield return new WaitForSeconds(1);
        }
    }

    private void AddVoxelToMesh(int x, int y, int z, OcclusionFilter occlusion)
    {
        // A map of where to find a specific vertex. -2 means it will not be used for this voxel.
        var vertexIndex = new List<int>();

        var vector = new Vector3(x, y, z);
        var index = occlusion.Back && occlusion.Under && occlusion.Left ? -2 : vertexIndexMap.GetValueOrDefault(vector, -1);
        if (index == -1)
        {
            vertices.Add(vector);
            index = vertices.Count - 1;
            vertexIndexMap.Add(vector, index);
        }
        vertexIndex.Add(index);

        vector = new Vector3(x + 1, y, z);
        index = occlusion.Back && occlusion.Under && occlusion.Right ? -2 : vertexIndexMap.GetValueOrDefault(vector, -1);
        if (index == -1)
        {
            vertices.Add(vector);
            index = vertices.Count - 1;
            vertexIndexMap.Add(vector, index);
        }
        vertexIndex.Add(index);

        vector = new Vector3(x + 1, y + 1, z);
        index = occlusion.Back && occlusion.Over && occlusion.Right ? -2 : vertexIndexMap.GetValueOrDefault(vector, -1);
        if (index == -1)
        {
            vertices.Add(vector);
            index = vertices.Count - 1;
            vertexIndexMap.Add(vector, index);
        }
        vertexIndex.Add(index);

        vector = new Vector3(x, y + 1, z);
        index = occlusion.Back && occlusion.Over && occlusion.Left ? -2 : vertexIndexMap.GetValueOrDefault(vector, -1);
        if (index == -1)
        {
            vertices.Add(vector);
            index = vertices.Count - 1;
            vertexIndexMap.Add(vector, index);
        }
        vertexIndex.Add(index);

        vector = new Vector3(x, y, z + 1);
        index = occlusion.Front && occlusion.Under && occlusion.Left ? -2 : vertexIndexMap.GetValueOrDefault(vector, -1);
        if (index == -1)
        {
            vertices.Add(vector);
            index = vertices.Count - 1;
            vertexIndexMap.Add(vector, index);
        }
        vertexIndex.Add(index);

        vector = new Vector3(x + 1, y, z + 1);
        index = occlusion.Front && occlusion.Under && occlusion.Right ? -2 : vertexIndexMap.GetValueOrDefault(vector, -1);
        if (index == -1)
        {
            vertices.Add(vector);
            index = vertices.Count - 1;
            vertexIndexMap.Add(vector, index);
        }
        vertexIndex.Add(index);

        vector = new Vector3(x + 1, y + 1, z + 1);
        index = occlusion.Front && occlusion.Over && occlusion.Right ? -2 : vertexIndexMap.GetValueOrDefault(vector, -1);
        if (index == -1)
        {
            vertices.Add(vector);
            index = vertices.Count - 1;
            vertexIndexMap.Add(vector, index);
        }
        vertexIndex.Add(index);

        vector = new Vector3(x, y + 1, z + 1);
        index = occlusion.Front && occlusion.Over && occlusion.Left ? -2 : vertexIndexMap.GetValueOrDefault(vector, -1);
        if (index == -1)
        {
            vertices.Add(vector);
            index = vertices.Count - 1;
            vertexIndexMap.Add(vector, index);
        }
        vertexIndex.Add(index);


        // Add the triangles that make up the sides of the voxel unless they are occluded
        if (!occlusion.Back)
        {
            triangles.Add(vertexIndex[0]); triangles.Add(vertexIndex[2]); triangles.Add(vertexIndex[1]);
            triangles.Add(vertexIndex[0]); triangles.Add(vertexIndex[3]); triangles.Add(vertexIndex[2]);
        }

        if (!occlusion.Front)
        {
            triangles.Add(vertexIndex[4]); triangles.Add(vertexIndex[5]); triangles.Add(vertexIndex[6]);
            triangles.Add(vertexIndex[4]); triangles.Add(vertexIndex[6]); triangles.Add(vertexIndex[7]);
        }

        if (!occlusion.Under)
        {
            triangles.Add(vertexIndex[0]); triangles.Add(vertexIndex[5]); triangles.Add(vertexIndex[4]);
            triangles.Add(vertexIndex[0]); triangles.Add(vertexIndex[1]); triangles.Add(vertexIndex[5]);
        }

        if (!occlusion.Over)
        {
            triangles.Add(vertexIndex[2]); triangles.Add(vertexIndex[3]); triangles.Add(vertexIndex[7]);
            triangles.Add(vertexIndex[2]); triangles.Add(vertexIndex[7]); triangles.Add(vertexIndex[6]);
        }

        if (!occlusion.Right)
        {
            triangles.Add(vertexIndex[1]); triangles.Add(vertexIndex[2]); triangles.Add(vertexIndex[5]);
            triangles.Add(vertexIndex[5]); triangles.Add(vertexIndex[2]); triangles.Add(vertexIndex[6]);
        }

        if (!occlusion.Left)
        {
            triangles.Add(vertexIndex[3]); triangles.Add(vertexIndex[0]); triangles.Add(vertexIndex[7]);
            triangles.Add(vertexIndex[0]); triangles.Add(vertexIndex[4]); triangles.Add(vertexIndex[7]);
        }

    }

}