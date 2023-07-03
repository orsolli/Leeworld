using UnityEngine;

public class BlueprintInstance
{
    private GameObject gameObject;
    private GameObject previewBlock;
    private GameObject[] previewBlocks;
    private Mesh mesh;
    private int sign;
    public Vector3 gridSize;

    public BlueprintInstance(GameObject root, GameObject previewBlockPrefab, Mesh mesh, Vector3 position, Vector3 size)
    {
        gameObject = root;
        previewBlock = previewBlockPrefab;
        this.mesh = mesh;
        gridSize = mesh.bounds.max;
        previewBlocks = new GameObject[64];

        UpdateBlueprint(position, size);
    }

    public void OnDestroy()
    {
        for (int i = 0; i < previewBlocks.Length; i++)
        {
            GameObject.Destroy(previewBlocks[i]);
        }
    }

    public void UpdateBlueprint(Vector3 dragStartPos, Vector3 size)
    {
        for (int i = 0; i < previewBlocks.Length; i++)
        {
            if (i > size.magnitude)
            {
                if (previewBlocks[i] != null) GameObject.Destroy(previewBlocks[i]);
            }
            else
            {
                if (previewBlocks[i] == null) previewBlocks[i] = GameObject.Instantiate(previewBlock);
                previewBlocks[i].transform.localScale = mesh.bounds.max;
                Vector3 step = size.normalized;
                step.Scale(gridSize);
                previewBlocks[i].transform.position = dragStartPos + step * i;
            }
        }
    }

}
