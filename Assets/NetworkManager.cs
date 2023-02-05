using System.Collections.Generic;
using System.Text;
using NativeWebSocket;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkManager : MonoBehaviour
{
    public OtherPlayer playerPrefab;
    public GameObject cursorTransform;
    private Server server;
    private WebSocket client;
    private Dictionary<string, OtherPlayer> otherPlayers = new Dictionary<string, OtherPlayer>();
    private string lastPositionReport = "";
    private string lastCursorReport = "";

    void Start()
    {
        server = FindObjectOfType<Server>(true);
        if (server.GetPlayer() == null || server.GetPlayer() == "")
        {
            Destroy();
            return;
        }
        Connect();
    }

    async void Update()
    {

        var position = GetUpdate(transform.position);
        if (!lastPositionReport.Equals(position))
        {
            await client.Send(Encoding.ASCII.GetBytes(
                $"{{\"part\":\"player\",\"block\":\"{position.Split('|')[0]}\",\"position\":\"{position.Split('|')[1]}\"}}"
            ));
            lastPositionReport = position;
        }

        var cursor = GetUpdate(cursorTransform.transform.position);
        if (!lastCursorReport.Equals(cursor))
        {
            await client.Send(Encoding.ASCII.GetBytes(
                $"{{\"part\":\"cursor\",\"block\":\"{cursor.Split('|')[0]}\",\"position\":\"{cursor.Split('|')[1]}\"}}"
            ));
            lastCursorReport = cursor;
        }
#if !UNITY_WEBGL || UNITY_EDITOR
        client.DispatchMessageQueue();
#endif
    }
    private string GetUpdate(Vector3 pos)
    {
        var block_pos = pos / 8;
        var block = $"{(int)block_pos.x}_{(int)block_pos.y}_{(int)block_pos.z}";
        var position = $"{Int8.ToUInt8(pos.x % 8)}_{Int8.ToUInt8(pos.y % 8)}_{Int8.ToUInt8(pos.z % 8)}";
        return $"{block}|{position}";
    }

    public void Destroy()
    {
        client.Close();
        StopAllCoroutines();
    }

    private async void Connect()
    {
        client = new WebSocket($"wss://{server.GetHost()}/ws/player/{server.GetPlayer()}/");
        client.OnMessage += Receive;
        client.OnClose += (e) =>
        {
            SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
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
            var position = FromUInt8Vector(parts[3]) + 8 * new Vector3(int.Parse(block[0]), int.Parse(block[1]), int.Parse(block[2]));
            OtherPlayer otherPlayer;
            if (otherPlayers.TryGetValue(player, out otherPlayer))
            {
                otherPlayer.transform.position = position;
            }
            else
            {
                otherPlayer = GameObject.Instantiate(playerPrefab, position, Quaternion.identity);
                otherPlayers.Add(player, otherPlayer);
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
                var position = FromUInt8Vector(parts[3]) + 8 * new Vector3(int.Parse(block[0]), int.Parse(block[1]), int.Parse(block[2]));
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
                Destroy(otherPlayer);
                otherPlayers.Remove(player);
            }
        }
    }

    private Vector3 FromUInt8Vector(string vector)
    {
        string[] vectorParts = vector.Split('_');
        string x = vectorParts[0];
        string y = vectorParts[1];
        string z = vectorParts[2];
        return new Vector3(FromUInt8(x), FromUInt8(y), FromUInt8(z));
    }

    private float FromUInt8(string num)
    {
        int i = 1;
        float f = 0;
        while (num.Length > 0)
        {
            int n = int.Parse(num.Substring(0, 1));
            f += (float)n / i;
            i = i * 8;
            num = num.Substring(1);
        }
        return f;
    }
}
