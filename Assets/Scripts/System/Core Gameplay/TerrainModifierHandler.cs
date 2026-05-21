using UnityEngine;

public class TerrainModifierHandler : MonoBehaviour
{
    // เพิ่ม RaiseMountain เข้ามาในระบบโครงสร้างประเภทแปรง
    public enum BrushMode { None, PaintRoad, DigPond, Eraser, RaiseMountain }
    public BrushMode currentMode = BrushMode.None;

    [Header("Terrain Settings")]
    [SerializeField] private Terrain terrain;
    [SerializeField] private float brushSize = 2f;
    [SerializeField] private float minSize = 1f;
    [SerializeField] private float maxSize = 20f;
    [SerializeField] private float sizeStep = 1f;

    [Space(5)]
    [Header("⛰️ Mountain Brush Settings (แยกเฉพาะของภูเขา)")]
    [SerializeField] private float mountainBrushSize = 8f; // ขนาดแปรงภูเขาปัจจุบัน (User กด +/- เพื่อปรับ กว้าง/แคบ ได้)
    [SerializeField] private float mountainBrushMaxSize = 40f; // ล็อกขนาดใหญ่สุดของแปรงภูเขา

    [Space(5)]
    [Header("📐 Mountain Shape Settings (สไตล์ Blocky ขอบคมด้านชัน)")]
    [SerializeField] private float firstClickHeightsOffset = 5f; // ระยะความสูงที่เด้งขึ้นมาจากพื้นเดิมทันที
    [SerializeField] private float maxMountainHeightOffset = 50f; // เพดานความสูงสูงสุด

    [Space(5)]
    [SerializeField] private int roadLayerIndex = 1;
    [SerializeField] private int grassLayerIndex = 0;

    [Header("Performance Settings")]
    [SerializeField] private float modifyRate = 0.05f; // ความถี่ในการวาด (วินาที) ยิ่งน้อยยิ่งถี่
    [SerializeField] private float mountainRaiseSpeed = 5f; // ความเร็วในการดึงดินปัจจุบัน (User กด [ / ] เพื่อปรับ สูงไว/สูงช้า ได้)
    [SerializeField] private float speedStep = 1f; // สเต็ปการเพิ่มลดความเร็วความสูงในการกดแต่ละครั้ง
    [SerializeField] private float minRaiseSpeed = 1f; // ความเร็วขั้นต่ำ
    [SerializeField] private float maxRaiseSpeed = 20f; // ความเร็วขั้นสูงสุด
    private float nextModifyTime = 0f;

    [Header("2D Sprite Visualizer (Prefab)")]
    [SerializeField] private GameObject brushVisualizerPrefab;
    [SerializeField] private float heightOffset = 0.2f;

    [Header("✨ Game View GL Line Settings")]
    [SerializeField] private Material lineMaterial;
    [SerializeField] private Color gameViewCircleColor = new Color(1f, 0f, 0f, 0.9f);

    private PlacementSystem system;
    private float[,] originalHeights;
    private GameObject visualizerInstance;

    private bool isFirstClick = false;
    private float baseTerrainHeightAtClick = 0f;
    private Vector3 currentMouseHitPoint;
    private Camera cachedCam;

    public void Initialize(PlacementSystem sys)
    {
        system = sys;
        if (terrain != null)
        {
            originalHeights = terrain.terrainData.GetHeights(0, 0, terrain.terrainData.heightmapResolution, terrain.terrainData.heightmapResolution);
        }

        if (brushVisualizerPrefab != null && visualizerInstance == null)
        {
            visualizerInstance = Instantiate(brushVisualizerPrefab);
            visualizerInstance.SetActive(false);
        }

        if (lineMaterial == null)
        {
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            if (shader != null) lineMaterial = new Material(shader);
        }
    }

    public void SetBrushMode(int modeIndex)
    {
        currentMode = (BrushMode)modeIndex;
        if (currentMode == BrushMode.None && visualizerInstance != null)
            visualizerInstance.SetActive(false);
    }

