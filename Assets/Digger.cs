using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading;
using NativeWebSocket;
using UnityEngine;

public class Digger : MonoBehaviour
{
    public Vector3 offset;
    public GameObject previewBlock;
    public Transform progressBlock;
    private Transform targetTransform;
    public LayerMask layerMask;
    private IAsyncEnumerator<float> diggingCoroutine;
    [ReadOnly(true)] public float progress;
    private bool stop = false;
    private int sign;
    public Vector3 gridSize;
    public Server server;
    public WebSocket client;

    void Start()
    {
        server = FindObjectOfType<Server>(true);
        client = FindObjectOfType<BlockClient>(true).client;
        client.OnMessage += OnMessage;

        var mesh = MeshParser.ParseOBJ(PlayerPrefs.GetString("PLAYER_MESH"), 1);

        sign = PlayerPrefs.GetString("PLAYER_BUILDER").Equals(true.ToString()) ? -1 : 1;
        gridSize = mesh.bounds.max;
        previewBlock.transform.localScale = gridSize;
    }

    async void Update()
    {
        if (diggingCoroutine == null && MovePreviewBlock() && Input.GetMouseButtonDown(0))
        {
            progress = 0;
            diggingCoroutine = Digg(previewBlock.transform);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            if (diggingCoroutine != null)
            {
                StopDigg();
                diggingCoroutine = null;
                progress = 0;
            }
        }

        if (diggingCoroutine != null)
        {
            if (await diggingCoroutine.MoveNextAsync())
            {
                progress = diggingCoroutine.Current;
            }
            else
            {
                diggingCoroutine = null;
                progress = 1;
            }
        }
        progressBlock.SetLocalPositionAndRotation(progressBlock.localPosition, Quaternion.AngleAxis(90 * progress, Vector3.up));
    }

    void Destroy()
    {
        client.OnMessage -= OnMessage;
    }

    void OnMessage(byte[] data)
    {
        string res = Encoding.ASCII.GetString(data);
        if (res.StartsWith("digg:"))
        {
            var ptc = res.Split(':')[2];
            progress = int.Parse(ptc) / 100f;
            if (ptc.Equals("100"))
            {
                progress = 0;
                StopDigg();
            }
        }
    }

    private bool MovePreviewBlock()
    {
        // Cast a ray from the mouse position
        Ray ray = Camera.main.ScreenPointToRay(offset + new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        // Check if the ray hits a collider on the specified layer
        if (!Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask))
        {
            previewBlock.SetActive(false);
            return false;
        }

        targetTransform = hit.transform;
        // Position the previewBlock at the hit point
        previewBlock.transform.position = new Vector3(
            Mathf.Round((hit.point.x - (sign * hit.normal.x / 512 + gridSize.x * 0.5f)) / gridSize.x) * gridSize.x,
            Mathf.Round((hit.point.y - (sign * hit.normal.y / 512 + gridSize.y * 0.5f)) / gridSize.y) * gridSize.y,
            Mathf.Round((hit.point.z - (sign * hit.normal.z / 512 + gridSize.z * 0.5f)) / gridSize.z) * gridSize.z);
        previewBlock.SetActive(true);
        return true;
    }

    async private IAsyncEnumerator<float> Digg(Transform previewBlock)
    {
        while (client == null || client.State != WebSocketState.Open) yield return 0;
        stop = false;
        Vector3 blockPos = new Vector3((int)(previewBlock.position.x / 8), (int)(previewBlock.position.y / 8), (int)(previewBlock.position.z / 8));
        Vector3 previewPos = previewBlock.position - blockPos * 8;
        string position = $"{Int8.ToUInt8(previewPos.x)}_{Int8.ToUInt8(previewPos.y)}_{Int8.ToUInt8(previewPos.z)}";
        string block = $"{blockPos.x}_{blockPos.y}_{blockPos.z}";
        while (!stop && progress < 1)
        {
            DateTime nextPoll = DateTime.UtcNow.AddMilliseconds(100);
            await client.Send(Encoding.ASCII.GetBytes($"{{\"action\":\"digg\",\"player\":\"{server.GetPlayer()}\",\"block\":\"{block}\",\"position\":\"{position}\"}}"));
            while ((nextPoll - DateTime.UtcNow).TotalMilliseconds > 0 && !stop && progress < 1)
            {
                yield return progress;
            }
        }
        yield return 1;
    }

    private async void StopDigg()
    {
        stop = true;
        await client.Send(Encoding.ASCII.GetBytes($"{{\"action\":\"stop\",\"player\":\"{server.GetPlayer()}\"}}"));
        Thread.Sleep(1);
    }
}
