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

    private async void OnDestroy()
    {
        if (client != null && client.State == WebSocketState.Open)
        {
            await client.Close();
        }
    }

    private void Connect()
    {
        client = new WebSocket($"{server.GetWsScheme()}://{server.GetHost()}/ws/blocks/{server.GetPlayer()}/", server.GetHeaders());

        client.OnError += (e) =>
        {
            Debug.LogError(e);
        };
        client.OnClose += (e) =>
        {
            Debug.Log(e);
        };
        client.Connect();
    }
}
