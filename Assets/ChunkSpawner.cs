using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using WebSocketSharp;

public class ChunkSpawner : MonoBehaviour
{
    public GameObject target;
    public GameObject chunkPrefab;
    public int memory = 32;
    public static int worldSize = 255;
    private int chunkSize = 8;
    private Vector3 lastPos;
    WebSocket client;
    public Server server;

    private GameObject[,,] chunks = new GameObject[worldSize, worldSize, worldSize];

    void Start()
    {
        chunkSize = (int)chunkPrefab.transform.localScale.x;
        Connect(null, null);
        StartCoroutine(Spawn());
    }

    void Receive(object sender, MessageEventArgs data)
    {
        string res = data.Data;
        Debug.Log(res);
        if (res.StartsWith("digg:"))
        {
            var block_id = res.Split(':')[1].Split('_');
            var chunk = chunks[int.Parse(block_id[0]), int.Parse(block_id[1]), int.Parse(block_id[2])];
            if (chunk == null) return;

            var block = chunk.GetComponent<Block>();
            var ptc = res.Split(':')[2];
            block.progress = int.Parse(ptc) / 100;
            if (ptc.Equals("100"))
            {
                block.progress = 0;
                block.StopDigg();
            }
        }
        else if (res.StartsWith("block:"))
        {
            var block_id = res.Split(':')[1].Split('_');
            var chunk = chunks[int.Parse(block_id[0]), int.Parse(block_id[1]), int.Parse(block_id[2])];
            if (chunk == null) return;
            StartCoroutine(chunk.GetComponent<Block>().UpdateMesh());
        }
    }

    public void Destroy()
    {
        client.OnClose -= Connect;
        client.Close();
        StopAllCoroutines();
    }

    private void Connect(object sender, CloseEventArgs e)
    {
        if (e != null) Thread.Sleep(1000);
        client = new WebSocket($"ws://{server.GetHost()}/ws/blocks/{server.GetPlayer()}/");
        client.OnMessage += Receive;
        client.OnClose += Connect;
        client.Connect();
    }

    private IEnumerator<int> Spawn()
    {
        var garbageCollector = GarbageCollect();
        while (true)
        {
            var pos = target.transform.position;
            if (lastPos == null || Vector3.Distance(lastPos, pos) > chunkSize)
            {
                lastPos = pos;
                //Debug.Log($"ChunkSpawner {pos}");
                var x = (int)(pos.x / chunkSize);
                var y = (int)(pos.y / chunkSize);
                var z = (int)(pos.z / chunkSize);
                var mem = (int)Mathf.Sqrt(memory);
                for (int xo = -mem; xo < mem; xo++)
                {
                    for (int yo = -mem; yo < mem; yo++)
                    {
                        for (int zo = -mem; zo < mem; zo++)
                        {
                            var chunkPos = new Vector3((x + xo) * chunkSize, (y + yo) * chunkSize, (z + zo) * chunkSize);
                            if (chunks[(x + xo + worldSize) % worldSize, (y + yo + worldSize) % worldSize, (z + zo + worldSize) % worldSize] == null && Vector3.Distance(pos, chunkPos) < memory * chunkSize)
                            {
                                var chunk = GameObject.Instantiate(chunkPrefab, chunkPos, Quaternion.identity);
                                var block = chunk.GetComponent<Block>();
                                block.client = client;
                                block.server = server;
                                chunks[(x + xo + worldSize) % worldSize, (y + yo + worldSize) % worldSize, (z + zo + worldSize) % worldSize] = chunk;
                                //Debug.Log($"Spawned chunk {chunkPos}");
                            }
                            yield return 0;
                        }
                    }
                }
                yield return 0;
            }
            yield return 0;
            garbageCollector.MoveNext();
        }
    }
    private IEnumerator<int> GarbageCollect()
    {
        while (true)
        {
            for (int xi = 0; xi < worldSize; xi++)
            {
                for (int yi = 0; yi < worldSize; yi++)
                {
                    for (int zi = 0; zi < worldSize; zi++)
                    {
                        var block = chunks[xi % worldSize, yi % worldSize, zi % worldSize];
                        if (block != null && Vector3.Distance(lastPos, block.transform.position) > (memory + 1) * chunkSize)
                        {
                            //Debug.Log($"Destroying chunk {block.transform.position}");
                            GameObject.Destroy(block);
                        }
                    }
                }
                yield return 0;
            }
        }
    }
}
