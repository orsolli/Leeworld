using NativeWebSocket;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BlockClient : PauseManager
{
    public WebSocket client;
    private Server server;

    void OnEnable()
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

    private async void OnDisable()
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
            SceneManager.LoadScene("Login", LoadSceneMode.Additive);
            Pause();
        };
        client.OnClose += (e) =>
        {
            Debug.Log(e);
        };
        client.Connect();
    }
}
