using UnityEngine;

public class Tools : MonoBehaviour
{
    public GameObject[] tools;
    private GameObject activeTool;
    public int activeToolIndex;

    void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        for (int i = 0; i < tools.Length; i++)
        {
            if (Input.GetKeyDown($"{i}")) activeToolIndex = i + 1;
        }
        if (Input.GetKeyDown("tab"))
        {
            activeToolIndex = (activeToolIndex + 1) % tools.Length;
        }
        if (activeToolIndex >= 0)
        {
            if (activeTool == null || !tools[activeToolIndex].name.Equals(activeTool.name))
            {
                if (activeTool) Destroy(activeTool);
                activeTool = Instantiate(tools[activeToolIndex], transform);
                activeTool.name = tools[activeToolIndex].name;
                SendMessage("UpdateTool");
            }
        }
    }
}
