using UnityEngine;

public class TerrainModifierHandler : MonoBehaviour
{
    public enum BrushMode { None, PaintRoad, DigPond, Eraser }
    public BrushMode currentMode = BrushMode.None;

    [Header("Terrain Settings")]
    [SerializeField] private Terrain terrain;
    [SerializeField] private float brushSize = 2f;
    [SerializeField] private float minSize = 1f;
    [SerializeField] private float maxSize = 20f;
    [SerializeField] private float sizeStep = 1f;
    [SerializeField] private int roadLayerIndex = 1;
    [SerializeField] private int grassLayerIndex = 0;

    [Header("Performance Settings")]
    [SerializeField] private float modifyRate = 0.05f; // ความถี่ในการวาด (วินาที) ยิ่งน้อยยิ่งถี่
    private float nextModifyTime = 0f;

    [Header("2D Sprite Visualizer (Prefab)")]
    [SerializeField] private GameObject brushVisualizerPrefab;
    [SerializeField] private float heightOffset = 0.2f;

    private PlacementSystem system;
    private float[,] originalHeights;
    private GameObject visualizerInstance;

    public void Initialize(PlacementSystem sys)
    {
        system = sys;
        if (terrain != null)
        {
            // บันทึกค่าความสูงเดิมไว้สำหรับยางลบ
            originalHeights = terrain.terrainData.GetHeights(0, 0, terrain.terrainData.heightmapResolution, terrain.terrainData.heightmapResolution);
        }

        if (brushVisualizerPrefab != null && visualizerInstance == null)
        {
            visualizerInstance = Instantiate(brushVisualizerPrefab);
            visualizerInstance.SetActive(false);
        }
    }

    public void SetBrushMode(int modeIndex)
    {
        currentMode = (BrushMode)modeIndex;
        if (currentMode == BrushMode.None && visualizerInstance != null)
            visualizerInstance.SetActive(false);
    }

    public void HandleTerrainEditor(Camera cam, EditTransformHandler editHandler)
    {
        if (currentMode == BrushMode.None)
        {
            if (visualizerInstance != null) visualizerInstance.SetActive(false);
            return;
        }

        // ปรับขนาดแปรง (+/-)
        if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus))
            brushSize = Mathf.Clamp(brushSize + sizeStep, minSize, maxSize);
        if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
            brushSize = Mathf.Clamp(brushSize - sizeStep, minSize, maxSize);

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, system.groundLayer))
        {
            UpdateVisualizer(hit);

            // ตรวจสอบการคลิกเมาส์และเช็คจังหวะเวลา (Rate Limit)
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

            // บังคับให้นอนราบโดยการมองลงพื้น (-hit.normal)
            visualizerInstance.transform.rotation = Quaternion.LookRotation(-hit.normal);

            // ปรับ Scale เป็นวงกลม (X, Y เท่ากัน)
            float visualScale = brushSize * 2f;
            visualizerInstance.transform.localScale = new Vector3(visualScale, visualScale, 1f);
        }
    }

    private void ExecuteAction(Vector3 point, EditTransformHandler editHandler)
    {
        switch (currentMode)
        {
            case BrushMode.PaintRoad: PaintTerrain(point, roadLayerIndex); break;
            case BrushMode.DigPond: ModifyHeight(point, -0.01f); break; // ปรับค่าความลึกให้สมดุลกับ modifyRate
            case BrushMode.Eraser: PerformEraser(point, editHandler); break;
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

        // คำนวณพิกัด Alphamap แบบละเอียด
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
}