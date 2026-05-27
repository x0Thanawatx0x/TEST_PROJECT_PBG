using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class HouseGenerationController : MonoBehaviour
{
    [Header("Manager Reference")]
    [SerializeField] private ToolManager toolManager;        // 📥 ลากวัตถุที่มีสคริปต์ ToolManager มาใส่ช่องนี้

    [Header("Raycast & Terrain Settings")]
    [SerializeField] private LayerMask groundLayer;          // Layer ของพื้นดิน (เช่น Ground)
    [SerializeField] private Terrain targetTerrain;          // 📥 ลากวัตถุ Terrain ในฉากมาใส่ช่องนี้
    [SerializeField] private float terrainBufferZone = 1.5f; // ระยะเผื่อปาดดินรอบตัวบ้าน (เมตร) ดินสูงๆ จะได้ไม่ค้ำกำแพง

    [Header("Cozy Preview Style")]
    [SerializeField] private Material previewMaterial;       // Material สีฟ้าใสโปร่งแสงสำหรับตอนลากพรีวิว

    [Header("Prototype Modular Prefabs")]
    [SerializeField] private GameObject wallPrefab;          // แผ่นกำแพงกล่องสี่เหลี่ยม (Bottom Pivot)
    [SerializeField] private float wallWidth = 1f;           // ความกว้างของกำแพง (1 เมตร)

    [SerializeField] private GameObject floorPrefab;         // แผ่นพื้นสี่เหลี่ยม (Bottom Pivot)
    [SerializeField] private float floorSize = 1f;           // ขนาดแผ่นพื้น (1 เมตร)
    [SerializeField] private float floorThickness = 0.1f;    // ความหนาของแผ่นพื้น (สเกลแกน Y ของแผ่นพื้น)

    private Vector3 startPlacementPoint;
    private Vector3 endPlacementPoint;
    private float lockedBuildHeightY;                        // ตัวแปรล็อกแกน Y ตั้งแต่จุดเริ่มคลิกแรก

    private enum BuilderState { Idle, Dragging, PendingConfirmation }
    private BuilderState currentState = BuilderState.Idle;

    private Camera mainCamera;
    private List<GameObject> spawnedActualPieces = new List<GameObject>();
    private List<GameObject> currentPreviewPieces = new List<GameObject>();

    void Start()
    {
        mainCamera = Camera.main;
        if (targetTerrain == null) targetTerrain = Terrain.activeTerrain;
        if (toolManager == null) toolManager = FindFirstObjectByType<ToolManager>();
    }

    void Update()
    {
        // 🛡️ เช็กสถานะจาก ToolManager: ถ้าไม่ได้ถือเครื่องมือสร้างบ้าน (HouseGen) ให้หยุดทำงานและล้างพรีวิวเก่าออกทันที
        if (toolManager == null || toolManager.currentTool != ToolManager.BuildTool.HouseGen)
        {
            if (currentPreviewPieces.Count > 0)
            {
                ClearPreviewPieces();
                currentState = BuilderState.Idle;
            }
            return;
        }

        // ระบบจะลากสร้างบ้านได้ เฉพาะตอนที่ ToolManager อยู่ในโหมด HouseGen เท่านั้น
        HandleInputState();
        if (currentState == BuilderState.Dragging)
        {
            UpdatePreviewModular();
        }
    }

    private void HandleInputState()
    {
        // แกะโค้ดตรวจปุ่มเลข 9 ออกเรียบร้อย ปล่อยให้เคลียร์ Logic คลิกและ Enter
        if (currentState == BuilderState.Idle || currentState == BuilderState.PendingConfirmation)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                StartDragging();
            }
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame && currentState == BuilderState.Dragging)
        {
            if (currentPreviewPieces.Count > 0) currentState = BuilderState.PendingConfirmation;
            else currentState = BuilderState.Idle;
        }

        if (Keyboard.current.enterKey.wasPressedThisFrame && currentState == BuilderState.PendingConfirmation)
        {
            ConfirmBuildAndFlattenTerrain();
        }
    }

    private void StartDragging()
    {
        ClearPreviewPieces();
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer))
        {
            startPlacementPoint = SnapToGrid(hit.point, floorSize);
            lockedBuildHeightY = startPlacementPoint.y; // ล็อกความสูงจากจุดเริ่มลากทันที
            currentState = BuilderState.Dragging;
        }
    }

    private void UpdatePreviewModular()
    {
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer))
        {
            Vector3 currentSnapPoint = SnapToGrid(hit.point, floorSize);
            if (currentSnapPoint.x == endPlacementPoint.x && currentSnapPoint.z == endPlacementPoint.z && currentPreviewPieces.Count > 0) return;

            endPlacementPoint = new Vector3(currentSnapPoint.x, lockedBuildHeightY, currentSnapPoint.z);
            ClearPreviewPieces();

            float minX = Mathf.Min(startPlacementPoint.x, endPlacementPoint.x);
            float maxX = Mathf.Max(startPlacementPoint.x, endPlacementPoint.x);
            float minZ = Mathf.Min(startPlacementPoint.z, endPlacementPoint.z);
            float maxZ = Mathf.Max(startPlacementPoint.z, endPlacementPoint.z);

            int countX = Mathf.RoundToInt((maxX - minX) / floorSize);
            int countZ = Mathf.RoundToInt((maxZ - minZ) / floorSize);
            if (countX == 0) countX = 1;
            if (countZ == 0) countZ = 1;

            // 📐 ระดับความสูงร่วมคำนวณแบบ Bottom Pivot กำแพงต่อผิวบนพื้นแนบสนิท 100%
            float baseFloorY = lockedBuildHeightY + 0.02f;
            float baseWallY = baseFloorY + floorThickness;

            // วาดพื้นพรีวิวสีฟ้าใส
            for (int x = 0; x < countX; x++)
            {
                for (int z = 0; z < countZ; z++)
                {
                    float fX = minX + (x * floorSize) + (floorSize / 2f);
                    float fZ = minZ + (z * floorSize) + (floorSize / 2f);
                    SpawnPreviewPiece(floorPrefab, new Vector3(fX, baseFloorY, fZ), Quaternion.identity);
                }
            }

            // วาดกำแพงพรีวิวแกน X
            for (int i = 0; i < countX; i++)
            {
                float pX = minX + (i * wallWidth) + (wallWidth / 2f);
                SpawnPreviewPiece(wallPrefab, new Vector3(pX, baseWallY, minZ), Quaternion.Euler(0, 0, 0));
                SpawnPreviewPiece(wallPrefab, new Vector3(pX, baseWallY, maxZ), Quaternion.Euler(0, 180, 0));
            }

            // วาดกำแพงพรีวิวแกน Z
            for (int j = 0; j < countZ; j++)
            {
                float pZ = minZ + (j * wallWidth) + (wallWidth / 2f);
                SpawnPreviewPiece(wallPrefab, new Vector3(minX, baseWallY, pZ), Quaternion.Euler(0, 90, 0));
                SpawnPreviewPiece(wallPrefab, new Vector3(maxX, baseWallY, pZ), Quaternion.Euler(0, -90, 0));
            }
        }
    }

    private void SpawnPreviewPiece(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return;
        GameObject previewObj = Instantiate(prefab, position, rotation);
        if (previewObj.TryGetComponent<Collider>(out Collider col)) col.enabled = false;

        if (previewMaterial != null)
        {
            if (previewObj.TryGetComponent<Renderer>(out Renderer rend)) rend.material = previewMaterial;
            foreach (Renderer childRend in previewObj.GetComponentsInChildren<Renderer>())
            {
                childRend.material = previewMaterial;
            }
        }
        currentPreviewPieces.Add(previewObj);
    }

    private void ConfirmBuildAndFlattenTerrain()
    {
        float minX = Mathf.Min(startPlacementPoint.x, endPlacementPoint.x);
        float maxX = Mathf.Max(startPlacementPoint.x, endPlacementPoint.x);
        float minZ = Mathf.Min(startPlacementPoint.z, endPlacementPoint.z);
        float maxZ = Mathf.Max(startPlacementPoint.z, endPlacementPoint.z);

        int countX = Mathf.RoundToInt((maxX - minX) / floorSize);
        int countZ = Mathf.RoundToInt((maxZ - minZ) / floorSize);
        if (countX == 0) countX = 1;
        if (countZ == 0) countZ = 1;

        // 🚜 [ปาดถล่มภูเขาสูง] สั่งปาด Overwrite ค่าความสูงดินภูเขาชันๆ ให้ยุบฮวบราบเรียบเท่าตัวบ้าน
        if (targetTerrain != null)
        {
            FlattenTerrainAreaForce(minX, maxX, minZ, maxZ, lockedBuildHeightY);
        }

        float baseFloorY = lockedBuildHeightY + 0.02f;
        float baseWallY = baseFloorY + floorThickness;

        // เสกพื้นจริงสีทึบ
        for (int x = 0; x < countX; x++)
        {
            for (int z = 0; z < countZ; z++)
            {
                float fX = minX + (x * floorSize) + (floorSize / 2f);
                float fZ = minZ + (z * floorSize) + (floorSize / 2f);
                SpawnActualPiece(floorPrefab, new Vector3(fX, baseFloorY, fZ), Quaternion.identity);
            }
        }

        // เสกกำแพงจริงสีทึบ
        for (int i = 0; i < countX; i++)
        {
            float pX = minX + (i * wallWidth) + (wallWidth / 2f);
            SpawnActualPiece(wallPrefab, new Vector3(pX, baseWallY, minZ), Quaternion.Euler(0, 0, 0));
            SpawnActualPiece(wallPrefab, new Vector3(pX, baseWallY, maxZ), Quaternion.Euler(0, 180, 0));
        }

        for (int j = 0; j < countZ; j++)
        {
            float pZ = minZ + (j * wallWidth) + (wallWidth / 2f);
            SpawnActualPiece(wallPrefab, new Vector3(minX, baseWallY, pZ), Quaternion.Euler(0, 90, 0));
            SpawnActualPiece(wallPrefab, new Vector3(maxX, baseWallY, pZ), Quaternion.Euler(0, -90, 0));
        }

        ClearPreviewPieces();
        currentState = BuilderState.Idle;
    }

    private void FlattenTerrainAreaForce(float minX, float maxX, float minZ, float maxZ, float targetWorldHeight)
    {
        TerrainData terrainData = targetTerrain.terrainData;
        float targetNormalizedHeight = (targetWorldHeight - targetTerrain.transform.position.y) / terrainData.size.y;

        int startX = Mathf.FloorToInt((minZ - terrainBufferZone - targetTerrain.transform.position.z) / terrainData.size.z * terrainData.heightmapResolution);
        int endX = Mathf.CeilToInt((maxZ + terrainBufferZone - targetTerrain.transform.position.z) / terrainData.size.z * terrainData.heightmapResolution);
        int startY = Mathf.FloorToInt((minX - terrainBufferZone - targetTerrain.transform.position.x) / terrainData.size.x * terrainData.heightmapResolution);
        int endY = Mathf.CeilToInt((maxX + terrainBufferZone - targetTerrain.transform.position.x) / terrainData.size.x * terrainData.heightmapResolution);

        startX = Mathf.Clamp(startX, 0, terrainData.heightmapResolution);
        endX = Mathf.Clamp(endX, 0, terrainData.heightmapResolution);
        startY = Mathf.Clamp(startY, 0, terrainData.heightmapResolution);
        endY = Mathf.Clamp(endY, 0, terrainData.heightmapResolution);

        int width = endX - startX;
        int height = endY - startY;

        if (width <= 0 || height <= 0) return;

        float[,] heights = new float[height, width];
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++) heights[i, j] = targetNormalizedHeight;
        }

        terrainData.SetHeights(startX, startY, heights);
        targetTerrain.Flush();
    }

    private void SpawnActualPiece(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return;
        GameObject piece = Instantiate(prefab, position, rotation);
        spawnedActualPieces.Add(piece);
    }

    private Vector3 SnapToGrid(Vector3 position, float snapValue)
    {
        return new Vector3(
            Mathf.Round(position.x / snapValue) * snapValue,
            position.y,
            Mathf.Round(position.z / snapValue) * snapValue
        );
    }

    private void ClearPreviewPieces()
    {
        foreach (GameObject obj in currentPreviewPieces)
        {
            if (obj != null) Destroy(obj);
        }
        currentPreviewPieces.Clear();
    }
}