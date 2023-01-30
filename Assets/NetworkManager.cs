using System;
using System.Collections.Generic;
using System.Text;
using Mirror.SimpleWeb;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    public OtherPlayer playerPrefab;
    private Server server;
    private SimpleWebClient client;
    private Dictionary<string, OtherPlayer> otherPlayers = new Dictionary<string, OtherPlayer>();
    private string lastPositionReport = "";

    void Start()
    {
        server = FindObjectOfType<Server>(true);
        if (server.GetPlayer() == null || server.GetPlayer() == "")
        {
            Destroy();
            return;
        }
        Connect();
        StartCoroutine(ProcessMessages());
    }

    void Update()
    {
        var pos = transform.position;
        var block_pos = pos / 8;
        var block = $"{(int)block_pos.x}_{(int)block_pos.y}_{(int)block_pos.z}";
        var position = $"{ToUInt8(pos.x % 8)}_{ToUInt8(pos.y % 8)}_{ToUInt8(pos.z % 8)}";
        var positionReport = $"{position}|{block}";
        if (!lastPositionReport.Equals(positionReport))
        {
            client.Send(new ArraySegment<byte>(Encoding.ASCII.GetBytes($"{{\"player\":\"{server.GetPlayer()}\",\"block\":\"{block}\",\"position\":\"{position}\"}}")));
            lastPositionReport = positionReport;
        }
    }

    IEnumerator<int> ProcessMessages()
    {
        while (this && client != null)
        {
            client.ProcessMessageQueue(this);
            yield return 0;
        }
    }

    public void Destroy()
    {
        client.onDisconnect -= Connect;
        client.Disconnect();
        StopAllCoroutines();
    }

    private void Connect()
    {
        client = SimpleWebClient.Create(2048, 64, new TcpConfig(true, 30000, 30000));
        client.Connect(new Uri($"ws://{server.GetHost()}/ws/player/{server.GetPlayer()}/"));
        client.onData += Receive;
        client.onDisconnect += Connect;
    }

    void Receive(ArraySegment<byte> data)
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

    private string ToUInt8(float f)
    {
        string res = "";
        int i = (int)f;
        while (f > (int)f && res.Length < 3)
        {
            i = (int)f;
            res = $"{res}{i}";
            f -= i;
            f *= 8;
        }
        if (res.Length == 0)
            return "0";
        return res;
    }
}
