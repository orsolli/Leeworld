using System;
using System.Collections.Generic;
using NativeWebSocket;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class BlueprintTool : MonoBehaviour, ITool
{
    public static string DEFAULT_MESH = @"v 0 0 0
v 0 0 1
v 0 1 0
v 0 1 1
v 1 0 0
v 1 0 1
v 1 1 0
v 1 1 1

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
    public bool isPrinting;
    private Vector3 dragStartPos;
    public Vector3 offset;
    public GameObject previewBlockPrefab;
    private GameObject previewBlock;
    private BlueprintInstance blueprint;
    public LayerMask layerMask;
    private int sign;
    public Vector3 gridSize;
    private Server server;
    public WebSocket client;
    private PauseManager pm;

    void Start()
    {
        pm = FindObjectOfType<PauseManager>(true);
        server = FindObjectOfType<Server>(true);
        var mesh = MeshParser.ParseOBJ(PlayerPrefs.GetString("PLAYER_MESH", DEFAULT_MESH), 1);
        gridSize = mesh.bounds.max;
        previewBlock = Instantiate(previewBlockPrefab);
        previewBlock.transform.localScale = gridSize;
        blueprint = new BlueprintInstance(gameObject, previewBlockPrefab, mesh, Vector3.zero, Vector3.zero);

        sign = PlayerPrefs.GetString("PLAYER_BUILDER").Equals(false.ToString()) ? 1 : -1;
    }

    void OnDestroy()
    {
        Destroy(previewBlock);
        blueprint.OnDestroy();
    }

    void Update()
    {
        if (MovePreviewBlock(previewBlock) && Input.GetMouseButtonDown(0))
        {
            if (!PlayerPrefs.GetString("SESSION_ACTIVE").Equals("true") && false)
            {
                SceneManager.LoadScene("Login", LoadSceneMode.Additive);
                pm.Pause();
                return;
            }
            isPrinting = true;
            dragStartPos = previewBlock.transform.position;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            if (isPrinting)
            {
                isPrinting = false;
                var endPositionNullable = GetBlockHitPosition();
                if (endPositionNullable == null) return;
                Vector3 endPosition = endPositionNullable ?? throw new ArgumentNullException();
                Vector3 pos = dragStartPos;
                var block_pos = pos / 8;
                var block = $"{Mathf.FloorToInt(block_pos.x)}_{Mathf.FloorToInt(block_pos.y)}_{Mathf.FloorToInt(block_pos.z)}";
                var position = $"{Int8.To8Adic((pos.x % 8 + 8) % 8 / 8)}_{Int8.To8Adic((pos.y % 8 + 8) % 8 / 8)}_{Int8.To8Adic((pos.z % 8 + 8) % 8 / 8)}";
                var size_vec = GetSize(endPosition - dragStartPos);
                var size = $"{Mathf.FloorToInt(size_vec.x)}_{Mathf.FloorToInt(size_vec.y)}_{Mathf.FloorToInt(size_vec.z)}";
                var scheme = "https";
                if (PlayerPrefs.GetString("SECURE", "true").Equals(false.ToString()))
                    scheme = "http";
                string host = PlayerPrefs.GetString("SERVER");
                UnityWebRequest request = UnityWebRequest.Post($"{scheme}://{host}/blueprint/print/", new Dictionary<string, string>
                {
                    ["player"] = server.GetPlayer(),
                    ["block"] = block,
                    ["position"] = position,
                    ["size"] = size,
                });
                request.useHttpContinue = false;
                request.redirectLimit = 0;
                request.timeout = 60;
                request.SendWebRequest();
            }
        }
        if (isPrinting)
        {
            var endPositionNullable = GetBlockHitPosition();
            if (endPositionNullable == null) return;
            Vector3 endPosition = endPositionNullable ?? throw new ArgumentNullException();
            var size = GetSize(endPosition - dragStartPos);
            blueprint.UpdateBlueprint(dragStartPos, size);
        }
    }

    private Vector3 GetSize(Vector3 diff)
    {
        var absDiff = new Vector3(Math.Abs(diff.x), Math.Abs(diff.y), Math.Abs(diff.z));
        Vector3 direction = Vector3.zero;
        if (absDiff.y >= Math.Max(absDiff.x, Math.Max(absDiff.y, absDiff.z)))
        {
            direction = Vector3.up * diff.y;
        }
        else if (absDiff.x >= Math.Max(absDiff.x, Math.Max(absDiff.y, absDiff.z)))
        {
            direction = Vector3.right * diff.x;
        }
        else if (absDiff.z >= Math.Max(absDiff.x, Math.Max(absDiff.y, absDiff.z)))
        {
            direction = Vector3.forward * diff.z;
        }
        return direction;
    }

    private bool MovePreviewBlock(GameObject subject)
    {
        Vector3? hitPosition = GetBlockHitPosition();
        if (hitPosition == null)
        {
            subject.SetActive(false);
            return false;
        }

        // Position the previewBlock at the hit point
        subject.transform.position = (Vector3)hitPosition;
        subject.SetActive(true);
        return true;
    }

    private Vector3? GetBlockHitPosition()
    {
        // Cast a ray from the mouse position
        Ray ray = Camera.main.ScreenPointToRay(offset + new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        // Check if the ray hits a collider on the specified layer
        if (!Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask))
        {
            return null;
        }

        return new Vector3(
            Mathf.Round((hit.point.x - (sign * hit.normal.x / 512 + gridSize.x * 0.5f)) / gridSize.x) * gridSize.x,
            Mathf.Round((hit.point.y - (sign * hit.normal.y / 512 + gridSize.y * 0.5f)) / gridSize.y) * gridSize.y,
            Mathf.Round((hit.point.z - (sign * hit.normal.z / 512 + gridSize.z * 0.5f)) / gridSize.z) * gridSize.z);
    }

    public Vector3 GetPosition()
    {
        return previewBlock.transform.position;
    }
}
