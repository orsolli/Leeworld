using NativeWebSocket;
using UnityEngine;

public class BlockClient : MonoBehaviour
{
    public WebSocket client;
    private Server server;

    void Start()
    {
        server = FindObjectOfType<Server>(true);
        Connect();
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        if (client != null && client.State == WebSocketState.Open) client.DispatchMessageQueue();
#endif
    }

    public void Destroy()
    {
        if (client != null && client.State == WebSocketState.Open)
        {
            client.CancelConnection();
        }
    }

    private void Connect()
    {
        client = new WebSocket($"{server.GetWsScheme()}://{server.GetHost()}/ws/blocks/{server.GetPlayer()}/", server.GetHeaders());

        client.OnError += (e) => { Debug.LogError(e); Destroy(); };
        client.OnClose += (e) => { Debug.Log(e); Destroy(); };
        client.Connect();
    }
}
