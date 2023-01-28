using System;
using System.Collections.Generic;
using System.Text;
using Mirror.SimpleWeb;
using UnityEngine;
using UnityEngine.Networking;

public class Block : MonoBehaviour
{
    private MeshCollider meshCollider;
    private MeshFilter meshFilter;
    private DateTime? startTime;
    private string block_id;
    private bool dirty = true;
    private bool stop = false;
    private bool destroy = false;
    private bool bussy = false;
    private float progress;
    private Server server;
    UnityWebRequest req;
    SimpleWebClient client;

    void Start()
    {
        block_id = $"{transform.position.x / 8}_{transform.position.y / 8}_{transform.position.z / 8}";
        server = FindObjectOfType<Server>(true);
        meshCollider = GetComponent<MeshCollider>();
        meshFilter = GetComponent<MeshFilter>();
        Connect();
        StartCoroutine(ProcessMessages());
    }

    IEnumerator<int> ProcessMessages()
    {
        while (this && client != null)
        {
            client.ProcessMessageQueue(this);
            if (dirty && !bussy)
            {
                var c = UpdateMesh();
                while (c.MoveNext())
                {
                    yield return 0;
                };
            }
            yield return 0;
        }
    }

    public void Destroy()
    {
        destroy = true;
        client.Disconnect();
    }

    private void Connect()
    {
        Debug.Log("Connecting");
        if (!destroy)
        {
            client = SimpleWebClient.Create(2048, 64, new TcpConfig(true, 30000, 30000));
            client.Connect(new Uri($"ws://{server.host}/ws/block/{block_id}/"));
            client.onData += Receive;
            client.onDisconnect += Connect;
        }
    }

    public IEnumerator<float> Digg(Transform previewBlock)
    {
        stop = false;
        startTime = DateTime.UtcNow;
        Vector3 previewPos = previewBlock.position - transform.position;
        string position = $"{ToUInt8(previewPos.x)}_{ToUInt8(previewPos.y)}_{ToUInt8(previewPos.z)}";
        while (!stop)
        {
            client.Send(new ArraySegment<byte>(Encoding.ASCII.GetBytes($"{{\"action\":\"digg\",\"player\":\"{server.player}\",\"block\":\"{block_id}\",\"position\":\"{position}\"}}")));
            client.ProcessMessageQueue();
            yield return progress;
        }

        yield break;
    }

    public void StopDigg()
    {
        stop = true;
        client.Send(new ArraySegment<byte>(Encoding.ASCII.GetBytes($"{{\"action\":\"stop\",\"player\":\"{server.player}\"}}")));
    }

    void Receive(ArraySegment<byte> data)
    {
        string res = Encoding.ASCII.GetString(data);
        Debug.Log(res);
        if (res.Equals("Waiting"))
            progress = Progress(1);
        if (res.Equals("Done"))
        {
            stop = true;
            dirty = true;
            progress = 1;
        }
    }

    IEnumerator<int> UpdateMesh()
    {
        bussy = true;
        UnityWebRequest meshRequest = UnityWebRequest.Get($"http://{server.host}/digg/block/?player={server.player}&block={block_id}");
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
            dirty = false;
        }
        else
        {
            dirty = true;
        }
        bussy = false;
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
}
