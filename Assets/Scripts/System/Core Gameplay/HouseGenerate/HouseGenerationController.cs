using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public enum FurnitureType
{
    Bed,
    Wardrobe,
    Desk,
    Decoration
}

[System.Serializable]
public class FurnitureItem
{
    public string furnitureName;
    public FurnitureType furnitureType;
    public GameObject prefab;
    [Range(0, 10)]
    public int spawnCount = 1;

    // ✨ พระเอกใหม่สำหรับรอบนี้ครับปิ๊บ! เอาไว้กำหนดโอกาสเกิด (สุ่มว่าจำเป็นต้องมีครบไหม)
    [Header("🎲 ระบบสุ่มโอกาสเกิด")]
    [Range(0f, 100f)]
    public float spawnChance = 100f; // ตั้งค่า 100% คือต้องเกิดแน่นอน, ถ้าน้อยกว่านั้นคือสุ่มลุ้นเอาครับ

    [Header("📐 เงื่อนไขขนาดห้องขั้นต่ำ (เมตร)")]
    public float minRoomWidthRequired = 3f;
    public float minRoomLengthRequired = 3f;
}

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

    [Header("Prototype Modular Prefabs")]
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private float wallWidth = 1f;
    [SerializeField] private GameObject floorPrefab;
    [SerializeField] private float floorSize = 1f;
    [SerializeField] private float floorThickness = 0.1f;

    [Header("🤖 ระบบ Dynamic AI จัดห้อง (ฉบับสุ่มโอกาสเกิด ไม่จำเป็นต้องมีครบ 100%)")]
    [SerializeField] private List<FurnitureItem> autoFurnitureList = new List<FurnitureItem>();

    private Vector3 startPlacementPoint;
    private Vector3 endPlacementPoint;
    private float lockedBuildHeightY;

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

            int countX = Mathf.RoundToInt((maxX - minX) / floorSize);
            int countZ = Mathf.RoundToInt((maxZ - minZ) / floorSize);
            if (countX == 0) countX = 1;
            if (countZ == 0) countZ = 1;

            float baseFloorY = lockedBuildHeightY + 0.02f;
            float baseWallY = baseFloorY + floorThickness;

            for (int x = 0; x < countX; x++)
            {
                for (int z = 0; z < countZ; z++)
                {
                    float fX = minX + (x * floorSize) + (floorSize / 2f);
                    float fZ = minZ + (z * floorSize) + (floorSize / 2f);
                    SpawnPreviewPiece(floorPrefab, new Vector3(fX, baseFloorY, fZ), Quaternion.identity);
                }
            }

            for (int i = 0; i < countX; i++)
            {
                float pX = minX + (i * wallWidth) + (wallWidth / 2f);
                SpawnPreviewPiece(wallPrefab, new Vector3(pX, baseWallY, minZ), Quaternion.Euler(0, 0, 0));
                SpawnPreviewPiece(wallPrefab, new Vector3(pX, baseWallY, maxZ), Quaternion.Euler(0, 180, 0));
            }

            for (int j = 0; j < countZ; j++)
            {
                float pZ = minZ + (j * wallWidth) + (wallWidth / 2f);
                SpawnPreviewPiece(wallPrefab, new Vector3(minX, baseWallY, pZ), Quaternion.Euler(0, 90, 0));
                SpawnPreviewPiece(wallPrefab, new Vector3(maxX, baseWallY, pZ), Quaternion.Euler(0, -90, 0));
            }
        }
    }

    // --- 🤖 ฟังก์ชัน Dynamic AI: เพิ่มตรรกะสุ่มทอยเต๋า Probability เพื่อเช็กว่าไอเทมชิ้นนั้นควรเกิดไหม ---
    private void AutoDecorateInteriorDynamic(float minX, float maxX, float minZ, float maxZ, float baseFloorY)
    {
        float roomWidth = maxX - minX;
        float roomLength = maxZ - minZ;
        float spawnY = baseFloorY + floorThickness;

        List<Vector3> availableSlots = new List<Vector3>();
        for (float x = minX + 1f; x <= maxX - 1f; x += 1f)
        {
            for (float z = minZ + 1f; z <= maxZ - 1f; z += 1f)
            {
                availableSlots.Add(new Vector3(x, spawnY, z));
            }
        }

        if (availableSlots.Count == 0) return;

        HashSet<Vector3> occupiedSlots = new HashSet<Vector3>();

        foreach (FurnitureItem item in autoFurnitureList)
        {
            if (item.prefab == null || item.spawnCount <= 0) continue;

            if (roomWidth < item.minRoomWidthRequired || roomLength < item.minRoomLengthRequired) continue;

            // 🎲 🛠️ [จุดแก้ไขสำคัญ: ทอยลูกเต๋าวัดดวง] 
            // สุ่มตัวเลข 0-100 ขึ้นมา ถ้าเลขที่สุ่มได้ดัน "มากกว่า" ค่าสเปคที่ปิ๊บตั้งไว้ AI จะสั่งข้ามไอเทมประเภทนี้ทันที!
            float rollDice = Random.Range(0f, 100f);
            if (rollDice > item.spawnChance)
            {
                Debug.Log($"🎲 AI ทอยเต๋าได้ {rollDice:F1} (โอกาสเกิดจริง {item.spawnChance}%) -> สั่งข้ามไม่เสก {item.furnitureName} ในรอบนี้");
                continue; // กระโดดข้ามไอเทมชิ้นนี้ไปเลย ไม่จำเป็นต้องมีครบทุกชิ้นแล้วครับปิ๊บ!
            }

            for (int i = 0; i < item.spawnCount; i++)
            {
                Vector3 bestSlot = Vector3.zero;
                bool slotFound = false;
                Quaternion targetRotation = Quaternion.identity;

                List<Vector3> randomSlots = new List<Vector3>(availableSlots);

                for (int t = 0; t < randomSlots.Count; t++)
                {
                    Vector3 temp = randomSlots[t];
                    int randomIndex = Random.Range(t, randomSlots.Count);
                    randomSlots[t] = randomSlots[randomIndex];
                    randomSlots[randomIndex] = temp;
                }

                foreach (Vector3 slot in randomSlots)
                {
                    if (!occupiedSlots.Contains(slot))
                    {
                        bestSlot = slot;
                        slotFound = true;
                        break;
                    }
                }

                float[] smartRotations = { 0f, 90f, 180f, 270f };
                float randomYRot = smartRotations[Random.Range(0, smartRotations.Length)];
                targetRotation = Quaternion.Euler(0, randomYRot, 0);

                if (!slotFound)
                {
                    Debug.LogWarning($"⚠️ สเปซเต็ม! ไม่สามารถวาง {item.furnitureName} ชิ้นที่ {i + 1} ได้");
                    break;
                }

                occupiedSlots.Add(bestSlot);
                GameObject spawnedObj = Instantiate(item.prefab, bestSlot, targetRotation);
                spawnedActualPieces.Add(spawnedObj);
            }
        }
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

        if (targetTerrain != null) FlattenTerrainAreaForce(minX, maxX, minZ, maxZ, lockedBuildHeightY);

        float baseFloorY = lockedBuildHeightY + 0.02f;
        float baseWallY = baseFloorY + floorThickness;

        for (int x = 0; x < countX; x++)
        {
            for (int z = 0; z < countZ; z++)
            {
                float fX = minX + (x * floorSize) + (floorSize / 2f);
                float fZ = minZ + (z * floorSize) + (floorSize / 2f);
                GameObject floorObj = Instantiate(floorPrefab, new Vector3(fX, baseFloorY, fZ), Quaternion.identity);
                SetLayerRecursively(floorObj, Mathf.RoundToInt(Mathf.Log(buildableLayer.value, 2)));
                spawnedActualPieces.Add(floorObj);
            }
        }

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

        AutoDecorateInteriorDynamic(minX, maxX, minZ, maxZ, baseFloorY);

        ClearPreviewPieces();
        currentState = BuilderState.Idle;
    }

    private void SpawnPreviewPiece(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return;
        GameObject previewObj = Instantiate(prefab, position, rotation);
        if (previewObj.TryGetComponent<Collider>(out Collider col)) col.enabled = false;
        if (previewMaterial != null)
        {
            if (previewObj.TryGetComponent<Renderer>(out Renderer rend)) rend.material = previewMaterial;
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

    private void SpawnActualPiece(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return;
        GameObject piece = Instantiate(prefab, position, rotation);
        spawnedActualPieces.Add(piece);
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
}