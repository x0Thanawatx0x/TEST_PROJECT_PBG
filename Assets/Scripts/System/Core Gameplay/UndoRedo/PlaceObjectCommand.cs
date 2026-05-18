using UnityEngine;

public class PlaceObjectCommand : ICommand
{
    private ItemData itemData;

    private Vector3 position;
    private Quaternion rotation;

    private GameObject spawnedObject;

    public PlaceObjectCommand(ItemData itemData, Vector3 position, Quaternion rotation)
    {
        this.itemData = itemData;
        this.position = position;
        this.rotation = rotation;
    }

    public void Execute()
    {
        // ครั้งแรก
        if (spawnedObject == null)
        {
            if (itemData == null || itemData.prefab == null)
            {
                Debug.LogWarning("Missing prefab");
                return;
            }

            spawnedObject = GameObject.Instantiate(
                itemData.prefab,
                position,
                rotation
            );
        }
        else
        {
            // Undo -> Redo
            spawnedObject.SetActive(true);
        }

        ScaleEffect scale = spawnedObject.GetComponent<ScaleEffect>();

        if (scale != null)
        {
            scale.StartGrow();
        }
    }

    public void Undo()
    {
        if (spawnedObject == null)
            return;

        ScaleEffect scale = spawnedObject.GetComponent<ScaleEffect>();

        if (scale != null)
        {
            scale.StartShrink();
        }

        spawnedObject.SetActive(false);
    }

    public void Redo()
    {
        if (spawnedObject == null)
            return;

        spawnedObject.SetActive(true);

        ScaleEffect scale = spawnedObject.GetComponent<ScaleEffect>();

        if (scale != null)
        {
            scale.StartGrow();
        }
    }
}