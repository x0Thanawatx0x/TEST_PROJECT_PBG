using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// ระบบเจนบ้านสไตล์ Cozy Sandbox สำหรับ Unity 6 (Center Pivot 2m)
/// เวอร์ชันเสร็จสมบูรณ์: เพิ่มระบบล็อกขนาดขั้นต่ำของตัวบ้าน (Minimum Size Clamping)
/// </summary>
public class HouseGenerationController : MonoBehaviour
{
    [Header("Manager Reference")]
    [SerializeField] private ToolManager toolManager;

    [Header("Raycast & Terrain Settings")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask buildableLayer;
    [SerializeField] private Terrain targetTerrain;
    [SerializeField] private float terrainBufferZone = 1.5f;

    [Header("Cozy Preview Style")]
    [SerializeField] private Material previewMaterial;

    [Header("🧱 ระบบโครงสร้างกำแพงโมดูลาร์ (Center Pivot 2m)")]
    [Tooltip("เสาค้ำมุมห้อง - ใช้สำหรับปักบังรอยต่อฉากมุมห้อง")]
    [SerializeField] private GameObject cornerPillarPrefab;

    [Tooltip("โมเดลกำแพงทึบกว้าง 2 เมตร (Pivot อยู่ตรงกลางแผ่นพอดีเป๊ะ)")]
    [SerializeField] private GameObject wallPrefab;

    [Tooltip("โมเดลกำแพงหน้าต่างกว้าง 2 เมตร (Pivot อยู่ตรงกลางแผ่นพอดีเป๊ะ)")]
    [SerializeField] private GameObject windowWallPrefab;

    [Range(0f, 100f)]
    [SerializeField] private float windowSpawnChance = 30f;

    [Tooltip("ความกว้างจริงของโมเดลกำแพง (ใส่ค่า 2)")]
    [SerializeField] private float wallWidth = 2f;

    [Header("🚪 ระบบผนังเจาะช่องประตู")]
    [Tooltip("โมเดลกำแพงประตูกว้าง 2 เมตร (Pivot อยู่ตรงกลางแผ่นพอดีเป๊ะ)")]
    [SerializeField] private GameObject doorWallFramePrefab;

    [Tooltip("บานประตูเดี่ยว")]
    [SerializeField] private GameObject standaloneDoorPrefab;

    [Header("📐 ระบบพื้นบ้านและการจำกัดสเกลขั้นต่ำ")]
    [Tooltip("โมเดลแผ่นพื้น (Pivot อยู่ตรงกลางแผ่น)")]
    [SerializeField] private GameObject floorPrefab;

    [Tooltip("ขนาดของโมเดลพื้นจริงในเอนจิ้น (ใส่ค่า 2)")]
    [SerializeField] private float floorSize = 2f;

    [SerializeField] private float floorThickness = 0.1f;

    [Tooltip("ขนาดขั้นต่ำของตัวบ้านในหน่วยเมตร (ปิ๊บใส่เลข 7 ได้เลย ระบบจะปัดเศษเข้าล็อกตารางพื้นให้อัตโนมัติ)")]
    [SerializeField] private float minBuildSize = 7f;

    private Vector3 startPlacementPoint;
    private Vector3 endPlacementPoint;
    private float lockedBuildHeightY;

    private enum BuilderState { Idle, Dragging, PendingConfirmation }
    private BuilderState currentState = BuilderState.Idle;

    private Camera mainCamera;
    private List<GameObject> spawnedActualPieces = new List<GameObject>();
    private List<GameObject> currentPreviewPieces = new List<GameObject>();

    private void Start()
    {
        mainCamera = Camera.main;
        if (targetTerrain == null) targetTerrain = Terrain.activeTerrain;
        if (toolManager == null) toolManager = FindFirstObjectByType<ToolManager>();
    }

    private void Update()
    {
        if (toolManager == null) return;

        if (toolManager.currentTool == ToolManager.BuildTool.HouseGen)
        {
            HandleInputState();
            if (currentState == BuilderState.Dragging) UpdatePreviewModular();
        }
        else
        {
            ClearPreviewPieces();
            currentState = BuilderState.Idle;
        }
    }

    private void HandleInputState()
    {
        if (currentState == BuilderState.Idle || currentState == BuilderState.PendingConfirmation)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame) StartDragging();
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
            lockedBuildHeightY = startPlacementPoint.y;
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

            // คำนวณจำนวนแผ่นพื้นตามระยะเมาส์จริง
            int floorCountX = Mathf.RoundToInt((maxX - minX) / floorSize);
            int floorCountZ = Mathf.RoundToInt((maxZ - minZ) / floorSize);

            // 🎯 [ระบบดักจับขนาดขั้นต่ำ] แปลงค่าเมตรขั้นต่ำ (minBuildSize) ให้เป็นจำนวนบล็อกพื้นขั้นต่ำ
            int minFloorCount = Mathf.Max(1, Mathf.RoundToInt(minBuildSize / floorSize));

            // บังคับล็อกว่าจำนวนแผ่นพื้นในแต่ละแกนห้ามต่ำกว่าค่าที่กำหนด
            if (floorCountX < minFloorCount) floorCountX = minFloorCount;
            if (floorCountZ < minFloorCount) floorCountZ = minFloorCount;

            // อัปเดตพิกัดปลายสายของขอบเขตบ้านให้กางออกตามขนาดขั้นต่ำจริง ดินและกำแพงจะได้ไม่หดสเกล
            maxX = minX + (floorCountX * floorSize);
            maxZ = minZ + (floorCountZ * floorSize);

            int wallCountX = Mathf.RoundToInt((maxX - minX) / wallWidth);
            int wallCountZ = Mathf.RoundToInt((maxZ - minZ) / wallWidth);
            if (wallCountX == 0) wallCountX = 1;
            if (wallCountZ == 0) wallCountZ = 1;

            float baseFloorY = lockedBuildHeightY + 0.05f;
            float baseWallY = baseFloorY + floorThickness;

            // 1. พรีวิวพื้น
            for (int x = 0; x < floorCountX; x++)
            {
                for (int z = 0; z < floorCountZ; z++)
                {
                    float fX = minX + (x * floorSize) + (floorSize / 2f);
                    float fZ = minZ + (z * floorSize) + (floorSize / 2f);
                    SpawnPreviewPiece(floorPrefab, new Vector3(fX, baseFloorY, fZ), Quaternion.identity);
                }
            }

            int doorIndexX = wallCountX / 2;
            float halfWall = wallWidth / 2f;

            // 2. แกน X: วางกำแพงแนวหน้าบ้าน-หลังบ้าน
            for (int i = 0; i < wallCountX; i++)
            {
                float pX = minX + (i * wallWidth) + halfWall;

                if (i == doorIndexX && doorWallFramePrefab != null)
                {
                    SpawnPreviewPiece(doorWallFramePrefab, new Vector3(pX, baseWallY, minZ), Quaternion.Euler(0, 0, 0));
                }
                else
                {
                    SpawnPreviewPiece(wallPrefab, new Vector3(pX, baseWallY, minZ), Quaternion.Euler(0, 0, 0));
                }

                SpawnPreviewPiece(wallPrefab, new Vector3(pX, baseWallY, maxZ), Quaternion.Euler(0, 180, 0));
            }

            // 3. แกน Z: วางกำแพงแนวซ้าย-ขวา
            for (int j = 0; j < wallCountZ; j++)
            {
                float pZ = minZ + (j * wallWidth) + halfWall;
                SpawnPreviewPiece(wallPrefab, new Vector3(minX, baseWallY, pZ), Quaternion.Euler(0, 90, 0));
                SpawnPreviewPiece(wallPrefab, new Vector3(maxX, baseWallY, pZ), Quaternion.Euler(0, -90, 0));
            }
        }
    }

    private void ConfirmBuildAndFlattenTerrain()
    {
        float minX = Mathf.Min(startPlacementPoint.x, endPlacementPoint.x);
        float maxX = Mathf.Max(startPlacementPoint.x, endPlacementPoint.x);
        float minZ = Mathf.Min(startPlacementPoint.z, endPlacementPoint.z);
        float maxZ = Mathf.Max(startPlacementPoint.z, endPlacementPoint.z);

        int floorCountX = Mathf.RoundToInt((maxX - minX) / floorSize);
        int floorCountZ = Mathf.RoundToInt((maxZ - minZ) / floorSize);

        // 🎯 ดักจับขนาดขั้นต่ำตอนกดคลิกสร้างจริงเช่นเดียวกับระบบ Preview
        int minFloorCount = Mathf.Max(1, Mathf.RoundToInt(minBuildSize / floorSize));
        if (floorCountX < minFloorCount) floorCountX = minFloorCount;
        if (floorCountZ < minFloorCount) floorCountZ = minFloorCount;

        maxX = minX + (floorCountX * floorSize);
        maxZ = minZ + (floorCountZ * floorSize);

        int wallCountX = Mathf.RoundToInt((maxX - minX) / wallWidth);
        int wallCountZ = Mathf.RoundToInt((maxZ - minZ) / wallWidth);
        if (wallCountX == 0) wallCountX = 1;
        if (wallCountZ == 0) wallCountZ = 1;

        if (targetTerrain != null) FlattenTerrainAreaForce(minX, maxX, minZ, maxZ, lockedBuildHeightY);

        float baseFloorY = lockedBuildHeightY + 0.05f;
        float baseWallY = baseFloorY + floorThickness;

        // 1. สร้างพื้นจริง
        for (int x = 0; x < floorCountX; x++)
        {
            for (int z = 0; z < floorCountZ; z++)
            {
                float fX = minX + (x * floorSize) + (floorSize / 2f);
                float fZ = minZ + (z * floorSize) + (floorSize / 2f);
                GameObject floorObj = Instantiate(floorPrefab, new Vector3(fX, baseFloorY, fZ), Quaternion.identity);
                SetLayerRecursively(floorObj, Mathf.RoundToInt(Mathf.Log(buildableLayer.value, 2)));
                spawnedActualPieces.Add(floorObj);
            }
        }

        // 2. เสกเสาค้ำมุม 4 ด้าน
        SpawnActualPillar(new Vector3(minX, baseWallY, minZ));
        SpawnActualPillar(new Vector3(maxX, baseWallY, minZ));
        SpawnActualPillar(new Vector3(minX, baseWallY, maxZ));
        SpawnActualPillar(new Vector3(maxX, baseWallY, maxZ));

        int doorIndexX = wallCountX / 2;
        float halfWall = wallWidth / 2f;

        // 3. สร้างกำแพงจริงแกน X
        for (int i = 0; i < wallCountX; i++)
        {
            float pX = minX + (i * wallWidth) + halfWall;

            if (i == doorIndexX && doorWallFramePrefab != null)
            {
                GameObject frameObj = Instantiate(doorWallFramePrefab, new Vector3(pX, baseWallY, minZ), Quaternion.Euler(0, 0, 0));
                spawnedActualPieces.Add(frameObj);

                if (standaloneDoorPrefab != null)
                {
                    Instantiate(standaloneDoorPrefab, new Vector3(pX, baseWallY, minZ), Quaternion.Euler(0, 0, 0), frameObj.transform);
                }
            }
            else
            {
                SpawnProceduralWallPiece(new Vector3(pX, baseWallY, minZ), Quaternion.Euler(0, 0, 0));
            }

            SpawnProceduralWallPiece(new Vector3(pX, baseWallY, maxZ), Quaternion.Euler(0, 180, 0));
        }

        // 4. สร้างกำแพงจริงแกน Z
        for (int j = 0; j < wallCountZ; j++)
        {
            float pZ = minZ + (j * wallWidth) + halfWall;
            SpawnProceduralWallPiece(new Vector3(minX, baseWallY, pZ), Quaternion.Euler(0, 90, 0));
            SpawnProceduralWallPiece(new Vector3(maxX, baseWallY, pZ), Quaternion.Euler(0, -90, 0));
        }

        ClearPreviewPieces();
        currentState = BuilderState.Idle;
    }

    private void SpawnProceduralWallPiece(Vector3 position, Quaternion rotation)
    {
        GameObject wallMeshToSpawn = wallPrefab;
        if (windowWallPrefab != null && Random.Range(0f, 100f) <= windowSpawnChance)
        {
            wallMeshToSpawn = windowWallPrefab;
        }
        GameObject spawnedWallObj = Instantiate(wallMeshToSpawn, position, rotation);
        spawnedActualPieces.Add(spawnedWallObj);
    }

    private void SpawnActualPillar(Vector3 position)
    {
        if (cornerPillarPrefab == null) return;
        GameObject pillarObj = Instantiate(cornerPillarPrefab, position, Quaternion.identity);
        spawnedActualPieces.Add(pillarObj);
    }

    private void SpawnPreviewPiece(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return;
        GameObject previewObj = Instantiate(prefab, position, rotation);
        if (previewObj.TryGetComponent<Collider>(out Collider col)) col.enabled = false;
        if (previewMaterial != null)
        {
            foreach (Renderer childRend in previewObj.GetComponentsInChildren<Renderer>()) childRend.material = previewMaterial;
        }
        currentPreviewPieces.Add(previewObj);
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

        int width = endX - startX; int height = endY - startY;
        if (width <= 0 || height <= 0) return;

        float[,] heights = new float[height, width];
        for (int i = 0; i < height; i++)
            for (int j = 0; j < width; j++) heights[i, j] = targetNormalizedHeight;

        terrainData.SetHeights(startX, startY, heights);
        targetTerrain.Flush();
    }

    private Vector3 SnapToGrid(Vector3 position, float snapValue)
    {
        return new Vector3(Mathf.Round(position.x / snapValue) * snapValue, position.y, Mathf.Round(position.z / snapValue) * snapValue);
    }

    private void ClearPreviewPieces()
    {
        foreach (GameObject obj in currentPreviewPieces) if (obj != null) Destroy(obj);
        currentPreviewPieces.Clear();
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        obj.layer = newLayer;
        foreach (Transform child in obj.transform) SetLayerRecursively(child.gameObject, newLayer);
    }

    public void BuildHouseFromExternalAPI(Vector3 simulatedStartPoint, Vector3 simulatedEndPoint)
    {
        this.startPlacementPoint = SnapToGrid(simulatedStartPoint, floorSize);
        this.endPlacementPoint = SnapToGrid(simulatedEndPoint, floorSize);
        this.lockedBuildHeightY = startPlacementPoint.y;
        ConfirmBuildAndFlattenTerrain();
    }
}