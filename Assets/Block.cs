using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
using WebSocketSharp;

public class Block : MonoBehaviour
{
    private MeshCollider meshCollider;
    private MeshFilter meshFilter;
    private DateTime? startTime;
    private string block_id;
    public bool dirty = true;
    private bool stop = false;
    private bool bussy = false;
    public float progress;
    public Server server;
    UnityWebRequest req;
    public WebSocket client;

    void Start()
    {
        block_id = $"{transform.position.x / 8}_{transform.position.y / 8}_{transform.position.z / 8}";
        server = FindObjectOfType<Server>(true);
        meshCollider = GetComponent<MeshCollider>();
        meshFilter = GetComponent<MeshFilter>();
        if (!block_id.Contains('-'))
        {
            meshCollider.sharedMesh = null;
            meshFilter.mesh = null;
        }
        StartCoroutine(UpdateMesh());
    }

    public IEnumerator<float> Digg(Transform previewBlock)
    {
        while (client == null || !client.IsAlive) yield return 0;
        stop = false;
        startTime = DateTime.UtcNow;
        Vector3 previewPos = previewBlock.position - transform.position;
        string position = $"{Int8.ToUInt8(previewPos.x)}_{Int8.ToUInt8(previewPos.y)}_{Int8.ToUInt8(previewPos.z)}";
        client.Send(Encoding.ASCII.GetBytes($"{{\"action\":\"digg\",\"player\":\"{server.GetPlayer()}\",\"block\":\"{block_id}\",\"position\":\"{position}\"}}"));
        while (!stop && progress < 1)
        {
            yield return progress;
        }
        yield return 1;
    }

    public void StopDigg()
    {
        stop = true;
        client.Send(Encoding.ASCII.GetBytes($"{{\"action\":\"stop\",\"player\":\"{server.GetPlayer()}\"}}"));
        Thread.Sleep(1);
    }

    public IEnumerator<int> UpdateMesh()
    {
        if (!bussy)
        {
            bussy = true;
            UnityWebRequest meshRequest = UnityWebRequest.Get($"http://{server.GetHost()}/digg/block/?player={server.GetPlayer()}&block={block_id}");
            meshRequest.downloadHandler = new DownloadHandlerBuffer();
            meshRequest.useHttpContinue = false;
            meshRequest.redirectLimit = 0;
            meshRequest.timeout = 5;
            meshRequest.SendWebRequest();
            while (!meshRequest.isDone) yield return 0;
            if ((int)(meshRequest.responseCode / 100) == 2)
            {
                while (!meshRequest.downloadHandler.isDone) yield return 0;
                meshFilter.mesh = MeshParser.ParseOBJ(meshRequest.downloadHandler.text);
                meshCollider.sharedMesh = meshFilter.mesh;
            }
            else
            {
                Debug.LogError($"Failed to fetch mesh for {block_id}");
            }
            bussy = false;
        }
    }
}
