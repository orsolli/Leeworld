using System;
using System.Collections;
using System.Collections.Generic;
using Extension;
using UnityEditor;
using UnityEngine;

public class Digg : MonoBehaviour
{
    public GameObject previewBlock;
    public Transform progressBlock;
    private Transform targetTransform;
    public LayerMask layerMask;
    private IEnumerator diggingCoroutine;
    public float progress;
    public int subDivisions = 8;

    void Start()
    {
        //Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible = false;
    }

    void Update()
    {
        // Cast a ray from the mouse position
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (diggingCoroutine == null)
        {
            // Check if the ray hits a collider on the specified layer
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask))
            {
                previewBlock.SetActive(true);
                targetTransform = hit.transform;
                Vector3 gridSize = previewBlock.transform.localScale;
                // Position the previewBlock at the hit point
                previewBlock.transform.position = new Vector3(
                    Mathf.Round((hit.point.x - hit.normal.x / 3 - 0.5f) / gridSize.x) * gridSize.x,
                    Mathf.Round((hit.point.y - hit.normal.y / 3 - 0.5f) / gridSize.y) * gridSize.y,
                    Mathf.Round((hit.point.z - hit.normal.z / 3 - 0.5f) / gridSize.z) * gridSize.z);
                //previewBlock.transform.rotation = Quaternion.LookRotation(hit.normal);
            }
            else
            {
                previewBlock.SetActive(false);
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            // Start the long-running task
            if (diggingCoroutine == null)
            {
                //Mesh m = targetTransform.gameObject.GetComponent<MeshFilter>().mesh;
                //m.SetColors(new List<Color>());
                //m.SetTangents(new List<Vector4>());
                //m.SetNormals(new List<Vector3>());
                //AssetDatabase.CreateAsset(m, "Assets/kube.asset");
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                progress = 0;
                diggingCoroutine = StartDigging();
                StartCoroutine(diggingCoroutine);
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            //Mesh m = targetTransform.gameObject.GetComponent<MeshFilter>().mesh;
            //AssetDatabase.CreateAsset(m, "Assets/kube.asset");
            // Stop the long-running task
            if (diggingCoroutine != null)
            {
                StopCoroutine(diggingCoroutine);
                diggingCoroutine = null;
                progress = 0;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
        progressBlock.SetLocalPositionAndRotation(progressBlock.localPosition, Quaternion.AngleAxis(90 * progress, Vector3.up));
    }

    public Net3dBool.Color3f[] getColorArray(int length, Color c)
    {
        var ar = new Net3dBool.Color3f[length];
        for (var i = 0; i < length; i++)
            ar[i] = new Net3dBool.Color3f(c.r, c.g, c.b);
        return ar;
    }

    Net3dBool.Solid meshToSolid(Mesh mesh, bool UpdateProgress)
    {
        Dictionary<Vector3, int> vertexIndexMap = new Dictionary<Vector3, int>();

        int mlen = mesh.vertices.Length;
        Net3dBool.Point3d[] vertices = new Net3dBool.Point3d[mlen];
        for (int i = 0; i < mlen; i++)
        {
            Vector3 p = mesh.vertices[i];
            p.x = Mathf.Round(p.x * subDivisions) / subDivisions;
            p.y = Mathf.Round(p.y * subDivisions) / subDivisions;
            p.z = Mathf.Round(p.z * subDivisions) / subDivisions;
            var b = new Net3dBool.Point3d((double)p.x, (double)p.y, (double)p.z);
            vertices[i] = b;
            vertexIndexMap.TryAdd(new Vector3((float)b.x, (float)b.y, (float)b.z), i);
            if (UpdateProgress)
                progress = (float)(i) / (mlen * 5);
        }
        int tlen = mesh.triangles.Length;
        HashSet<int[]> hs = new HashSet<int[]>(new IntArrayEqualityComparer());
        for (int i = 0; i < tlen; i += 3)
        {
            var b = vertices[mesh.triangles[i]];
            var newOne = vertexIndexMap.GetValueOrDefault(new Vector3((float)b.x, (float)b.y, (float)b.z));
            b = vertices[mesh.triangles[i + 1]];
            var newTwo = vertexIndexMap.GetValueOrDefault(new Vector3((float)b.x, (float)b.y, (float)b.z));
            b = vertices[mesh.triangles[i + 2]];
            var newThree = vertexIndexMap.GetValueOrDefault(new Vector3((float)b.x, (float)b.y, (float)b.z));
            int[] arr = { newOne, newTwo, newThree };
            hs.Add(arr);
            if (UpdateProgress)
                progress = (float)(mlen + i) / (mlen * 5);
        }

        int hlen = hs.Count;
        int[] triangles = new int[hlen * 3];
        int index = 0;
        foreach (int[] triangle in hs)
        {
            triangles[index++] = triangle[0];
            triangles[index++] = triangle[1];
            triangles[index++] = triangle[2];
        }
        return new Net3dBool.Solid(vertices, triangles, getColorArray(mlen, Color.red));
    }
    IEnumerator StartDigging()
    {
        Vector3 targetPosition = targetTransform.position;
        var targetSolid = meshToSolid(targetTransform.gameObject.GetComponent<MeshFilter>().mesh, true);
        yield return null;
        targetSolid.scale(targetTransform.localScale.x, targetTransform.localScale.y, targetTransform.localScale.z);
        targetSolid.translate(targetPosition.x, targetPosition.y);
        targetSolid.zoom(targetPosition.z);

        Vector3 position = previewBlock.transform.position;
        var previewSolid = meshToSolid(previewBlock.GetComponent<MeshFilter>().mesh, false);
        yield return null;
        float epsilon = 0.0001f;
        previewSolid.scale(previewBlock.transform.localScale.x + epsilon, previewBlock.transform.localScale.y + epsilon, previewBlock.transform.localScale.z + epsilon);
        previewSolid.translate(position.x, position.y);
        previewSolid.zoom(position.z);


        var modeller = new Net3dBool.BooleanModeller(targetSolid, previewSolid);
        yield return null;
        var diff = modeller.getDifference();
        diff.zoom(-targetPosition.z);
        diff.translate(-targetPosition.x, -targetPosition.y);
        diff.scale(1 / targetTransform.localScale.x, 1 / targetTransform.localScale.y, 1 / targetTransform.localScale.z);

        var boolOp = CSGGameObject.GenerateMesh(targetTransform.gameObject, targetTransform.gameObject.GetComponent<MeshRenderer>().materials[0], diff);
        while (boolOp.MoveNext())
        {
            progress = (float)boolOp.Current;
            yield return null;
        }

        var meshCollider = targetTransform.gameObject.GetComponent<MeshCollider>();
        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = targetTransform.gameObject.GetComponent<MeshFilter>().mesh;

    }

}
