using System.Collections.Generic;
using UnityEngine;

public class Server : MonoBehaviour
{
    private string player;
    private string host;
    private string session;
    private bool secure = true;
    System.Diagnostics.Process process;

    void Awake()
    {
        if (PlayerPrefs.GetString("SECURE").Equals(false.ToString()))
            secure = false;
        player = PlayerPrefs.GetString("PLAYER", player);
        host = PlayerPrefs.GetString("SERVER", host);
        session = PlayerPrefs.GetString("SESSION", session);
    }

    public string GetPlayer()
    {
        return player;
    }
    public string GetHost()
    {
        return host;
    }
    public string GetHttpScheme()
    {
        if (secure)
            return "https";
        return "http";
    }
    public string GetWsScheme()
    {
        if (secure)
            return "wss";
        return "ws";
    }
    public Dictionary<string, string> GetHeaders()
    {
        var headers = new Dictionary<string, string>();
        headers.Add("Cookie", $"sessionid={session}");
        return headers;
    }
}