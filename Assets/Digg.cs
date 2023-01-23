using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class Digg : MonoBehaviour
{
    public int player = 1;
    public Vector3 offset;
    public GameObject previewBlock;
    public Transform progressBlock;
    private Transform targetTransform;
    public LayerMask layerMask;
    private IEnumerator<float> diggingCoroutine;
    [ReadOnly(true)] public float progress;
    private Block block;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Cast a ray from the mouse position
        Ray ray = Camera.main.ScreenPointToRay(offset + new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        if (diggingCoroutine == null)
        {
            // Check if the ray hits a collider on the specified layer
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask))
            {
                previewBlock.SetActive(true);
                targetTransform = hit.transform;
                Vector3 gridSize = previewBlock.transform.localScale;
                // Position the previewBlock at the hit point
                previewBlock.transform.position = new Vector3(
                    Mathf.Round((hit.point.x - hit.normal.x / 512 - gridSize.x * 0.5f) / gridSize.x) * gridSize.x,
                    Mathf.Round((hit.point.y - hit.normal.y / 512 - gridSize.y * 0.5f) / gridSize.y) * gridSize.y,
                    Mathf.Round((hit.point.z - hit.normal.z / 512 - gridSize.z * 0.5f) / gridSize.z) * gridSize.z);
                //previewBlock.transform.rotation = Quaternion.LookRotation(hit.normal);
            }
            else
            {
                previewBlock.SetActive(false);
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            // Release mouse when debug incase of infinite loop.
            // Cursor.lockState = CursorLockMode.None;
            // Cursor.visible = true;
            // Start the long-running task
            if (diggingCoroutine == null)
            {
                //Mesh m = targetTransform.gameObject.GetComponent<MeshFilter>().mesh;
                //m.SetColors(new List<Color>());
                //m.SetTangents(new List<Vector4>());
                //m.SetNormals(new List<Vector3>());
                //AssetDatabase.CreateAsset(m, "Assets/kube.asset");
                progress = 0;
                block = targetTransform.GetComponent<Block>();
                diggingCoroutine = block.Digg(previewBlock.transform, player);
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            // Cursor.lockState = CursorLockMode.Locked;
            // Cursor.visible = false;
            //Mesh m = targetTransform.gameObject.GetComponent<MeshFilter>().mesh;
            //AssetDatabase.CreateAsset(m, "Assets/kube.asset");
            // Stop the long-running task
            if (diggingCoroutine != null)
            {
                block.StopDigg(previewBlock.transform);
                block = null;
                diggingCoroutine = null;
                progress = 0;
            }
        }

        if (diggingCoroutine != null)
        {
            if (diggingCoroutine.MoveNext())
            {
                progress = diggingCoroutine.Current;
            }
            else
            {
                diggingCoroutine = null;
                progress = 1;
            }
        }
        progressBlock.SetLocalPositionAndRotation(progressBlock.localPosition, Quaternion.AngleAxis(90 * progress, Vector3.up));
    }
}
