using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoginMenu : MonoBehaviour
{
    public static string DEFAULT_SEREVR = "sollisoft.com";
    private string connectedServer = DEFAULT_SEREVR;
    public TMPro.TMP_InputField ServerInputField;
    public TMPro.TMP_InputField UsernameInputField;
    public TMPro.TMP_InputField PasswordInputField;
    public GameObject LoadingIcon;
    public TMPro.TMP_Text ErrorMessage;
    private UnityWebRequest loginRequest;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        string host = PlayerPrefs.GetString("SERVER");
        if (host.Equals("")) {
            PlayerPrefs.SetString("SERVER", DEFAULT_SEREVR);
            host = DEFAULT_SEREVR;
        }
        connectedServer = host;
        if (PlayerPrefs.GetString("SECURE").Equals(false.ToString()))
        {
            host = "http://" + host;
        }
        ServerInputField.text = host;
        UsernameInputField.text = PlayerPrefs.GetString("USERNAME");
        if (host.Length > 0)
            StartCoroutine(Authenticate());
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
        }
        else if (Input.GetKeyUp(KeyCode.Tab))
        {
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                if (PasswordInputField.isFocused)
                {
                    UsernameInputField.Select();
                }
                else if (ServerInputField.isFocused)
                {
                    PasswordInputField.Select();
                }
                else if (UsernameInputField.isFocused)
                {
                    ServerInputField.Select();
                }
            }
            else
            {
                if (UsernameInputField.isFocused)
                {
                    PasswordInputField.Select();
                }
                else if (PasswordInputField.isFocused)
                {
                    ServerInputField.Select();
                }
                else
                {
                    UsernameInputField.Select();
                }
            }
        }
        else if (Input.GetKeyUp(KeyCode.Return))
        {
            Login();
        }
    }

    private IEnumerator Authenticate()
    {
        ServerInputField.interactable = false;
        ErrorMessage.text = "";
        LoadingIcon.SetActive(true);
        try
        {
            var scheme = "https";
            if (PlayerPrefs.GetString("SECURE", "true").Equals(false.ToString()))
                scheme = "http";
            string host = PlayerPrefs.GetString("SERVER");
            UnityWebRequest authRequest = UnityWebRequest.Get($"{scheme}://{host}/auth/login/");
            authRequest.downloadHandler = new DownloadHandlerBuffer();
            authRequest.useHttpContinue = false;
            authRequest.redirectLimit = 0;
            authRequest.timeout = 3;
            authRequest.SendWebRequest();
            while (!authRequest.isDone)
            {
                LoadingIcon.transform.Rotate(Vector3.forward);
                yield return null;
            };
            if ((int)(authRequest.responseCode / 100) == 2)
            {
                PlayerPrefs.SetString("SESSION_ACTIVE", "true");
                SceneManager.LoadScene("OpenWorld"); // Restart
            } else {
                PlayerPrefs.SetString("SESSION_ACTIVE", "");
            }
        }
        finally
        {
            LoadingIcon.SetActive(false);
            ServerInputField.interactable = true;
        }
    }

    public void SetServer(string server)
    {
        var isHttps = true;
        if (server.StartsWith("http://"))
        {
            isHttps = false;
            server = server.Substring("http://".Length);
        }
        else if (server.StartsWith("https://"))
        {
            isHttps = true;
            server = server.Substring("https://".Length);
        }
        if (server.Equals(""))
        {
            isHttps = true;
            server = DEFAULT_SEREVR;
        }

        PlayerPrefs.SetString("SECURE", isHttps.ToString());
        PlayerPrefs.SetString("SERVER", server);
    }

    public void SetUsername(string username)
    {
        PlayerPrefs.SetString("USERNAME", username);
    }

    public void Login()
    {
        StartCoroutine(TryLogin());
    }

    public void Close()
    {
        if (loginRequest != null) loginRequest.Abort();
        StopAllCoroutines();
        if (connectedServer.Equals(PlayerPrefs.GetString("SERVER")))
            SceneManager.UnloadSceneAsync("Login");
        else
            SceneManager.LoadScene("OpenWorld"); // Restart
    }

    private IEnumerator TryLogin()
    {
        ServerInputField.interactable = false;
        UsernameInputField.interactable = false;
        PasswordInputField.interactable = false;
        ErrorMessage.text = "";
        LoadingIcon.SetActive(true);
        try
        {
            var scheme = "https";
            if (PlayerPrefs.GetString("SECURE", "true").Equals(false.ToString()))
                scheme = "http";
            string host = PlayerPrefs.GetString("SERVER");
            if (loginRequest != null)
            {
                loginRequest.Abort();
                yield break;
            }
            loginRequest = UnityWebRequest.Post($"{scheme}://{host}/auth/login/", new Dictionary<string, string>
            {
                ["username"] = UsernameInputField.text,
                ["password"] = PasswordInputField.text,
            });
            loginRequest.useHttpContinue = false;
            loginRequest.redirectLimit = 0;
            loginRequest.timeout = 60;
            loginRequest.SendWebRequest();
            while (!loginRequest.isDone)
            {
                LoadingIcon.transform.Rotate(Vector3.forward);
                yield return null;
            };

#if UNITY_EDITOR
            string cookie = loginRequest.GetResponseHeader("Set-Cookie");
            if (cookie != null && cookie.Contains("sessionid="))
                PlayerPrefs.SetString("SESSION", cookie.Split("sessionid=")[1].Split("; ")[0]);
#endif

            ErrorMessage.text = loginRequest.error;
            StopAllCoroutines();
            StartCoroutine(Authenticate());
        }
        finally
        {
            LoadingIcon.SetActive(false);
            ServerInputField.interactable = true;
            UsernameInputField.interactable = true;
            PasswordInputField.interactable = true;
            loginRequest = null;
        }
    }
}
