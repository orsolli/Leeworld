using System.Collections;
using System.Collections.Generic;
using System.Text;
using NativeWebSocket;
using UnityEngine;
using UnityEngine.Networking;

public class NetworkManager : MonoBehaviour
{
    public OtherPlayer playerPrefab;
    public GameObject cursorTransform;
    private Server server;
    private WebSocket client;
    private Dictionary<string, OtherPlayer> otherPlayers = new Dictionary<string, OtherPlayer>();
    private string lastPositionReport = "";
    private string lastCursorReport = "";

    void OnEnable()
    {
        server = FindObjectOfType<Server>(true);
        Connect();
    }

    async void Update()
    {
        if (client == null || client.State != WebSocketState.Open) return;
        var position = GetUpdate(transform.position);
        if (!lastPositionReport.Equals(position))
        {
            await client.SendText(
                $"{{\"part\":\"player\",\"block\":\"{position.Split('|')[0]}\",\"position\":\"{position.Split('|')[1]}\"}}");
            lastPositionReport = position;
        }

        var cursor = GetUpdate(cursorTransform.transform.position);
        if (!lastCursorReport.Equals(cursor) && PlayerPrefs.GetString("SESSION_ACTIVE").Equals("true"))
        {
            await client.SendText(
                $"{{\"part\":\"cursor\",\"block\":\"{cursor.Split('|')[0]}\",\"position\":\"{cursor.Split('|')[1]}\"}}"
            );
            lastCursorReport = cursor;
        }
#if !UNITY_WEBGL || UNITY_EDITOR
        client.DispatchMessageQueue();
#endif
    }
    private string GetUpdate(Vector3 pos)
    {
        var block_pos = pos / 8;
        var block = $"{Mathf.FloorToInt(block_pos.x)}_{Mathf.FloorToInt(block_pos.y)}_{Mathf.FloorToInt(block_pos.z)}";
        var position = $"{Int8.To8Adic((pos.x % 8 + 8) % 8 / 8)}_{Int8.To8Adic((pos.y % 8 + 8) % 8 / 8)}_{Int8.To8Adic((pos.z % 8 + 8) % 8 / 8)}";

        if (!PlayerPrefs.GetString("SESSION_ACTIVE").Equals("true"))
        {
            var acuratePos = position.Split('_');
            position = $"{acuratePos[0].Substring(acuratePos[0].Length - 1, 1)}_{acuratePos[1].Substring(acuratePos[1].Length - 1, 1)}_{acuratePos[2].Substring(acuratePos[2].Length - 1, 1)}";
        }
        return $"{block}|{position}";
    }

    private async void OnDisable()
    {
        if (client != null && client.State == WebSocketState.Open)
            await client.Close();
    }

    private async void Connect()
    {
        client = new WebSocket($"{server.GetWsScheme()}://{server.GetHost()}/ws/player/{server.GetPlayer()}/", server.GetHeaders());
        client.OnMessage += Receive;
        client.OnError += (e) =>
        {
            Debug.LogError(e);
        };
        client.OnClose += (e) =>
        {
            Debug.Log(e);
        };
        await client.Connect();
    }

    void Receive(byte[] data)
    {
        string res = Encoding.ASCII.GetString(data);
        if (res.StartsWith("pos:"))
        {
            var parts = res.Split(":");
            var player = parts[1];
            var block = parts[2].Split('_');
            var position = 8 * (Int8.From8AdicVector(parts[3]) + new Vector3(int.Parse(block[0]), int.Parse(block[1]), int.Parse(block[2])));
            OtherPlayer otherPlayer;
            if (otherPlayers.TryGetValue(player, out otherPlayer))
            {
                otherPlayer.transform.position = position;
            }
            else
            {
                otherPlayer = GameObject.Instantiate(playerPrefab, position, Quaternion.identity);
                otherPlayers.Add(player, otherPlayer);
                StartCoroutine(GetMesh(player, otherPlayer));
                lastPositionReport = "";
                lastCursorReport = "";
            }
        }
        else if (res.StartsWith("cur:"))
        {
            var parts = res.Split(":");
            var player = parts[1];
            OtherPlayer otherPlayer;
            if (otherPlayers.TryGetValue(player, out otherPlayer))
            {
                var block = parts[2].Split('_');
                var position = 8 * (Int8.From8AdicVector(parts[3]) + new Vector3(int.Parse(block[0]), int.Parse(block[1]), int.Parse(block[2])));
                otherPlayer.GetCursor().position = position;
            }
        }
        else if (res.StartsWith("act:"))
        {
            var parts = res.Split(":");
            var player = parts[1];
            var act = parts[2];
            OtherPlayer otherPlayer;
            if (otherPlayers.TryGetValue(player, out otherPlayer))
            {
                otherPlayer.action = act;
            }
        }
        else if (res.StartsWith("off:"))
        {
            var parts = res.Split(":");
            var player = parts[1];
            OtherPlayer otherPlayer;
            if (otherPlayers.TryGetValue(player, out otherPlayer))
            {
                Destroy(otherPlayer.gameObject);
                otherPlayers.Remove(player);
            }
        }
    }

    private IEnumerator GetMesh(string id, OtherPlayer otherPlayer)
    {
        UnityWebRequest loginRequest = UnityWebRequest.Get($"{server.GetHttpScheme()}://{server.GetHost()}/profile/{id}/");
        loginRequest.downloadHandler = new DownloadHandlerBuffer();
        loginRequest.useHttpContinue = false;
        loginRequest.redirectLimit = 0;
        loginRequest.timeout = 60;
        loginRequest.SendWebRequest();
        while (!loginRequest.isDone) yield return null;
        if ((int)(loginRequest.responseCode / 100) == 2)
        {
            while (!loginRequest.downloadHandler.isDone) yield return null;
            var res = loginRequest.downloadHandler.text;
            var json = JsonUtility.FromJson<Profile>(res);
            otherPlayer.GetCursor().transform.localScale = MeshParser.ParseOBJ(json.mesh, 1).bounds.max;
        }
    }
}