    public void SetMountainShapeByTool(ToolManager.BuildTool currentTool)
    {
        if (currentTool == ToolManager.BuildTool.House)
        {
            mountainBrushSize = 12f;
        }
        else if (currentTool == ToolManager.BuildTool.Furniture)
        {
            mountainBrushSize = 5f;
        }
        else
        {
            mountainBrushSize = 8f;
        }
    }

    public void HandleTerrainEditor(Camera cam, EditTransformHandler editHandler)
    {
        cachedCam = cam;

        // ปุ่มลัดพิเศษ: กดเลข 8 บนแป้นพิมพ์เพื่อเข้าสู่โหมดทำภูเขาทันที
        if (Input.GetKeyDown(KeyCode.Alpha8) || Input.GetKeyDown(KeyCode.Keypad8))
        {
            SetBrushMode((int)BrushMode.RaiseMountain);
        }

        if (currentMode == BrushMode.None)
        {
            if (visualizerInstance != null) visualizerInstance.SetActive(false);
            return;
        }

        // 1. ระบบปรับขนาดแปรงอินเกม (User กดเพื่อปรับ กว้างขึ้น / แคบลง)
        if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            if (currentMode == BrushMode.RaiseMountain)
                mountainBrushSize = Mathf.Clamp(mountainBrushSize + sizeStep, minSize, mountainBrushMaxSize);
            else
                brushSize = Mathf.Clamp(brushSize + sizeStep, minSize, maxSize);
        }

