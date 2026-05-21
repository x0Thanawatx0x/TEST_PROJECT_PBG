// ==============================
// PlacementSystem.cs  (FIXED)
// ==============================

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

        // ---- ESC ----
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (editHandler) editHandler.StopEditing();
            if (splineHandler) splineHandler.ResetSplines();
            if (terrainHandler) terrainHandler.SetBrushMode(0);
        }

        // ---- ถ้ากำลัง edit อยู่ → ส่งทุก input ให้ editHandler ----
        if (editHandler != null && editHandler.IsEditing)
        {
            editHandler.HandleEditUpdate(mainCam);
            return;
        }

        // ---- ไม่ได้ edit — mode = None → รับ selection ----
        if (toolManager.currentTool == ToolManager.BuildTool.None)
        {
            HandleSelection();
        }

        UpdateToolLogic();
    }

    private void HandleSelection()
    {
        // FIX: early return ถ้าไม่ได้กดคลิก
        if (!Input.GetMouseButtonDown(0))
            return;

        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);

        // RaycastAll ไม่กรอง layer เพื่อให้โดน WallLayer ด้วย
        RaycastHit[] hits = Physics.RaycastAll(ray, 500f);
        System.Array.Sort(
            hits,
            (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            GameObject clicked = hit.collider.gameObject;

            // pillar (tag = "Wall") → StartEditing โดยตรง
            if (clicked.CompareTag("Wall"))
            {
                if (editHandler)
                    editHandler.StartEditing(clicked);
                return;
            }

            // object ทั่วไป — ขึ้นหา parent ก่อน
            GameObject target = clicked;
            if (target.transform.parent != null)
                target = target.transform.parent.gameObject;

            if (target.CompareTag("TinyHouse") ||
                target.CompareTag("Furniture") ||
                target.CompareTag("Player"))
            {
                if (editHandler)
                    editHandler.StartEditing(target);
                return;
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
                objectHandler.HandleHousePlacement(
                    mainCam,
                    toolManager.houseIndex);
                break;

            case ToolManager.BuildTool.Furniture:
                terrainHandler.SetBrushMode(0);
                objectHandler.HandleMultiPlacement(
                    mainCam,
                    toolManager.furnitureIndex);
                break;

            case ToolManager.BuildTool.Nature:
                terrainHandler.SetBrushMode(0);
                objectHandler.HandleNatureSpline(
                    mainCam,
                    toolManager.natureIndex);
                break;

            case ToolManager.BuildTool.Wall:
                terrainHandler.SetBrushMode(0);
                splineHandler.HandleWallSpline(mainCam);
                break;

            case ToolManager.BuildTool.Road:
                terrainHandler.SetBrushMode(1);
                terrainHandler.HandleTerrainEditor(
                    mainCam, editHandler);
                break;

            case ToolManager.BuildTool.Pond:
                terrainHandler.SetBrushMode(2);
                terrainHandler.HandleTerrainEditor(
                    mainCam, editHandler);
                break;

            case ToolManager.BuildTool.Eraser:
                terrainHandler.SetBrushMode(3);
                terrainHandler.HandleTerrainEditor(
                    mainCam, editHandler);
                break;

            case ToolManager.BuildTool.Mountain:
                terrainHandler.SetBrushMode(4);
                terrainHandler.HandleTerrainEditor(
                    mainCam, editHandler);
                break;

            case ToolManager.BuildTool.None:
                terrainHandler.SetBrushMode(0);
                break;
        }
    }

    public Vector3 SnapToGrid(Vector3 point)
    {
        return new Vector3(
            Mathf.Round(point.x / gridSize) * gridSize,
            point.y,
            Mathf.Round(point.z / gridSize) * gridSize);
    }
}