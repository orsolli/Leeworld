using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public TMPro.TMP_InputField PlayerInputField;
    public TMPro.TMP_Dropdown PlayerInputSelector;

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
            if (PlayerInputField.enabled)
                PlayerInputField.Select();
            else if (PlayerInputSelector.enabled)
                PlayerInputSelector.Select();
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
        UnityWebRequest loginRequest = UnityWebRequest.Get($"{scheme}://{host}/profiles/");
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
            PlayerInputSelector.ClearOptions();
            if (res.Length > 0)
            {
                PlayerInputSelector.AddOptions(res.Split(", ").ToList());
                PlayerInputSelector.gameObject.SetActive(true);
                PlayerInputField.gameObject.SetActive(false);
                PlayerInputSelector.value = 0;
                PlayerInputSelector.value = PlayerInputSelector.options.FindIndex((option) => option.text.Equals(PlayerInputField.text));

            }
            else
            {
                PlayerInputSelector.gameObject.SetActive(false);
                PlayerInputField.gameObject.SetActive(true);
            }
        }
    }

    public void SetPlayer(string player)
    {
        PlayerPrefs.SetString("PLAYER", player.Trim());
    }

    public void SelectPlayer(int playerIndex)
    {
        PlayerPrefs.SetString("PLAYER", PlayerInputSelector.options[playerIndex].text);
    }

    public void Play()
    {
        if (!PlayerPrefs.GetString("PLAYER").Contains(","))
            SceneManager.LoadScene("OpenWorld");
    }

    public void Logout()
    {
        var scheme = "https";
        if (PlayerPrefs.GetString("SECURE", "true").Equals(false.ToString()))
            scheme = "http";
        string host = PlayerPrefs.GetString("SERVER", "localhost");
        PlayerPrefs.DeleteAll();
        UnityWebRequest loginRequest = UnityWebRequest.Get($"{scheme}://{host}/auth/logout/");
        loginRequest.downloadHandler = new DownloadHandlerBuffer();
        loginRequest.useHttpContinue = false;
        loginRequest.redirectLimit = 0;
        loginRequest.timeout = 120;
        loginRequest.SendWebRequest();
        SceneManager.LoadScene("Login");
    }
}