        if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            if (currentMode == BrushMode.RaiseMountain)
                mountainBrushSize = Mathf.Clamp(mountainBrushSize - sizeStep, minSize, mountainBrushMaxSize);
            else
                brushSize = Mathf.Clamp(brushSize - sizeStep, minSize, maxSize);
        }

        // 2. ระบบปรับความเร็ว/ความชันอินเกม 
        if (currentMode == BrushMode.RaiseMountain)
        {
            // ✅ แก้ไข: เปลี่ยนจาก CloseBracket เป็น RightBracket
            if (Input.GetKeyDown(KeyCode.RightBracket))
            {
                mountainRaiseSpeed = Mathf.Clamp(mountainRaiseSpeed + speedStep, minRaiseSpeed, maxRaiseSpeed);
            }
            // ✅ แก้ไข: เปลี่ยนจาก OpenBracket เป็น LeftBracket
            if (Input.GetKeyDown(KeyCode.LeftBracket))
            {
                mountainRaiseSpeed = Mathf.Clamp(mountainRaiseSpeed - speedStep, minRaiseSpeed, maxRaiseSpeed);
            }
        }

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, system.groundLayer))
        {
            currentMouseHitPoint = hit.point;
            UpdateVisualizer(hit);

            if (Input.GetMouseButtonDown(0) && currentMode == BrushMode.RaiseMountain)
            {
                isFirstClick = true;
                if (terrain)
                {
                    baseTerrainHeightAtClick = terrain.SampleHeight(hit.point);
                }
            }

            if (Input.GetMouseButton(0) && Time.time >= nextModifyTime)
            {
                ExecuteAction(hit.point, editHandler);
                nextModifyTime = Time.time + modifyRate;
            }
        }
        else
        {
            if (visualizerInstance != null) visualizerInstance.SetActive(false);
        }
    }

    private void UpdateVisualizer(RaycastHit hit)
    {
        if (visualizerInstance != null)
        {
            if (!visualizerInstance.activeSelf) visualizerInstance.SetActive(true);

            visualizerInstance.transform.position = hit.point + (Vector3.up * heightOffset);
            visualizerInstance.transform.rotation = Quaternion.LookRotation(-hit.normal);

            float activeSize = (currentMode == BrushMode.RaiseMountain) ? mountainBrushSize : brushSize;
            float visualScale = activeSize * 2f;
            visualizerInstance.transform.localScale = new Vector3(visualScale, visualScale, 1f);
        }
    }

    private void ExecuteAction(Vector3 point, EditTransformHandler editHandler)
    {
        switch (currentMode)
        {
            case BrushMode.PaintRoad: PaintTerrain(point, roadLayerIndex); break;
            case BrushMode.DigPond: ModifyHeight(point, -0.01f); break;
            case BrushMode.Eraser: PerformEraser(point, editHandler); break;

            case BrushMode.RaiseMountain:
                float calculatedAmt = isFirstClick ? firstClickHeightsOffset : (mountainRaiseSpeed * modifyRate);
                ModifyHeightBlockyStyle(point, calculatedAmt);
                isFirstClick = false;
                break;
        }
    }

    private void PerformEraser(Vector3 point, EditTransformHandler editHandler)
    {
        Collider[] hits = Physics.OverlapSphere(point, brushSize, system.buildableLayer);
        foreach (Collider c in hits)
        {
            if (editHandler != null) editHandler.StopEditing();
            Destroy(c.gameObject);
        }
        FlattenTerrain(point);
        PaintTerrain(point, grassLayerIndex);
    }

    private void PaintTerrain(Vector3 worldPos, int layerIdx)
    {
        if (!terrain) return;
        TerrainData td = terrain.terrainData;

        float percentX = (worldPos.x - terrain.transform.position.x) / td.size.x;
        float percentZ = (worldPos.z - terrain.transform.position.z) / td.size.z;

        int mapX = Mathf.RoundToInt(percentX * td.alphamapWidth);
        int mapZ = Mathf.RoundToInt(percentZ * td.alphamapHeight);

        int r = Mathf.RoundToInt(brushSize);
        int startX = Mathf.Clamp(mapX - r, 0, td.alphamapWidth);
        int startZ = Mathf.Clamp(mapZ - r, 0, td.alphamapHeight);
        int endX = Mathf.Clamp(mapX + r, 0, td.alphamapWidth);
        int endZ = Mathf.Clamp(mapZ + r, 0, td.alphamapHeight);

        int width = endX - startX;
        int height = endZ - startZ;

        if (width <= 0 || height <= 0) return;

        float[,,] alphas = td.GetAlphamaps(startX, startZ, width, height);
        for (int i = 0; i < height; i++)
            for (int j = 0; j < width; j++)
                for (int k = 0; k < td.alphamapLayers; k++)
                    alphas[i, j, k] = (k == layerIdx) ? 1f : 0f;

        td.SetAlphamaps(startX, startZ, alphas);
    }

    private void ModifyHeight(Vector3 worldPos, float amt)
    {
        if (!terrain) return;
        TerrainData td = terrain.terrainData;
        int res = td.heightmapResolution;

        float percentX = (worldPos.x - terrain.transform.position.x) / td.size.x;
        float percentZ = (worldPos.z - terrain.transform.position.z) / td.size.z;

        int x = Mathf.RoundToInt(percentX * (res - 1));
        int z = Mathf.RoundToInt(percentZ * (res - 1));

        int r = Mathf.RoundToInt(brushSize);
        int startX = Mathf.Clamp(x - r, 0, res);
        int startZ = Mathf.Clamp(z - r, 0, res);
        int endX = Mathf.Clamp(x + r, 0, res);
        int endZ = Mathf.Clamp(z + r, 0, res);

        int width = endX - startX;
        int height = endZ - startZ;

        if (width <= 0 || height <= 0) return;

        float[,] heights = td.GetHeights(startX, startZ, width, height);
        for (int i = 0; i < height; i++)
            for (int j = 0; j < width; j++)
                heights[i, j] = Mathf.Clamp01(heights[i, j] + amt);

        td.SetHeights(startX, startZ, heights);
    }

    private void ModifyHeightBlockyStyle(Vector3 worldPos, float amt)
    {
        if (!terrain) return;
        TerrainData td = terrain.terrainData;
        int res = td.heightmapResolution;

        float normalizedAmt = amt / td.size.y;
        float normalizedMaxHeight = (baseTerrainHeightAtClick + maxMountainHeightOffset) / td.size.y;

        float percentX = (worldPos.x - terrain.transform.position.x) / td.size.x;
        float percentZ = (worldPos.z - terrain.transform.position.z) / td.size.z;

        int centerX = Mathf.RoundToInt(percentX * (res - 1));
        int centerZ = Mathf.RoundToInt(percentZ * (res - 1));

        int r = Mathf.RoundToInt(mountainBrushSize);
        int startX = Mathf.Clamp(centerX - r, 0, res);
        int startZ = Mathf.Clamp(centerZ - r, 0, res);
        int endX = Mathf.Clamp(centerX + r, 0, res);
        int endZ = Mathf.Clamp(centerZ + r, 0, res);

        int width = endX - startX;
        int height = endZ - startZ;

        if (width <= 0 || height <= 0) return;

        float[,] heights = td.GetHeights(startX, startZ, width, height);

        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                int currentX = startX + j;
                int currentZ = startZ + i;

                float distance = Vector2.Distance(new Vector2(centerX, centerZ), new Vector2(currentX, currentZ));

                if (distance <= mountainBrushSize)
                {
                    float falloff = 1f;
                    heights[i, j] = Mathf.Clamp(heights[i, j] + (normalizedAmt * falloff), 0f, normalizedMaxHeight);
                }
            }
        }

        td.SetHeights(startX, startZ, heights);
    }

    private void FlattenTerrain(Vector3 worldPos)
    {
        if (!terrain || originalHeights == null) return;
        TerrainData td = terrain.terrainData;
        int res = td.heightmapResolution;

        float percentX = (worldPos.x - terrain.transform.position.x) / td.size.x;
        float percentZ = (worldPos.z - terrain.transform.position.z) / td.size.z;

        int x = Mathf.RoundToInt(percentX * (res - 1));
        int z = Mathf.RoundToInt(percentZ * (res - 1));

        int r = Mathf.RoundToInt(brushSize);
        int startX = Mathf.Clamp(x - r, 0, res);
        int startZ = Mathf.Clamp(z - r, 0, res);
        int endX = Mathf.Clamp(x + r, 0, res);
        int endZ = Mathf.Clamp(z + r, 0, res);

        int width = endX - startX;
        int height = endZ - startZ;

        if (width <= 0 || height <= 0) return;

        float[,] restoreHeights = new float[height, width];
        for (int i = 0; i < height; i++)
            for (int j = 0; j < width; j++)
                restoreHeights[i, j] = originalHeights[startZ + i, startX + j];

        td.SetHeights(startX, startZ, restoreHeights);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (currentMode != BrushMode.RaiseMountain || system == null || terrain == null) return;

        Ray ray = UnityEditor.HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, system.groundLayer))
        {
            Gizmos.color = gameViewCircleColor;
            Gizmos.DrawWireSphere(hit.point, mountainBrushSize);
        }
    }
