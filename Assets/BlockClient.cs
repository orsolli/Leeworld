using NativeWebSocket;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BlockClient : MonoBehaviour
{
    public WebSocket client;
    private Server server;
    private PauseManager pm;

    void OnEnable()
    {
        pm = FindObjectOfType<PauseManager>(true);
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
            pm.Pause();
        };
        client.OnClose += (e) =>
        {
            Debug.Log(e);
        };
        client.Connect();
    }
}
