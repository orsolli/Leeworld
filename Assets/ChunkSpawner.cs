using System.Collections.Generic;
using System.Text;
using NativeWebSocket;
using UnityEngine;

public class ChunkSpawner : MonoBehaviour
{
    public GameObject target;
    public GameObject chunkPrefab;
    public int drawDistance = 16;
    public int memory = 128;
    public static int worldSize = 255;
    private int chunkSize = 8;
    private Vector3 lastPos;
    WebSocket client;
    public Server server;

    private GameObject[,,] chunks = new GameObject[worldSize, worldSize, worldSize];

    void Start()
    {
        chunkSize = (int)chunkPrefab.transform.localScale.x;
        Connect();
        StartCoroutine(Spawn());
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        client.DispatchMessageQueue();
#endif
    }

    void Receive(byte[] data)
    {
        string res = Encoding.ASCII.GetString(data);
        Debug.Log(res);
        if (res.StartsWith("digg:"))
        {
            var block_id = res.Split(':')[1].Split('_');
            var chunk = chunks[ToIndex(int.Parse(block_id[0])), ToIndex(int.Parse(block_id[1])), ToIndex(int.Parse(block_id[2]))];
            if (chunk == null) return;

            var block = chunk.GetComponent<Block>();
            var ptc = res.Split(':')[2];
            block.progress = int.Parse(ptc) / 100f;
            if (ptc.Equals("100"))
            {
                block.progress = 0;
                block.StopDigg();
            }
        }
        else if (res.StartsWith("block:"))
        {
            var block_id = res.Split(':')[1].Split('_');
            var chunk = chunks[ToIndex(int.Parse(block_id[0])), ToIndex(int.Parse(block_id[1])), ToIndex(int.Parse(block_id[2]))];
            if (chunk == null) return;
            StartCoroutine(chunk.GetComponent<Block>().UpdateMesh(10));
        }
    }

    public void Destroy()
    {
        StopAllCoroutines();
    }

    private void Connect()
    {
        client = new WebSocket($"{server.GetWsScheme()}://{server.GetHost()}/ws/blocks/{server.GetPlayer()}/", server.GetHeaders());

        client.OnMessage += Receive;
        client.OnError += (e) => { Debug.LogError(e); Destroy(); };
        client.OnClose += (e) => { Debug.Log(e); Destroy(); };
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
                var spawnQueue = new LinkedList<SpawnPlan>();
                for (int xo = -mem; xo < mem; xo++)
                {
                    for (int yo = -mem; yo < mem; yo++)
                    {
                        for (int zo = -mem; zo < mem; zo++)
                        {
                            var chunkPos = new Vector3((x + xo) * chunkSize, (y + yo) * chunkSize, (z + zo) * chunkSize);
                            if (chunks[ToIndex(x + xo), ToIndex(y + yo), ToIndex(z + zo)] == null)
                            {
                                var distance = Vector3.Distance(pos, chunkPos);
                                if (distance < memory * chunkSize)
                                {
                                    var node = new SpawnPlan
                                    {
                                        distance = distance,
                                        position = chunkPos,
                                        x = ToIndex(x + xo),
                                        y = ToIndex(y + yo),
                                        z = ToIndex(z + zo),
                                    };
                                    if (spawnQueue.Count == 0 || node.distance < spawnQueue.First.Value.distance)
                                    {
                                        spawnQueue.AddFirst(node);
                                        continue;
                                    }
                                    else if (node.distance > spawnQueue.Last.Value.distance)
                                    {
                                        spawnQueue.AddLast(node);
                                        continue;
                                    }
                                    foreach (var n in spawnQueue)
                                    {
                                        if (node.distance < n.distance)
                                        {
                                            spawnQueue.AddBefore(spawnQueue.Find(n), node);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                foreach (var plan in spawnQueue)
                {
                    var chunk = GameObject.Instantiate(chunkPrefab, plan.position, Quaternion.identity);
                    var block = chunk.GetComponent<Block>();
                    block.client = client;
                    block.server = server;
                    chunks[plan.x, plan.y, plan.z] = chunk;
                    yield return 0;
                }
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
                        var block = chunks[xi, yi, zi];
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

    private int ToIndex(int scalar)
    {
        return (scalar + worldSize) % worldSize;
    }

    class SpawnPlan
    {
        internal int x;
        internal float distance;
        internal Vector3 position;
        internal int y;
        internal int z;
    }
}
