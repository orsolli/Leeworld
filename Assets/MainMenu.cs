using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public TMPro.TMP_InputField ServerInputField;
    public TMPro.TMP_InputField PythonInputField;
    public TMPro.TMP_InputField PlayerInputField;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        ServerInputField.text = PlayerPrefs.GetString("SERVER");
        PythonInputField.text = PlayerPrefs.GetString("PYTHON_PATH"); //"/home/orsolli/.local/share/virtualenvs/Server-74Jy8cNt/bin");
        PlayerInputField.text = PlayerPrefs.GetString("PLAYER");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

    public void SetServer(string server)
    {
        PlayerPrefs.SetString("SERVER", server);
    }

    public void SetPlayer(string player)
    {
        PlayerPrefs.SetString("PLAYER", player);
    }

    public void Play()
    {
        SceneManager.LoadScene("OpenWorld");
    }

    public void ToggleAdvancedMenu(GameObject AdvancedMenu)
    {
        AdvancedMenu.SetActive(!AdvancedMenu.activeSelf);
    }

    public void SetPythonPath(string pythonPath)
    {
        PlayerPrefs.SetString("PYTHON_PATH", pythonPath); //"/home/orsolli/.local/share/virtualenvs/Server-74Jy8cNt/bin");
    }
}