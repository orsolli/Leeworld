using UnityEngine;

public class OtherPlayer : MonoBehaviour
{
    static int TIME = 2;
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
            cursor.transform.GetChild(0).Rotate(Vector3.up * Time.deltaTime * 90 / TIME);
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
