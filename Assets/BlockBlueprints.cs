using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class BlockBlueprints : MonoBehaviour
{
    public GameObject blueprintPrefab;
    private List<BlueprintInstance> blueprints;
    private Dictionary<string, Profile> profiles = new Dictionary<string, Profile>();
    private List<string> fetchingProfile = new List<string>();
    private string block_id;
    private bool bussy = false;
    private Server server;
    private BlockClient client;

    void Start()
    {
        block_id = $"{transform.position.x / 8}_{transform.position.y / 8}_{transform.position.z / 8}";
        server = FindObjectOfType<Server>(true);
        client = FindObjectOfType<BlockClient>(true);
        blueprints = new List<BlueprintInstance>();
        StartCoroutine(UpdateBlueprints(2));
        client.client.OnMessage += OnMessage;
    }

    private void OnDestroy()
    {
        client.client.OnMessage -= OnMessage;
    }

    void OnMessage(byte[] data)
    {
        string res = System.Text.Encoding.ASCII.GetString(data);
        if (res.StartsWith("block:") && block_id.Equals(res.Split(':')[1]))
        {
            StartCoroutine(UpdateBlueprints(10));
        }
    }

    public IEnumerator<int> UpdateBlueprints(int recursionLimit)
    {
        for (int i = 0; i < 100; i++) yield return 0; // Delay for a number of frames
        if (!bussy)
        {
            bussy = true;
            UnityWebRequest request = UnityWebRequest.Get($"{server.GetHttpScheme()}://{server.GetHost()}/blueprint/?block={block_id}");
            request.downloadHandler = new DownloadHandlerBuffer();
            request.useHttpContinue = false;
            request.redirectLimit = 0;
            request.timeout = 60;
            foreach (var header in server.GetHeaders())
            {
                request.SetRequestHeader(header.Key, header.Value);
            }
            request.SendWebRequest();
            while (!request.isDone) yield return 0;
            if ((int)(request.responseCode / 100) == 2)
            {
                while (!request.downloadHandler.isDone) yield return 0;
                Debug.Log(request.downloadHandler.text);
                var json = "{\"blueprints\":" + request.downloadHandler.text + "}";
                var bs = JsonUtility.FromJson<BlueprintList>(json).blueprints;
                foreach (Blueprint blueprint in bs)
                {
                    StartCoroutine(GetMesh(blueprint));
                    yield return 0;
                }
            }
            else
            {
                if (recursionLimit > 0)
                {
                    yield return 0;
                    StartCoroutine(UpdateBlueprints(recursionLimit - 1));
                }
                else
                {
                    Debug.LogError($"Failed to fetch blueprints for {block_id}");
                }
            }
            bussy = false;
        }
    }

    private IEnumerator GetMesh(Blueprint blueprint)
    {
        if (!profiles.ContainsKey(blueprint.player))
        {
            if (!fetchingProfile.Contains(blueprint.player))
            {
                fetchingProfile.Add(blueprint.player);
                UnityWebRequest loginRequest = UnityWebRequest.Get($"{server.GetHttpScheme()}://{server.GetHost()}/profile/{blueprint.player}/");
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
                    profiles.Add(blueprint.player, JsonUtility.FromJson<Profile>(res));
                }
                fetchingProfile.Remove(blueprint.player);
            }
            else
            {
                while (fetchingProfile.Contains(blueprint.player)) yield return null;
            }
        }
        if (profiles.TryGetValue(blueprint.player, out var profile))
        {
            var mesh = MeshParser.ParseOBJ(profile.mesh, 1);

            var block = blueprint.block.Split(',');
            var position = 8 * (Int8.From8AdicVector(blueprint.position.Replace(',', '_')) + new Vector3(int.Parse(block[0]), int.Parse(block[1]), int.Parse(block[2])));
            var size = new Vector3(blueprint.size_x, blueprint.size_y, blueprint.size_z);

            blueprints.Add(new BlueprintInstance(gameObject, blueprintPrefab, mesh, position, size));
        }
        else
        {
            Debug.Log("Failed to get profile from cache");
        }
    }
}
