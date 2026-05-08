using UnityEngine;

public class PlacementSystem : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private ToolManager toolManager;
    [SerializeField] private EditTransformHandler editHandler;
    [SerializeField] private ObjectPlacementHandler objectHandler;
    [SerializeField] private TerrainModifierHandler terrainHandler;
    [SerializeField] private SplinePlacementHandler splineHandler;

    [Header("General Settings")]
    [SerializeField] public LayerMask groundLayer;
    [SerializeField] public LayerMask buildableLayer;
    [SerializeField] public float gridSize = 1f;

    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
        if (editHandler) editHandler.Initialize(this);
        if (objectHandler) objectHandler.Initialize(this);
        if (terrainHandler) terrainHandler.Initialize(this);
        if (splineHandler) splineHandler.Initialize(this);
    }

    void Update()
    {
        if (toolManager == null) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (editHandler) editHandler.StopEditing();
            if (splineHandler) splineHandler.ResetSplines();
            if (terrainHandler) terrainHandler.SetBrushMode(0);
        }

        if (editHandler != null && editHandler.IsEditing)
        {
            editHandler.HandleEditUpdate(mainCam);
            return;
        }

        if (toolManager.currentTool == ToolManager.BuildTool.None)
        {
            HandleSelection();
        }

        UpdateToolLogic();
    }

    private void HandleSelection()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, buildableLayer))
            {
                GameObject target = hit.collider.gameObject;
                if (target.transform.parent != null) target = target.transform.parent.gameObject;

                if (target.CompareTag("TinyHouse") || target.CompareTag("Furniture") || target.CompareTag("Player"))
                {
                    if (editHandler) editHandler.StartEditing(target);
                }
            }
        }
    }

    private void UpdateToolLogic()
    {
        if (objectHandler) objectHandler.HideAllPreviews();

        switch (toolManager.currentTool)
        {
            case ToolManager.BuildTool.House:
                terrainHandler.SetBrushMode(0);
                objectHandler.HandleHousePlacement(mainCam, toolManager.houseIndex);
                break;
            case ToolManager.BuildTool.Furniture:
                terrainHandler.SetBrushMode(0);
                objectHandler.HandleMultiPlacement(mainCam, toolManager.furnitureIndex);
                break;
            case ToolManager.BuildTool.Nature:
                terrainHandler.SetBrushMode(0);
                objectHandler.HandleNatureSpline(mainCam, toolManager.natureIndex);
                break;
            case ToolManager.BuildTool.Wall:
                terrainHandler.SetBrushMode(0);
                splineHandler.HandleWallSpline(mainCam);
                break;

            case ToolManager.BuildTool.Road:
                terrainHandler.SetBrushMode(1);
                terrainHandler.HandleTerrainEditor(mainCam, editHandler);
                break;
            case ToolManager.BuildTool.Pond:
                terrainHandler.SetBrushMode(2);
                terrainHandler.HandleTerrainEditor(mainCam, editHandler);
                break;
            case ToolManager.BuildTool.Eraser:
                terrainHandler.SetBrushMode(3);
                terrainHandler.HandleTerrainEditor(mainCam, editHandler);
                break;

            case ToolManager.BuildTool.None:
                terrainHandler.SetBrushMode(0);
                break;
        }
    }

    public Vector3 SnapToGrid(Vector3 point)
    {
        return new Vector3(Mathf.Round(point.x / gridSize) * gridSize, point.y, Mathf.Round(point.z / gridSize) * gridSize);
    }
}