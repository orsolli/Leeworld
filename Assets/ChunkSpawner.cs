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
    public Vector3 lastPos;
    public Server server;

    private GameObject[,,] chunks = new GameObject[worldSize, worldSize, worldSize];

    void OnEnable()
    {
        chunkSize = (int)chunkPrefab.transform.localScale.x;
        StartCoroutine(Spawn());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private IEnumerator<int> Spawn()
    {
        var garbageCollector = GarbageCollect();
        while (true)
        {
            var pos = target.transform.position;
            if (Vector3.Distance(lastPos, pos) > chunkSize)
            {
                var dir = pos - lastPos;
                lastPos = pos;
                pos += dir*2;
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
                    chunks[plan.x, plan.y, plan.z] = chunk;
                    yield return 0;
                    if (Vector3.Distance(lastPos, target.transform.position) > chunkSize) break;
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
                        if (block != null && lastPos != null && Vector3.Distance(lastPos, block.transform.position) > (memory + 1) * chunkSize)
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
