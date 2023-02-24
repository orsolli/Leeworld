using System.Collections.Generic;
using UnityEngine;

public class Server : MonoBehaviour
{
    private string player;
    private string host;
    private string session;
    private bool secure = true;

    void OnEnable()
    {
        if (PlayerPrefs.GetString("SECURE").Equals(false.ToString()))
            secure = false;
        player = PlayerPrefs.GetString("PLAYER");
        if (player.Equals("")) {
            PlayerPrefs.SetString("PLAYER", "0");
            player = "0";
        }

        host = PlayerPrefs.GetString("SERVER");
        if (host.Equals("")) {
            host = LoginMenu.DEFAULT_SEREVR;
            PlayerPrefs.SetString("SERVER", LoginMenu.DEFAULT_SEREVR);
            secure = true;
        }
        session = PlayerPrefs.GetString("SESSION");
    }

    public string GetPlayer()
    {
        return player.Equals("0") ? PlayerPrefs.GetString("PLAYER", "0") : player;
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