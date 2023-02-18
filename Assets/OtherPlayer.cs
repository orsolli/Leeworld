using UnityEngine;

public class OtherPlayer : MonoBehaviour
{
    public GameObject cursorPrefab;
    private GameObject cursor;
    public string action { get; set; }

    void Start()
    {
        cursor = Instantiate(cursorPrefab, Vector3.zero, Quaternion.identity);
    }

    void Update()
    {
        if (action != null && action.Equals("digg"))
        {
            cursor.transform.GetChild(0).Rotate(Vector3.up * Time.deltaTime * 90 / 5);
        }
        else
        {
            cursor.transform.GetChild(0).rotation = Quaternion.identity;
        }
    }

    void OnDestroy()
    {
        Destroy(cursor);
    }

    public Transform GetCursor()
    {
        return cursor.transform;
    }
}
