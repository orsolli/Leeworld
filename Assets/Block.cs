using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Block : MonoBehaviour
{
    private MeshCollider meshCollider;
    private MeshFilter meshFilter;
    private string block_id;
    private bool bussy = false;
    private Server server;
    private BlockClient client;

    void Start()
    {
        block_id = $"{transform.position.x / 8}_{transform.position.y / 8}_{transform.position.z / 8}";
        server = FindObjectOfType<Server>(true);
        client = FindObjectOfType<BlockClient>(true);
        client.client.OnMessage += OnMessage;
        meshCollider = GetComponent<MeshCollider>();
        meshFilter = GetComponent<MeshFilter>();
        if (!block_id.Contains('-'))
        {
            meshFilter.mesh = null;
        }
        StartCoroutine(UpdateMesh(2));
    }

    private void OnDestroy()
    {
        client.client.OnMessage -= OnMessage;
    }

    void OnMessage(byte[] data)
    {
        string res = System.Text.Encoding.ASCII.GetString(data);
        if (res.StartsWith("block:") && block_id.Equals(res.Split(':')[1]))
        {
            StartCoroutine(UpdateMesh(10));
        }
    }

    public IEnumerator<int> UpdateMesh(int recursionLimit)
    {
        if (!bussy)
        {
            bussy = true;
            UnityWebRequest meshRequest = UnityWebRequest.Get($"{server.GetHttpScheme()}://{server.GetHost()}/digg/block/?player={server.GetPlayer()}&block={block_id}");
            meshRequest.downloadHandler = new DownloadHandlerBuffer();
            meshRequest.useHttpContinue = false;
            meshRequest.redirectLimit = 0;
            meshRequest.timeout = 60;
            foreach (var header in server.GetHeaders())
            {
                meshRequest.SetRequestHeader(header.Key, header.Value);
            }
            meshRequest.SendWebRequest();
            while (!meshRequest.isDone) yield return 0;
            if ((int)(meshRequest.responseCode / 100) == 2)
            {
                while (!meshRequest.downloadHandler.isDone) yield return 0;
                meshFilter.mesh = MeshParser.ParseOBJ(meshRequest.downloadHandler.text, 1f / 8);
                meshCollider.sharedMesh = meshFilter.mesh;
            }
            else
            {
                if (recursionLimit > 0)
                {
                    yield return 0;
                    StartCoroutine(UpdateMesh(recursionLimit - 1));
                }
                else
                {
                    Debug.LogError($"Failed to fetch mesh for {block_id}");
                }
            }
            bussy = false;
        }
    }
}
