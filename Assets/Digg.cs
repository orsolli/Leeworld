using System;
using System.Collections;
using System.Collections.Generic;
using Extension;
using UnityEngine;

public class Digg : MonoBehaviour
{
    public GameObject previewBlock;
    public Transform progressBlock;
    private Transform targetTransform;
    public LayerMask layerMask;
    private IEnumerator diggingCoroutine;
    public float progress;

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
                //previewBlock.enabled = true;
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
                //previewBlock.enabled = false;
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            // Start the long-running task
            if (diggingCoroutine == null)
            {
                progress = 0;
                diggingCoroutine = StartDigging();
                StartCoroutine(diggingCoroutine);
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            // Stop the long-running task
            if (diggingCoroutine != null)
            {
                StopCoroutine(diggingCoroutine);
                diggingCoroutine = null;
                progress = 0;
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
            p.x = Mathf.Round(p.x * 8) / 8;
            p.y = Mathf.Round(p.y * 8) / 8;
            p.z = Mathf.Round(p.z * 8) / 8;
            var b = new Net3dBool.Point3d((double)p.x, (double)p.y, (double)p.z);
            vertices[i] = b;
            vertexIndexMap.TryAdd(new Vector3((float)b.x, (float)b.y, (float)b.z), i);
            if (UpdateProgress)
                progress = (float)(i) / (mlen * 4);
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
                progress = (float)(mlen + i) / (mlen * 4);
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
        previewSolid.scale(previewBlock.transform.localScale.x, previewBlock.transform.localScale.y, previewBlock.transform.localScale.z);
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