#endif

    private void OnRenderObject()
    {
        if (currentMode != BrushMode.RaiseMountain || lineMaterial == null || currentMouseHitPoint == Vector3.zero) return;
        if (Camera.current != cachedCam) return;

        lineMaterial.SetPass(0);
        GL.Begin(GL.LINES);
        GL.Color(gameViewCircleColor);

        float radius = mountainBrushSize;
        int segments = 40;

        Vector3 previousPoint = currentMouseHitPoint + new Vector3(radius, 0.1f, 0f);

        for (int i = 1; i <= segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            Vector3 nextPointLocal = new Vector3(x, 0.1f, z);
            Vector3 nextPointWorld = currentMouseHitPoint + nextPointLocal;

            if (terrain)
            {
                previousPoint.y = terrain.SampleHeight(previousPoint) + terrain.transform.position.y + 0.1f;
                nextPointWorld.y = terrain.SampleHeight(nextPointWorld) + terrain.transform.position.y + 0.1f;
            }

            GL.Vertex(previousPoint);
            GL.Vertex(nextPointWorld);

            previousPoint = nextPointWorld;
        }

        GL.End();
    }
    // 🧱 ฟังก์ชันงอกใหม่ 1: สั่งปั้นภูเขาแบบระบุพิกัดตรง ๆ (ใช้ตอนย้ายตำแหน่งชิ้นงาน)
    public void CreateMountainAtPosition(Vector3 position, float size, float speed)
    {
        if (!terrain) return;

        // จำลองสถานการณ์เสมือนคลิกเมาส์ที่จุดนั้น ๆ 
        baseTerrainHeightAtClick = terrain.SampleHeight(position);

        // สั่งใช้ลอจิกทรงเหลี่ยมคม (Blocky Style) วาดลงไปที่ตำแหน่งนั้นทันที
        float calculatedAmt = 5f; // ค่าความสูงเริ่มต้น หรือดึงจาก speed * modifyRate

        // ปรับแก้ฟังก์ชัน ModifyHeightBlockyStyle ตัวเดิมของปิ๊บให้รับค่าขนาดแบบ Dynamic ได้
        ModifyHeightBlockyStyleAt(position, calculatedAmt, size);
    }

    // 🧹 ฟังก์ชันงอกใหม่ 2: สั่งลบภูเขาเฉพาะจุดให้กลับมาแบนราบ (เมื่อภูเขาโดนย้ายหนีหรือโดนลบ)
    public void FlattenMountainAtPosition(Vector3 position, float size)
    {
        if (!terrain || originalHeights == null) return;
        TerrainData td = terrain.terrainData;
        int res = td.heightmapResolution;

        float percentX = (position.x - terrain.transform.position.x) / td.size.x;
        float percentZ = (position.z - terrain.transform.position.z) / td.size.z;

        int centerX = Mathf.RoundToInt(percentX * (res - 1));
        int centerZ = Mathf.RoundToInt(percentZ * (res - 1));

        int r = Mathf.RoundToInt(size);
        int startX = Mathf.Clamp(centerX - r, 0, res);
        int startZ = Mathf.Clamp(centerZ - r, 0, res);
        int endX = Mathf.Clamp(centerX + r, 0, res);
        int endZ = Mathf.Clamp(centerZ + r, 0, res);

        int width = endX - startX;
        int height = endZ - startZ;

        if (width <= 0 || height <= 0) return;

        float[,] restoreHeights = new float[height, width];
        for (int i = 0; i < height; i++)
            for (int j = 0; j < width; j++)
                restoreHeights[i, j] = originalHeights[startZ + i, startX + j];

        td.SetHeights(startX, startZ, restoreHeights);
    }

    // ฟังก์ชันช่วยคำนวณแบบระบุตำแหน่งและขนาดได้อิสระ
    private void ModifyHeightBlockyStyleAt(Vector3 targetPos, float amt, float customSize)
    {
        TerrainData td = terrain.terrainData;
        int res = td.heightmapResolution;
        float normalizedAmt = amt / td.size.y;
        float normalizedMaxHeight = (baseTerrainHeightAtClick + maxMountainHeightOffset) / td.size.y;

        float percentX = (targetPos.x - terrain.transform.position.x) / td.size.x;
        float percentZ = (targetPos.z - terrain.transform.position.z) / td.size.z;

        int centerX = Mathf.RoundToInt(percentX * (res - 1));
        int centerZ = Mathf.RoundToInt(percentZ * (res - 1));

        int r = Mathf.RoundToInt(customSize);
        int startX = Mathf.Clamp(centerX - r, 0, res);
        int startZ = Mathf.Clamp(centerZ - r, 0, res);
        int endX = Mathf.Clamp(centerX + r, 0, res);
        int endZ = Mathf.Clamp(centerZ + r, 0, res);

        int width = endX - startX;
        int height = endZ - startZ;

        if (width <= 0 || height <= 0) return;
        float[,] heights = td.GetHeights(startX, startZ, width, height);

        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                int currentX = startX + j;
                int currentZ = startZ + i;
                float distance = Vector2.Distance(new Vector2(centerX, centerZ), new Vector2(currentX, currentZ));

                if (distance <= customSize)
                {
                    heights[i, j] = Mathf.Clamp(heights[i, j] + normalizedAmt, 0f, normalizedMaxHeight);
                }
            }
        }
        td.SetHeights(startX, startZ, heights);
    }
}