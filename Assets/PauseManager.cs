using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    public List<GameObject> disableGameObjectsOnPause = new List<GameObject>();

    void Start()
    {
        SceneManager.sceneUnloaded += UnPause;
    }
    void OnDestroy()
    {
        SceneManager.sceneUnloaded -= UnPause;
    }
    private void SetPause(bool pause)
    {
        foreach (var go in disableGameObjectsOnPause)
        {
            go.SetActive(!pause);
        }
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!SceneManager.GetSceneByName("MainMenu").isLoaded) SceneManager.LoadScene("MainMenu", LoadSceneMode.Additive);
            Pause();
        }
    }

    public void Pause()
    {
        SetPause(true);
    }

    public void UnPause(Scene scene)
    {
        SetPause(false);
    }
}
