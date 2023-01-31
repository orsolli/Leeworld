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

    public Transform GetCursor()
    {
        return cursor.transform;
    }
}
