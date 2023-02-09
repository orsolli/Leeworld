using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public TMPro.TMP_InputField PlayerInputField;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        PlayerInputField.text = PlayerPrefs.GetString("PLAYER");
        StartCoroutine(GetProfiles());
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
        else if (Input.GetKeyUp(KeyCode.Tab))
        {
            PlayerInputField.Select();
        }
        else if (Input.GetKeyUp(KeyCode.Return))
        {
            Play();
        }
    }

    private IEnumerator GetProfiles()
    {
        var scheme = "https";
        if (PlayerPrefs.GetString("SECURE", "true").Equals(false.ToString()))
            scheme = "http";
        string host = PlayerPrefs.GetString("SERVER", "localhost");
        string session = PlayerPrefs.GetString("SESSION");
        UnityWebRequest loginRequest = UnityWebRequest.Get($"{scheme}://{host}/profiles/");
        loginRequest.downloadHandler = new DownloadHandlerBuffer();
        loginRequest.useHttpContinue = false;
        loginRequest.redirectLimit = 0;
        loginRequest.timeout = 60;
        loginRequest.SetRequestHeader("Cookie", $"sessionid={session}");
        loginRequest.SendWebRequest();
        while (!loginRequest.isDone) yield return null;
        if ((int)(loginRequest.responseCode / 100) == 2)
        {
            while (!loginRequest.downloadHandler.isDone) yield return 0;
            if (!loginRequest.downloadHandler.text.Contains(PlayerInputField.text))
                PlayerInputField.text = loginRequest.downloadHandler.text;
        }
    }

    public void SetPlayer(string player)
    {
        PlayerPrefs.SetString("PLAYER", player.Trim());
    }

    public void Play()
    {
        if (!PlayerPrefs.GetString("PLAYER").Contains(","))
            SceneManager.LoadScene("OpenWorld", LoadSceneMode.Single);
    }

    public void Logout()
    {
        PlayerPrefs.DeleteAll();
        SceneManager.LoadScene("Login", LoadSceneMode.Single);
    }
}