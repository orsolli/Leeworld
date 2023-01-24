using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class Block : MonoBehaviour
{
    private MeshCollider meshCollider;
    private MeshFilter meshFilter;
    private DateTime? startTime;
    private string block_id;
    private bool stop;

    async void Start()
    {
        meshCollider = GetComponent<MeshCollider>();
        meshFilter = GetComponent<MeshFilter>();
        block_id = $"{transform.position.x / 8},{transform.position.y / 8},{transform.position.z / 8}";
        var req = new UnityWebRequest($"http://127.0.0.1:8000/digg/block/?player=1&block={block_id}");
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SendWebRequest();
        while (!req.isDone) await Task.Delay(10);
        if ((int)(req.responseCode / 100) == 2)
        {
            while (!req.downloadHandler.isDone) await Task.Delay(10);
            meshFilter.mesh = MeshParser.ParseOBJ(req.downloadHandler.text);
            meshCollider.sharedMesh = meshFilter.mesh;
        }
        else
        {
            Debug.LogError("Could not load block. Trying again in one second");
            await Task.Delay(1000);
            if (this != null)
                Start();
        }
    }

    private string ToUInt8(float f)
    {
        string res = "";
        int i = (int)f;
        while (f > (int)f && res.Length < 3)
            i = (int)f;
        res = $"{res}{i}";
        f -= i;
        f *= 8;
        return res;
    }

    private float Progress(float duration)
    {
        return (float)(DateTime.UtcNow - startTime)?.TotalSeconds / 10 % duration;
    }

    public IEnumerator<float> Digg(Transform previewBlock, int player)
    {
        startTime = DateTime.UtcNow;
        Vector3 previewPos = previewBlock.position - transform.position;
        string position = $"{ToUInt8(previewPos.x)},{ToUInt8(previewPos.y)},{ToUInt8(previewPos.z)}";
        UnityWebRequest req;
        string text = "Waiting";
        while (!text.Equals("Done"))
        {
            req = new UnityWebRequest($"http://127.0.0.1:8000/digg/request/?player={player}&block={block_id}&position={position}", "PUT");
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SendWebRequest();
            while (!req.isDone)
                yield return Progress(0.5f);

            text = req.downloadHandler.text;

            if (stop)
            {
                new UnityWebRequest($"http://127.0.0.1:8000/digg/request/?player={player}", "DELETE").SendWebRequest();
                stop = false;
                yield break;
            }
        }

        req = new UnityWebRequest($"http://127.0.0.1:8000/digg/block/?player={player}&block={block_id}");
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SendWebRequest();

        while (!req.isDone)
            yield return Progress(0.9f);

        if (req.responseCode.Equals(200))
        {
            meshFilter.mesh = MeshParser.ParseOBJ(req.downloadHandler.text);
            meshCollider.sharedMesh = meshFilter.mesh;
        }
        yield return 1;
    }

    public void StopDigg()
    {
        stop = true;
    }

}

