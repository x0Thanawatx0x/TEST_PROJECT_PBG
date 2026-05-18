using UnityEngine;

public class ObjectPlacementHandler : MonoBehaviour
{
    [Header("Object Lists")]
    [SerializeField] private ItemData[] houseItems;
    [SerializeField] private ItemData[] furnitureItems;
    [SerializeField] private ItemData[] natureItems;

    [Header("Layers")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask buildableLayer;

    [Header("Preview")]
    [SerializeField] private Material previewMaterial;

    private PlacementSystem placementSystem;

    private GameObject currentPreview;
    private ItemData currentPreviewItem;

    // =========================
    // INITIALIZE
    // =========================

    public void Initialize(PlacementSystem system)
    {
        placementSystem = system;
    }

    // =========================
    // HOUSE
    // =========================

    public void HandleHousePlacement(Camera cam, int index)
    {
        if (houseItems == null || houseItems.Length == 0)
            return;

        if (index < 0 || index >= houseItems.Length)
            return;

        HandlePlacement(cam, houseItems[index]);
    }

    // =========================
    // FURNITURE
    // =========================

    public void HandleMultiPlacement(Camera cam, int index)
    {
        if (furnitureItems == null || furnitureItems.Length == 0)
            return;

        if (index < 0 || index >= furnitureItems.Length)
            return;

        HandlePlacement(cam, furnitureItems[index]);
    }

    // =========================
    // NATURE
    // =========================

    public void HandleNatureSpline(Camera cam, int index)
    {
        if (natureItems == null || natureItems.Length == 0)
            return;

        if (index < 0 || index >= natureItems.Length)
            return;

        HandlePlacement(cam, natureItems[index]);
    }

    // =========================
    // MAIN PLACE LOGIC
    // =========================

    private void HandlePlacement(Camera cam, ItemData item)
    {
        if (cam == null || item == null || item.prefab == null)
            return;

        CreateOrUpdatePreview(item);

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer))
        {
            Vector3 pos = hit.point;

            if (placementSystem != null)
            {
                pos = placementSystem.SnapToGrid(pos);
            }

            // MOVE PREVIEW
            if (currentPreview != null)
            {
                currentPreview.transform.position = pos;
            }

            // PLACE OBJECT
            if (Input.GetMouseButtonDown(0))
            {
                ICommand cmd = new PlaceObjectCommand(
                    item,
                    pos,
                    Quaternion.identity
                );

                CommandManager.Instance?.AddCommand(cmd);

                // รีเฟรช preview กัน state ค้าง
                ForceRefreshPreview();
            }
        }
    }

    // =========================
    // PREVIEW SYSTEM
    // =========================

    private void CreateOrUpdatePreview(ItemData item)
    {
        if (currentPreviewItem == item && currentPreview != null)
            return;

        HideAllPreviews();

        currentPreviewItem = item;

        // ใช้ Preview Prefab ถ้ามี
        GameObject previewToSpawn = item.previewPrefab != null
            ? item.previewPrefab
            : item.prefab;

        currentPreview = Instantiate(previewToSpawn);

        currentPreview.name = previewToSpawn.name + "_Preview";

        // ถ้าไม่มี Preview Prefab ค่อยใช้ ghost material
        if (item.previewPrefab == null)
        {
            ApplyPreviewMaterial(currentPreview);
        }

        // ปิด collider preview
        Collider[] cols = currentPreview.GetComponentsInChildren<Collider>();

        foreach (Collider col in cols)
        {
            col.enabled = false;
        }

        // ตั้ง layer preview
        SetLayerRecursively(currentPreview, LayerMask.NameToLayer("Ignore Raycast"));
    }

    private void ApplyPreviewMaterial(GameObject obj)
    {
        if (previewMaterial == null)
            return;

        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

        foreach (Renderer rend in renderers)
        {
            Material[] mats = new Material[rend.materials.Length];

            for (int i = 0; i < mats.Length; i++)
            {
                mats[i] = previewMaterial;
            }

            rend.materials = mats;
        }
    }

    public void HideAllPreviews()
    {
        if (currentPreview != null)
        {
            DestroyImmediate(currentPreview);
        }

        currentPreview = null;
        currentPreviewItem = null;
    }

    public void ForceRefreshPreview()
    {
        HideAllPreviews();
    }

    // =========================
    // HELPERS
    // =========================

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj == null)
            return;

        obj.layer = layer;

        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    // =========================
    // DELETE
    // =========================

    private void Update()
    {
        HandleDeletion();
    }

    private void HandleDeletion()
    {
        if (!Input.GetKeyDown(KeyCode.Alpha7))
            return;

        if (Camera.main == null)
            return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, buildableLayer))
        {
            ICommand cmd = new DeleteObjectCommand(hit.collider.gameObject);

            CommandManager.Instance?.AddCommand(cmd);

            ForceRefreshPreview();
        }
    }
}