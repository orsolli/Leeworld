using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Block : MonoBehaviour
{
    private MeshCollider meshCollider;
    private MeshFilter meshFilter;
    private Net3dBool.Solid baseBlock;
    public Net3dBool.Solid diggBlock;
    public List<Transform> queue = new List<Transform>();
    public List<Transform> workers = new List<Transform>();
    public IEnumerator<float> diggingCoroutine;

    void Start()
    {
        meshCollider = GetComponent<MeshCollider>();
        meshFilter = GetComponent<MeshFilter>();
        var mesh = meshFilter.mesh;
        baseBlock = new Net3dBool.Solid(mesh.vertices, mesh.triangles, new Color[mesh.vertices.Length]);
        float epsilon = 0.00012207f;
        baseBlock.scale(transform.localScale.x - epsilon, transform.localScale.y - epsilon, transform.localScale.z - epsilon);
        baseBlock.translate(transform.position.x + epsilon / 2, transform.position.y + epsilon / 2);
        baseBlock.zoom(transform.position.z + epsilon / 2);
    }

    public IEnumerator<float> Digg(Transform previewBlock)
    {
        if (diggingCoroutine == null)
        {
            workers = new List<Transform> { previewBlock };
            diggingCoroutine = StartDigging(workers);
        }
        else if (!queue.Contains(previewBlock) && !workers.Contains(previewBlock))
        {
            queue.Add(previewBlock);
            // Finish the ongoing digg
            while (diggingCoroutine.MoveNext())
            {
                yield return 0.1f * diggingCoroutine.Current;
            }
            if (queue[0] == previewBlock)
            {
                // Digg the whole queue all at once
                workers = queue;
                queue = new List<Transform>();
                diggingCoroutine = StartDigging(workers);
            }
            yield return 0.5f;
        }
        while (diggingCoroutine.MoveNext())
        {
            yield return diggingCoroutine.Current;
        }
        workers = new List<Transform>();
    }

    public void StopDigg(Transform previewBlock)
    {
        queue.Remove(previewBlock);
        if (diggingCoroutine != null && workers.Remove(previewBlock))
        {
            workers.AddRange(queue);
            queue = new List<Transform>();
            diggingCoroutine = StartDigging(workers);
        }
    }

    private IEnumerator<float> StartDigging(List<Transform> previewBlocks)
    {
        var digger = diggBlock;
        int progress = 0;
        foreach (Transform previewBlock in previewBlocks)
        {
            Vector3 position = previewBlock.position;
            Mesh mesh = previewBlock.GetComponent<MeshFilter>().mesh;
            var previewSolid = new Net3dBool.Solid(mesh.vertices, mesh.triangles, new Color[mesh.vertices.Length]);
            previewSolid.scale(previewBlock.localScale.x, previewBlock.localScale.y, previewBlock.localScale.z);
            previewSolid.translate(position.x, position.y);
            previewSolid.zoom(position.z);
            yield return progress++ / (previewBlocks.Count * 2);

            if (digger == null)
            {
                digger = previewSolid;
                continue;
            }
            var diggerModeller = new Net3dBool.BooleanModeller(digger, previewSolid);
            digger = diggerModeller.getUnion();
            yield return progress++ / (previewBlocks.Count * 2);
        }
        var diggSiteModeller = new Net3dBool.BooleanModeller(baseBlock, digger);
        var diggSite = diggSiteModeller.getDifference();
        diggSite.zoom(-transform.position.z);
        diggSite.translate(-transform.position.x, -transform.position.y);
        diggSite.scale(1 / transform.localScale.x, 1 / transform.localScale.y, 1 / transform.localScale.z);
        yield return 0.3f;

        var boolOp = CSGGameObject.GenerateMesh(gameObject, GetComponent<MeshRenderer>().materials[0], diggSite);
        while (boolOp.MoveNext())
        {
            if (boolOp.GetType() == 0f.GetType())
                yield return boolOp.Current;
        }
        diggBlock = digger;

        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = meshFilter.mesh;
    }

}

