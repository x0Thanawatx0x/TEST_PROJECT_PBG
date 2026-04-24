using UnityEngine;

public class PlacementSystem : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float gridSize = 1f;

    [Header("Prefabs")]
    [SerializeField] private GameObject housePrefab;
    [SerializeField] private GameObject previewPrefab;

    [Header("Tool System")]
    [SerializeField] private ToolManager toolManager;

    private GameObject currentPreview;
    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;

        if (previewPrefab != null)
        {
            currentPreview = Instantiate(previewPrefab);
            currentPreview.SetActive(false);
        }
    }

    void Update()
    {
        // ❌ ถ้าไม่ใช่ House → ปิดระบบ
        if (toolManager == null || toolManager.currentTool != ToolManager.BuildTool.House)
        {
            if (currentPreview != null)
                currentPreview.SetActive(false);

            return;
        }

        HandlePreview();

        if (Input.GetMouseButtonDown(0))
        {
            PlaceObject();
        }
    }

    void HandlePreview()
    {
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer))
        {
            if (!currentPreview.activeSelf)
                currentPreview.SetActive(true);

            Vector3 raw = hit.point;

            float x = Mathf.Round(raw.x / gridSize) * gridSize;
            float z = Mathf.Round(raw.z / gridSize) * gridSize;

            Vector3 targetPos = new Vector3(x, raw.y, z);

            currentPreview.transform.position = Vector3.Lerp(
                currentPreview.transform.position,
                targetPos,
                Time.deltaTime * 20f
            );
        }
    }

    void PlaceObject()
    {
        if (currentPreview != null && currentPreview.activeSelf)
        {
            Instantiate(
                housePrefab,
                currentPreview.transform.position,
                Quaternion.identity
            );
        }
    }
}