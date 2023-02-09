using UnityEngine;

public class OtherPlayer : MonoBehaviour
{
    public GameObject cursorPrefab;
    private GameObject cursor;
    public string action;

    void Start()
    {
        cursor = Instantiate(cursorPrefab, Vector3.zero, Quaternion.identity);
    }

    void Update()
    {
        if (action.Equals("digg"))
        {
            cursor.transform.Rotate(Vector3.up * Time.deltaTime / 5);
        }
        else
        {
            cursor.transform.rotation = Quaternion.identity;
        }
    }

    public Transform GetCursor()
    {
        return cursor.transform;
    }
}
