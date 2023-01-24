using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkSpawner : MonoBehaviour
{
    public GameObject target;
    public GameObject chunkPrefab;
    public int memory = 32;
    public int worldSize = 255;
    private int chunkSize;

    private GameObject[,] chunks;

    // Start is called before the first frame update
    void Start()
    {
        chunks = new GameObject[worldSize, worldSize];
        chunkSize = (int)chunkPrefab.transform.localScale.x;
    }

    void Update()
    {
        chunkSize = (int)chunkPrefab.transform.localScale.x;
        var pos = target.transform.position;
        {
            var mem = (int)Mathf.Sqrt(memory);
            var x = (int)(pos.x / chunkSize);
            var z = (int)(pos.z / chunkSize);
            for (int xo = -mem; xo < mem; xo++)
            {
                for (int zo = -mem; zo < mem; zo++)
                {
                    if (chunks[(x + xo + worldSize) % worldSize, (z + zo + worldSize) % worldSize] == null)
                    {
                        chunks[(x + xo + worldSize) % worldSize, (z + zo + worldSize) % worldSize] = GameObject.Instantiate(chunkPrefab, new Vector3((x + xo) * chunkSize, 0, (z + zo) * chunkSize), Quaternion.identity);
                    }
                }
            }
        }
        {
            for (int x = 0; x < worldSize; x++)
            {
                for (int z = 0; z < worldSize; z++)
                {
                    if (chunks[x % worldSize, z % worldSize] != null && Vector2.Distance(new Vector2(pos.x, pos.z), new Vector2(x * chunkSize, z * chunkSize)) > memory * chunkSize)
                    {
                        GameObject.Destroy(chunks[x % worldSize, z % worldSize]);
                    }
                }
            }
        }
    }
}
