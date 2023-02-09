using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoginMenu : MonoBehaviour
{
    public TMPro.TMP_InputField ServerInputField;
    public TMPro.TMP_InputField UsernameInputField;
    public TMPro.TMP_InputField PasswordInputField;
    public Button LoginButton;
    public GameObject LoadingIcon;
    public TMPro.TMP_Text ErrorMessage;
    private UnityWebRequest loginRequest;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Authenticate();
        string host = PlayerPrefs.GetString("SERVER");
        if (!host.Equals("") && PlayerPrefs.GetString("SECURE").Equals(false.ToString()))
        {
            host = "http://" + host;
        }
        ServerInputField.text = host;
        UsernameInputField.text = PlayerPrefs.GetString("USERNAME");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
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

    private bool Authenticate()
    {
        var expires = PlayerPrefs.GetString("EXPIRES");
        if (!PlayerPrefs.GetString("SESSION").Equals("") && (expires.Equals("") || DateTime.Parse(expires).CompareTo(DateTime.Now) > 0))
        {
            SceneManager.LoadScene("MainMenu");
            return true;
        }
        return false;
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

    private IEnumerator TryLogin()
    {
        ServerInputField.interactable = false;
        UsernameInputField.interactable = false;
        PasswordInputField.interactable = false;
        ErrorMessage.text = "";
        var loginAttempt = TryLogin();
        LoadingIcon.SetActive(true);
        try
        {
            var scheme = "https";
            if (PlayerPrefs.GetString("SECURE", "true").Equals(false.ToString()))
                scheme = "http";
            string host = PlayerPrefs.GetString("SERVER", "localhost");
            if (loginRequest != null)
            {
                loginRequest.Abort();
                yield break;
            }
            loginRequest = UnityWebRequest.Get($"{scheme}://{host}/auth/login/");
            loginRequest.useHttpContinue = false;
            loginRequest.redirectLimit = 0;
            loginRequest.timeout = 60;
            loginRequest.SendWebRequest();
            while (!loginRequest.isDone)
            {
                LoadingIcon.transform.Rotate(Vector3.forward);
                yield return null;
            };
            if ((int)(loginRequest.responseCode / 100) == 2)
            {
                var cookieValues = loginRequest.GetResponseHeader("set-cookie").Split("; ");
                if (cookieValues[0].StartsWith("csrftoken="))
                {
                    var csrf = cookieValues[0].Substring("csrftoken=".Length);
                    loginRequest = UnityWebRequest.Post($"{scheme}://{host}/auth/login/", new Dictionary<string, string>
                    {
                        ["csrfmiddlewaretoken"] = csrf,
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

                    string sessionCookie = loginRequest.GetResponseHeader("set-cookie").Split("sessionid=")[1];
                    PlayerPrefs.SetString("SESSION", sessionCookie.Split("; ")[0]);
                    PlayerPrefs.SetString("EXPIRES", sessionCookie.Split("expires=")[1].Split("; ")[0]);

                    if (!Authenticate())
                    {
                        ErrorMessage.text = "Try again";
                    }
                }
            }
            else
            {
                ErrorMessage.text = loginRequest.error;
            }
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
