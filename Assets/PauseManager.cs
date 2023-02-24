using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public abstract class PauseManager : MonoBehaviour
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

    protected void Pause()
    {
        SetPause(true);
    }

    protected void UnPause(Scene scene)
    {
        SetPause(false);
    }
}
