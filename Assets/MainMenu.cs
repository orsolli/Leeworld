using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public TMPro.TMP_InputField PlayerInputField;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        PlayerInputField.text = PlayerPrefs.GetString("PLAYER");
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

    public void SetPlayer(string player)
    {
        PlayerPrefs.SetString("PLAYER", player);
    }

    public void Play()
    {
        SceneManager.LoadScene("OpenWorld", LoadSceneMode.Single);
    }

    public void Logout()
    {
        PlayerPrefs.DeleteAll();
        SceneManager.LoadScene("Login", LoadSceneMode.Single);
    }
}