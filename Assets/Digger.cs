using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading;
using NativeWebSocket;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Digger : MonoBehaviour
{
    public static string DEFAULT_MESH = @"v 0 0 0
v 0 0 0.001
v 0 0.001 0
v 0 0.001 0.001
v 0.001 0 0
v 0.001 0 0.001
v 0.001 0.001 0
v 0.001 0.001 0.001

vn 1 0 0
vn -1 0 0
vn 0 1 0
vn 0 -1 0
vn 0 0 1
vn 0 0 -1

f 1//2 2 3//2
f 2 4//2 3//2
f 2//5 6//5 4//5
f 4//5 6//5 8//5
f 1//4 5//4 2//4
f 2//4 5//4 6//4
f 3 8//3 7//3
f 3 4//3 8//3
f 1//6 3//6 7//6
f 1//6 7//6 5//6
f 5//1 8//1 6//1
f 5//1 7//1 8//1
";
    public Vector3 offset;
    public GameObject previewBlockPrefab;
    private GameObject previewBlock;
    private Transform progressBlock;
    private Transform targetTransform;
    public LayerMask layerMask;
    private IAsyncEnumerator<float> diggingCoroutine;
    [ReadOnly(true)] public float progress;
    private bool stop = false;
    private int sign;
    public Vector3 gridSize;
    private Server server;
    public WebSocket client;
    private PauseManager pm;

    void Start()
    {
        pm = FindObjectOfType<PauseManager>(true);
        server = FindObjectOfType<Server>(true);
        client = FindObjectOfType<BlockClient>(true).client;
        client.OnMessage += OnMessage;

        var mesh = MeshParser.ParseOBJ(PlayerPrefs.GetString("PLAYER_MESH", DEFAULT_MESH), 1);
        gridSize = mesh.bounds.max;
        previewBlock = Instantiate(previewBlockPrefab);
        previewBlock.transform.localScale = gridSize;
        progressBlock = previewBlock.transform.GetChild(0);

        sign = PlayerPrefs.GetString("PLAYER_BUILDER").Equals(false.ToString()) ? 1 : -1;
    }

    void OnDestroy()
    {
        Destroy(previewBlock);
        client.OnMessage -= OnMessage;
    }

    async void Update()
    {
        if (diggingCoroutine == null && MovePreviewBlock() && Input.GetMouseButtonDown(0))
        {
            progress = 0;
            if (!PlayerPrefs.GetString("SESSION_ACTIVE").Equals("true"))
            {
                SceneManager.LoadScene("Login", LoadSceneMode.Additive);
                pm.Pause();
                return;
            }
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
        Vector3 pos = previewBlock.position;
        var block_pos = pos / 8;
        var block = $"{Mathf.FloorToInt(block_pos.x)}_{Mathf.FloorToInt(block_pos.y)}_{Mathf.FloorToInt(block_pos.z)}";
        var position = $"{Int8.ToUInt8((pos.x % 8 + 8) % 8)}_{Int8.ToUInt8((pos.y % 8 + 8) % 8)}_{Int8.ToUInt8((pos.z % 8 + 8) % 8)}";
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
