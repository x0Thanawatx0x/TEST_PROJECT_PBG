using UnityEngine;
using System.Collections.Generic;

public class PlacementSystem : MonoBehaviour
{
    [Header("General Settings")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask buildableLayer;
    [SerializeField] private float gridSize = 1f;
    [SerializeField] private ToolManager toolManager;

    [Header("Scaling & Rotation Gizmo Settings")]
    [SerializeField] private GameObject scalingGizmoPrefab;
    [SerializeField] public float gizmoVerticalOffset = 0.5f;
    [SerializeField] private float scaleSensitivity = 2f;
    [SerializeField] private float rotationSensitivity = 150f; // ✅ ปรับความเร็วการหมุนใน Inspector ได้

    [Header("Gizmo Size Control")]
    // ✅ ปรับขนาด Gizmo รวมได้จาก Inspector (เช่น 0.8 เล็กขยับเข้าใกล้บ้าน, 1.2 ใหญ่ขยับออก)
    [SerializeField] public float gizmoSizeMultiplier = 1.0f;

    private GameObject activeGizmo;
    private string currentDraggingAxis = "";

    private Transform axisX, axisY, axisZ, axisUniform, axisRotate; // ✅ เพิ่มตัวแปรแกนหมุน

    [Header("Edit Mode Settings")]
    [SerializeField] private Color highlightColor = new Color(1, 0.9f, 0, 0.5f);
    [SerializeField] private float liftHeight = 0.2f;
    private GameObject selectedObject = null;
    private bool isEditing = false;
    private Color originalColor;
    private Vector3 mouseOffset;
    private float lastGroundY;
    private Vector3 lastMousePos;

    [Header("House (Key 1)")]
    [SerializeField] private GameObject housePrefab;
    [SerializeField] private GameObject housePreview;

    [Header("Road Painting (Key 2)")]
    [SerializeField] private int roadLayerIndex = 1;
    [SerializeField] private int grassLayerIndex = 0;

    [Header("Furniture List (Key 3)")]
    [SerializeField] private List<GameObject> furniturePrefabs;
    [SerializeField] private List<GameObject> furniturePreviews;

    [Header("Wall Spline (Key 4)")]
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private GameObject pillarPrefab;
    private List<Vector3> wallPoints = new List<Vector3>();
    private bool isDrawingWall = false;

    [Header("Nature List (Key 5)")]
    [SerializeField] private List<GameObject> naturePrefabs;
    [SerializeField] private List<GameObject> naturePreviews;

    [Header("Pond & Brush Settings (Key 6)")]
    [SerializeField] private Terrain terrain;
    [SerializeField] private float brushSize = 2f;
    [SerializeField] private float minBrushSize = 0.5f;
    [SerializeField] private float maxBrushSize = 10f;
    [SerializeField] private float brushStep = 1.5f;
    [SerializeField] private float moldSpeed = 5f;
    [SerializeField] private GameObject pondPreview;

    [Header("Brush Timer Settings")]
    [SerializeField] private float brushInputDelay = 0.3f;
    private float brushTimer = 0f;
    private bool isAdjustingBrush = false;

    private Camera mainCam;
    private float[,] originalHeights;

    void Start()
    {
        mainCam = Camera.main;
        if (terrain != null) originalHeights = terrain.terrainData.GetHeights(0, 0, terrain.terrainData.heightmapResolution, terrain.terrainData.heightmapResolution);

        if (housePreview) housePreview = Instantiate(housePreview);
        if (pondPreview) pondPreview = Instantiate(pondPreview);

        if (scalingGizmoPrefab)
        {
            activeGizmo = Instantiate(scalingGizmoPrefab);
            activeGizmo.SetActive(false);

            axisX = activeGizmo.transform.Find("Axis_X");
            axisY = activeGizmo.transform.Find("Axis_Y");
            axisZ = activeGizmo.transform.Find("Axis_Z");
            axisUniform = activeGizmo.transform.Find("Axis_Uniform");
            axisRotate = activeGizmo.transform.Find("Axis_Rotate"); // ✅ ลิงก์แกนหมุนจาก Prefab
        }

        for (int i = 0; i < furniturePreviews.Count; i++) if (furniturePreviews[i]) furniturePreviews[i] = Instantiate(furniturePreviews[i]);
        for (int i = 0; i < naturePreviews.Count; i++) if (naturePreviews[i]) naturePreviews[i] = Instantiate(naturePreviews[i]);

        HideAllPreviews();
    }

    void Update()
    {
        if (toolManager == null) return;
        if (isEditing && selectedObject != null) { HandleGizmoInteraction(); return; }

        HandleBrushSizeButtons();
        HandleObjectSelection();
        HideAllPreviews();

        switch (toolManager.currentTool)
        {
            case ToolManager.BuildTool.House: HandleSinglePlacement(housePrefab, housePreview); break;
            case ToolManager.BuildTool.Road: HandleRoadPainting(); break;
            case ToolManager.BuildTool.Furniture: HandleMultiPlacement(furniturePrefabs, furniturePreviews, toolManager.furnitureIndex); break;
            case ToolManager.BuildTool.Wall: HandleWallSplineSystem(); break;
            case ToolManager.BuildTool.Nature: HandleMultiPlacement(naturePrefabs, naturePreviews, toolManager.natureIndex); break;
            case ToolManager.BuildTool.Pond: HandlePondSystem(); break;
            case ToolManager.BuildTool.Eraser: HandleEraser(); break;
        }
    }

    // ================= [ Edit Mode Logic ] =================

    void HandleObjectSelection()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, buildableLayer)) StartEditing(hit.collider.gameObject);
        }
    }

    void StartEditing(GameObject obj)
    {
        selectedObject = obj;
        isEditing = true;
        lastGroundY = selectedObject.transform.position.y;
        if (activeGizmo) { activeGizmo.SetActive(true); UpdateGizmoPosition(); }
        Renderer r = selectedObject.GetComponentInChildren<Renderer>();
        if (r != null) { originalColor = r.material.color; r.material.color = highlightColor; }
    }

    void UpdateGizmoPosition()
    {
        if (activeGizmo == null || selectedObject == null) return;

        // วางตำแหน่ง Gizmo และหมุนตาม Object (ยกเว้นตอนหมุนเอง จะได้ไม่สั่น)
        activeGizmo.transform.position = selectedObject.transform.position + Vector3.up * gizmoVerticalOffset;
        activeGizmo.transform.rotation = selectedObject.transform.rotation;

        // ✅ คำนวณขนาดจาก Scale ของ Object และคูณด้วย SizeMultiplier จาก Inspector
        Vector3 objScale = selectedObject.transform.localScale;
        float baseScale = (objScale.x + objScale.z) / 2f;
        activeGizmo.transform.localScale = Vector3.one * (baseScale * gizmoSizeMultiplier);
    }

    void HandleGizmoInteraction()
    {
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        if (Input.GetMouseButtonDown(0))
        {
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
            {
                if (hit.collider.name.Contains("Axis")) { currentDraggingAxis = hit.collider.name; lastMousePos = Input.mousePosition; }
                else if (hit.collider.gameObject == selectedObject) { currentDraggingAxis = "Move"; mouseOffset = selectedObject.transform.position - hit.point; mouseOffset.y = 0; }
                else StopEditing();
            }
        }
        if (Input.GetMouseButtonUp(0)) currentDraggingAxis = "";

        if (currentDraggingAxis != "")
        {
            Vector3 mouseDelta = Input.mousePosition - lastMousePos;

            if (currentDraggingAxis == "Move")
            {
                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer))
                {
                    lastGroundY = hit.point.y;
                    selectedObject.transform.position = Vector3.Lerp(selectedObject.transform.position, SnapToGrid(hit.point + mouseOffset) + Vector3.up * liftHeight, Time.deltaTime * 25f);
                }
            }
            else if (currentDraggingAxis == "Axis_Rotate") // ✅ ระบบหมุน (Rotate)
            {
                // ลากเมาส์ซ้าย-ขวาเพื่อหมุนวัตถุ
                float rotationAmount = mouseDelta.x * rotationSensitivity * Time.deltaTime;
                selectedObject.transform.Rotate(Vector3.up, -rotationAmount, Space.World);
            }
            else // ระบบปรับขนาด (Scaling)
            {
                float delta = (mouseDelta.x + mouseDelta.y) * scaleSensitivity * Time.deltaTime;
                if (currentDraggingAxis == "Axis_X") selectedObject.transform.localScale += new Vector3(delta, 0, 0);
                else if (currentDraggingAxis == "Axis_Y") selectedObject.transform.localScale += new Vector3(0, delta, 0);
                else if (currentDraggingAxis == "Axis_Z") selectedObject.transform.localScale += new Vector3(0, 0, delta);
                else if (currentDraggingAxis == "Axis_Uniform") selectedObject.transform.localScale += Vector3.one * delta;

                Vector3 s = selectedObject.transform.localScale;
                selectedObject.transform.localScale = new Vector3(Mathf.Max(s.x, 0.1f), Mathf.Max(s.y, 0.1f), Mathf.Max(s.z, 0.1f));
            }

            UpdateGizmoPosition();
            lastMousePos = Input.mousePosition;
        }
        if (Input.GetMouseButtonDown(1)) StopEditing();
    }

    void StopEditing()
    {
        if (selectedObject != null)
        {
            selectedObject.transform.position = new Vector3(selectedObject.transform.position.x, lastGroundY, selectedObject.transform.position.z);
            Renderer r = selectedObject.GetComponentInChildren<Renderer>();
            if (r != null) r.material.color = originalColor;
        }
        currentDraggingAxis = ""; if (activeGizmo) activeGizmo.SetActive(false); selectedObject = null; isEditing = false;
    }

    // ================= [ ส่วน Logic อื่นๆ คงเดิม ] =================
    void HandleBrushSizeButtons() { if (Input.GetKey(KeyCode.Equals) || Input.GetKey(KeyCode.KeypadPlus)) { brushSize += brushStep * Time.deltaTime * 5f; ResetBrushTimer(); } if (Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.KeypadMinus)) { brushSize -= brushStep * Time.deltaTime * 5f; ResetBrushTimer(); } brushSize = Mathf.Clamp(brushSize, minBrushSize, maxBrushSize); if (isAdjustingBrush) { brushTimer -= Time.deltaTime; if (brushTimer <= 0) isAdjustingBrush = false; } }
    void ResetBrushTimer() { isAdjustingBrush = true; brushTimer = brushInputDelay; }
    void HandleRoadPainting() { Ray ray = mainCam.ScreenPointToRay(Input.mousePosition); if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer)) { if (pondPreview) { pondPreview.SetActive(true); pondPreview.transform.position = hit.point + Vector3.up * 0.1f; pondPreview.transform.localScale = new Vector3(brushSize * 2, 1, brushSize * 2); } if (Input.GetMouseButton(0)) PaintTerrain(hit.point, roadLayerIndex); } }
    void PaintTerrain(Vector3 worldPos, int layerIdx) { if (!terrain) return; TerrainData td = terrain.terrainData; int mapX = Mathf.RoundToInt((worldPos.x - terrain.transform.position.x) / td.size.x * td.alphamapWidth); int mapZ = Mathf.RoundToInt((worldPos.z - terrain.transform.position.z) / td.size.z * td.alphamapHeight); int radius = Mathf.RoundToInt(brushSize); int startX = Mathf.Clamp(mapX - radius, 0, td.alphamapWidth - 1); int startZ = Mathf.Clamp(mapZ - radius, 0, td.alphamapHeight - 1); int width = Mathf.Clamp(radius * 2, 1, td.alphamapWidth - startX); int height = Mathf.Clamp(radius * 2, 1, td.alphamapHeight - startZ); float[,,] maps = td.GetAlphamaps(startX, startZ, width, height); for (int y = 0; y < height; y++) for (int x = 0; x < width; x++) for (int k = 0; k < td.alphamapLayers; k++) maps[y, x, k] = (k == layerIdx) ? 1f : 0f; td.SetAlphamaps(startX, startZ, maps); }
    void HandlePondSystem() { Ray ray = mainCam.ScreenPointToRay(Input.mousePosition); if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer)) { if (pondPreview) { pondPreview.SetActive(true); pondPreview.transform.position = hit.point + Vector3.up * 0.1f; pondPreview.transform.localScale = new Vector3(brushSize * 2, 1, brushSize * 2); } if (Input.GetMouseButton(0)) ModifyTerrain(hit.point); } }
    void ModifyTerrain(Vector3 point) { if (!terrain) return; TerrainData td = terrain.terrainData; int mapX = Mathf.RoundToInt((point.x - terrain.transform.position.x) / td.size.x * td.heightmapResolution); int mapZ = Mathf.RoundToInt((point.z - terrain.transform.position.z) / td.size.z * td.heightmapResolution); int r = (int)brushSize; int startX = Mathf.Clamp(mapX - r, 0, td.heightmapResolution - 1); int startZ = Mathf.Clamp(mapZ - r, 0, td.heightmapResolution - 1); int w = Mathf.Clamp(r * 2, 1, td.heightmapResolution - startX); int h = Mathf.Clamp(r * 2, 1, td.heightmapResolution - startZ); float[,] heights = td.GetHeights(startX, startZ, w, h); for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) heights[y, x] -= moldSpeed * 0.01f * Time.deltaTime; td.SetHeights(startX, startZ, heights); }
    void HandleWallSplineSystem() { Ray ray = mainCam.ScreenPointToRay(Input.mousePosition); if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer)) { Vector3 currentSnapPos = SnapToGrid(hit.point); if (pondPreview) { pondPreview.SetActive(true); pondPreview.transform.position = currentSnapPos + Vector3.up * 0.1f; MeshRenderer ren = pondPreview.GetComponentInChildren<MeshRenderer>(); if (ren != null) ren.material.color = IsOccupied(currentSnapPos) ? Color.red : Color.green; } if (Input.GetMouseButtonDown(0) && !IsOccupied(currentSnapPos)) { isDrawingWall = true; wallPoints.Clear(); wallPoints.Add(currentSnapPos); SpawnPillar(currentSnapPos); } if (isDrawingWall && Input.GetMouseButton(0)) { Vector3 lastPoint = wallPoints[wallPoints.Count - 1]; if (Vector3.Distance(lastPoint, currentSnapPos) >= gridSize && !IsOccupied(currentSnapPos)) { BuildWallSegment(lastPoint, currentSnapPos); wallPoints.Add(currentSnapPos); } } if (Input.GetMouseButtonUp(0)) isDrawingWall = false; } }
    bool IsOccupied(Vector3 pos) { return Physics.CheckSphere(pos, 0.2f, buildableLayer); }
    void BuildWallSegment(Vector3 start, Vector3 end) { Vector3 dir = end - start; if (dir != Vector3.zero) { Quaternion wallRot = Quaternion.LookRotation(dir); GameObject wall = Instantiate(wallPrefab, start + (dir / 2f), wallRot); Vector3 scale = wall.transform.localScale; scale.z = dir.magnitude; wall.transform.localScale = scale; wall.layer = LayerMaskToLayer(buildableLayer); if (!wall.GetComponent<Collider>()) wall.AddComponent<MeshCollider>(); } SpawnPillar(end); }
    void SpawnPillar(Vector3 pos) { if (pillarPrefab == null) return; GameObject pillar = Instantiate(pillarPrefab, pos, Quaternion.identity); pillar.layer = LayerMaskToLayer(buildableLayer); if (!pillar.GetComponent<Collider>()) pillar.AddComponent<BoxCollider>(); }
    void HandleSinglePlacement(GameObject prefab, GameObject preview) { if (!prefab || !preview) return; Ray ray = mainCam.ScreenPointToRay(Input.mousePosition); if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer)) { preview.SetActive(true); Vector3 pos = SnapToGrid(hit.point); preview.transform.position = pos; if (Input.GetMouseButtonDown(0)) SpawnObject(prefab, pos); } }
    void HandleMultiPlacement(List<GameObject> prefabs, List<GameObject> previews, int subIndex) { if (prefabs.Count == 0 || previews.Count == 0) return; int index = subIndex % prefabs.Count; Ray ray = mainCam.ScreenPointToRay(Input.mousePosition); if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer)) { GameObject preview = previews[index]; if (preview) { preview.SetActive(true); Vector3 pos = SnapToGrid(hit.point); preview.transform.position = pos; if (Input.GetMouseButtonDown(0)) SpawnObject(prefabs[index], pos); } } }
    void SpawnObject(GameObject prefab, Vector3 pos) { GameObject newObj = Instantiate(prefab, pos, Quaternion.identity); newObj.layer = LayerMaskToLayer(buildableLayer); if (!newObj.GetComponent<Collider>()) newObj.AddComponent<BoxCollider>(); }
    void HandleEraser() { Ray ray = mainCam.ScreenPointToRay(Input.mousePosition); RaycastHit hit; if (Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayer | buildableLayer)) { if (pondPreview) { pondPreview.SetActive(true); pondPreview.transform.position = hit.point + Vector3.up * 0.1f; pondPreview.transform.localScale = new Vector3(brushSize * 2, 1, brushSize * 2); } if (Input.GetMouseButton(0)) { Collider[] hits = Physics.OverlapSphere(hit.point, brushSize, buildableLayer); foreach (Collider c in hits) Destroy(c.gameObject); FlattenTerrain(hit.point); PaintTerrain(hit.point, grassLayerIndex); } } }
    void FlattenTerrain(Vector3 point) { if (!terrain || originalHeights == null) return; TerrainData td = terrain.terrainData; int mapX = Mathf.RoundToInt((point.x - terrain.transform.position.x) / td.size.x * td.heightmapResolution); int mapZ = Mathf.RoundToInt((point.z - terrain.transform.position.z) / td.size.z * td.heightmapResolution); int r = (int)brushSize; int startX = Mathf.Clamp(mapX - r, 0, td.heightmapResolution - 1); int startZ = Mathf.Clamp(mapZ - r, 0, td.heightmapResolution - 1); int w = Mathf.Clamp(r * 2, 1, td.heightmapResolution - startX); int h = Mathf.Clamp(r * 2, 1, td.heightmapResolution - startZ); float[,] heights = td.GetHeights(startX, startZ, w, h); for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) heights[y, x] = originalHeights[startZ + y, startX + x]; td.SetHeights(startX, startZ, heights); }
    Vector3 SnapToGrid(Vector3 point) { return new Vector3(Mathf.Round(point.x / gridSize) * gridSize, point.y, Mathf.Round(point.z / gridSize) * gridSize); }
    void HideAllPreviews() { if (housePreview) housePreview.SetActive(false); if (pondPreview) pondPreview.SetActive(false); foreach (var p in furniturePreviews) if (p) p.SetActive(false); foreach (var p in naturePreviews) if (p) p.SetActive(false); }
    private int LayerMaskToLayer(LayerMask mask) { int val = mask.value; for (int i = 0; i < 32; i++) if (((val >> i) & 1) == 1) return i; return 0; }
}