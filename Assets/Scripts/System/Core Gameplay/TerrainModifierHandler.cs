using UnityEngine;
using System.Collections.Generic;

public class TerrainModifierHandler : MonoBehaviour
{
    public enum BrushMode { None, PaintRoad, DigPond, Eraser, EditFreeform }
    [Header("🎮 Current Tool Active")]
    public BrushMode currentMode = BrushMode.None;

    [Header("Terrain Settings")]
    [SerializeField] private Terrain terrain;
    public float brushSize = 2f;
    public float minSize = 1f;
    public float maxSize = 20f;
    public float sizeStep = 1f;

    [Space(5)]
    [Header("⛰️ Mountain Brush Settings (รัศมีฐานเขาเลข 8)")]
    public float mountainBrushSize = 5f;
    public float mountainBrushMaxSize = 8f;

    [Space(5)]
    [Header("📐 Mountain Shape Settings")]
    public float firstClickHeightsOffset = 5f;
    public float maxMountainHeightOffset = 20f;

    [Space(5)]
    [Header("🍃 Tiny Glade Style Settings")]
    public int cliffLayerIndex = 2;
    [Range(10f, 60f)] public float cliffSlopeThreshold = 30f;

    [Space(5)]
    [SerializeField] private int roadLayerIndex = 1;
    [SerializeField] private int grassLayerIndex = 0;

    [Header("Performance Settings")]
    public float modifyRate = 0.1f;
    public float mountainRaiseSpeed = 0.0005f;
    public float speedStep = 1f;
    public float minRaiseSpeed = 1f;
    public float maxRaiseSpeed = 20f;
    private float nextModifyTime = 0f;

    [Header("2D Sprite Visualizer (Prefab)")]
    public GameObject brushVisualizerPrefab;
    public float heightOffset = 0.5f;

    [Header("🧱 Mountain Anchor Spawner")]
    public GameObject mountainAnchorPrefab;

    private List<MountainEditableAnchor> activeAnchorsInScene = new List<MountainEditableAnchor>();
    public MountainEditableAnchor currentlySelectedMountain = null;

    private PlacementSystem system;
    private float[,] originalHeights;
    private GameObject visualizerInstance;

    private bool isFirstClick = false;
    private float baseTerrainHeightAtClick = 0f;

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
    }

    public void SetBrushMode(int modeIndex)
    {
        currentMode = (BrushMode)modeIndex;
        Debug.Log($"<color=lime>[TerrainHandler]</color> สลับแปรงเป็นโหมด: <b>{currentMode}</b>");
        if (currentMode == BrushMode.None && visualizerInstance != null)
            visualizerInstance.SetActive(false);
    }

    public void HandleTerrainEditor(Camera cam, EditTransformHandler editHandler)
    {
        // 🛠️ ปุ่ม ESC สั่งซ่อนกิซโมให้เล่นอนิเมชั่นหด Scale ลงดินนุ่มนวล พร้อมปิดโหมดแปรงทันที
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            DeselectCurrentMountain();
            SetBrushMode((int)BrushMode.None);
            Debug.Log("<color=red><b>[ESC Pressed]</b></color> สั่งซ่อนกิซโมแบบนุ่มนวลเรียบร้อยแล้วนะปิ๊บ!");
            return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha8) || Input.GetKeyDown(KeyCode.Keypad8)) SetBrushMode((int)BrushMode.EditFreeform);

        if (currentMode == BrushMode.None)
        {
            if (visualizerInstance != null) visualizerInstance.SetActive(false);

            if (Input.GetMouseButtonDown(0) && currentlySelectedMountain != null)
            {
                DeselectCurrentMountain();
            }
            return;
        }

        if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            if (currentMode == BrushMode.EditFreeform)
                mountainBrushSize = Mathf.Clamp(mountainBrushSize + sizeStep, minSize, mountainBrushMaxSize);
            else
                brushSize = Mathf.Clamp(brushSize + sizeStep, minSize, maxSize);
        }
        if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            if (currentMode == BrushMode.EditFreeform)
                mountainBrushSize = Mathf.Clamp(mountainBrushSize - sizeStep, minSize, mountainBrushMaxSize);
            else
                brushSize = Mathf.Clamp(brushSize - sizeStep, minSize, maxSize);
        }

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, system.groundLayer))
        {
            UpdateVisualizer(hit);

            if (Input.GetMouseButtonDown(0))
            {
                MountainEditableAnchor clickedMountain = FindMountainAtPosition(hit.point);

                if (clickedMountain != null)
                {
                    DeselectCurrentMountain();
                    currentlySelectedMountain = clickedMountain;
                    currentlySelectedMountain.SelectMountain(true);
                    if (visualizerInstance != null) visualizerInstance.SetActive(false);
                    return;
                }

                if (currentlySelectedMountain != null)
                {
                    DeselectCurrentMountain();
                    return;
                }

                if (currentMode == BrushMode.EditFreeform && mountainAnchorPrefab != null)
                {
                    SpawnMountainAnchorAtPosition(hit.point);
                    return;
                }

                isFirstClick = true;
                if (terrain) baseTerrainHeightAtClick = terrain.SampleHeight(hit.point);
            }

            if (Input.GetMouseButton(0) && Time.time >= nextModifyTime && currentlySelectedMountain == null)
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

    private MountainEditableAnchor FindMountainAtPosition(Vector3 worldPos)
    {
        activeAnchorsInScene.RemoveAll(item => item == null);
        foreach (var anchor in activeAnchorsInScene)
        {
            float distXZ = Vector2.Distance(new Vector2(worldPos.x, worldPos.z), new Vector2(anchor.transform.position.x, anchor.transform.position.z));
            if (distXZ <= anchor.GetCurrentSize()) return anchor;
        }
        return null;
    }

    public void DeselectCurrentMountain()
    {
        if (currentlySelectedMountain != null)
        {
            // 🟢 ปลดสถานะเลือก เพื่อเปิดทางให้ลูกบอลวิ่งอนิเมชั่น Lerp หดขนาดสเกลลงดินแบบ Cozy สวยงามร้อยเปอร์เซ็นต์!
            currentlySelectedMountain.SelectMountain(false);
            currentlySelectedMountain = null;
        }

        if (visualizerInstance != null)
        {
            visualizerInstance.SetActive(false);
        }
    }

    private void SpawnMountainAnchorAtPosition(Vector3 worldPos)
    {
        Vector3 spawnPos = worldPos;
        if (terrain != null) spawnPos.y = terrain.SampleHeight(worldPos) + terrain.transform.position.y;

        GameObject anchorGo = Instantiate(mountainAnchorPrefab, spawnPos, Quaternion.identity);
        MountainEditableAnchor anchorScript = anchorGo.GetComponent<MountainEditableAnchor>();
        if (anchorScript != null)
        {
            anchorScript.SetupAnchor(this, mountainBrushSize, mountainRaiseSpeed);
            activeAnchorsInScene.Add(anchorScript);
            currentlySelectedMountain = anchorScript;
        }
    }

    private void UpdateVisualizer(RaycastHit hit)
    {
        if (visualizerInstance != null)
        {
            if (currentlySelectedMountain != null)
            {
                visualizerInstance.SetActive(false);
                return;
            }

            if (!visualizerInstance.activeSelf) visualizerInstance.SetActive(true);
            visualizerInstance.transform.position = hit.point + (Vector3.up * heightOffset);
            visualizerInstance.transform.rotation = Quaternion.LookRotation(-hit.normal);

            float activeSize = (currentMode == BrushMode.EditFreeform) ? mountainBrushSize : brushSize;
            float visualScale = activeSize * 2f;
            visualizerInstance.transform.localScale = new Vector3(visualScale, visualScale, 1f);
        }
    }

    private void ExecuteAction(Vector3 point, EditTransformHandler editHandler)
    {
        float calculatedAmt = isFirstClick ? firstClickHeightsOffset : (mountainRaiseSpeed * modifyRate);
        switch (currentMode)
        {
            case BrushMode.PaintRoad: PaintTerrain(point, roadLayerIndex); break;
            case BrushMode.DigPond: ModifyHeight(point, -0.01f); break;
            case BrushMode.Eraser: PerformEraser(point, editHandler); break;
            case BrushMode.EditFreeform:
                ModifyHeightFreeformStyle(point, calculatedAmt);
                isFirstClick = false;
                break;
        }
    }

    private void ModifyHeightFreeformStyle(Vector3 worldPos, float amt)
    {
        if (!terrain) return;
        TerrainData td = terrain.terrainData;
        int res = td.heightmapResolution;
        float normalizedAmt = amt / td.size.y;
        float normalizedMaxHeight = (baseTerrainHeightAtClick + maxMountainHeightOffset) / td.size.y;

        int centerX = Mathf.RoundToInt(((worldPos.x - terrain.transform.position.x) / td.size.x) * (res - 1));
        int centerZ = Mathf.RoundToInt(((worldPos.z - terrain.transform.position.z) / td.size.z) * (res - 1));
        float radiusInPixels = (mountainBrushSize / td.size.x) * (res - 1);
        int r = Mathf.RoundToInt(radiusInPixels);

        int startX = Mathf.Clamp(centerX - r, 0, res - 1);
        int startZ = Mathf.Clamp(centerZ - r, 0, res - 1);
        int endX = Mathf.Clamp(centerX + r, 0, res - 1);
        int endZ = Mathf.Clamp(centerZ + r, 0, res - 1);

        int width = endX - startX + 1; int height = endZ - startZ + 1;
        if (width <= 0 || height <= 0) return;

        float[,] heights = td.GetHeights(startX, startZ, width, height);
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                float distance = Vector2.Distance(new Vector2(centerX, centerZ), new Vector2(startX + j, startZ + i));
                if (distance <= radiusInPixels)
                {
                    float t = distance / radiusInPixels;
                    float falloff = 1f - (t * t * (3f - 2f * t));
                    heights[i, j] = Mathf.Clamp(heights[i, j] + (normalizedAmt * falloff), 0f, normalizedMaxHeight);
                }
            }
        }
        td.SetHeights(startX, startZ, heights);
        ApplyAutoCliffTexture(startX, startZ, width, height);
    }

    private void ApplyAutoCliffTexture(int startX, int startZ, int width, int height)
    {
        if (!terrain) return;
        TerrainData td = terrain.terrainData;
        int res = td.heightmapResolution;

        int alphaStartX = Mathf.Clamp(Mathf.RoundToInt((float)startX / res * td.alphamapWidth), 0, td.alphamapWidth - 1);
        int alphaGridZ = Mathf.Clamp(Mathf.RoundToInt((float)startZ / res * td.alphamapHeight), 0, td.alphamapHeight - 1);
        int alphaWidth = Mathf.Clamp(Mathf.RoundToInt((float)width / res * td.alphamapWidth), 1, td.alphamapWidth - alphaStartX);
        int alphaHeight = Mathf.Clamp(Mathf.RoundToInt((float)height / res * td.alphamapHeight), 1, td.alphamapHeight - alphaGridZ);

        float[,,] alphas = td.GetAlphamaps(alphaStartX, alphaGridZ, alphaWidth, alphaHeight);
        for (int i = 0; i < alphaHeight; i++)
        {
            for (int j = 0; j < alphaWidth; j++)
            {
                float slope = td.GetSteepness((float)(alphaStartX + j) / td.alphamapWidth, (float)(alphaGridZ + i) / td.alphamapHeight);
                bool isCliff = slope >= cliffSlopeThreshold;
                for (int k = 0; k < td.alphamapLayers; k++)
                {
                    alphas[i, j, k] = isCliff ? (k == cliffLayerIndex ? 1f : 0f) : (k == grassLayerIndex ? 1f : 0f);
                }
            }
        }
        td.SetAlphamaps(alphaStartX, alphaGridZ, alphas);
    }

    private void PerformEraser(Vector3 point, EditTransformHandler editHandler)
    {
        Collider[] hits = Physics.OverlapSphere(point, brushSize, system.buildableLayer);
        foreach (Collider c in hits) { if (editHandler != null) editHandler.StopEditing(); Destroy(c.gameObject); }
        FlattenTerrain(point); PaintTerrain(point, grassLayerIndex);
    }

    private void PaintTerrain(Vector3 worldPos, int layerIdx)
    {
        if (!terrain) return; TerrainData td = terrain.terrainData;
        int mapX = Mathf.RoundToInt(((worldPos.x - terrain.transform.position.x) / td.size.x) * td.alphamapWidth);
        int mapZ = Mathf.RoundToInt(((worldPos.z - terrain.transform.position.z) / td.size.z) * td.alphamapHeight);
        int r = Mathf.RoundToInt(brushSize);
        int startX = Mathf.Clamp(mapX - r, 0, td.alphamapWidth - 1); int startZ = Mathf.Clamp(mapZ - r, 0, td.alphamapHeight - 1);
        int width = Mathf.Clamp(mapX + r, 0, td.alphamapWidth) - startX; int height = Mathf.Clamp(mapZ + r, 0, td.alphamapHeight) - startZ;
        if (width <= 0 || height <= 0) return;

        float[,,] alphas = td.GetAlphamaps(startX, startZ, width, height);
        for (int i = 0; i < height; i++)
            for (int j = 0; j < width; j++)
                for (int k = 0; k < td.alphamapLayers; k++) alphas[i, j, k] = (k == layerIdx) ? 1f : 0f;
        td.SetAlphamaps(startX, startZ, alphas);
    }

    private void ModifyHeight(Vector3 worldPos, float amt)
    {
        if (!terrain) return; TerrainData td = terrain.terrainData; int res = td.heightmapResolution;
        int x = Mathf.RoundToInt(((worldPos.x - terrain.transform.position.x) / td.size.x) * (res - 1));
        int z = Mathf.RoundToInt(((worldPos.z - terrain.transform.position.z) / td.size.z) * (res - 1));
        int r = Mathf.RoundToInt(brushSize);
        int startX = Mathf.Clamp(x - r, 0, res - 1); int startZ = Mathf.Clamp(z - r, 0, res - 1);
        int width = Mathf.Clamp(x + r, 0, res) - startX; int height = Mathf.Clamp(z + r, 0, res) - startZ;
        if (width <= 0 || height <= 0) return;

        float[,] heights = td.GetHeights(startX, startZ, width, height);
        for (int i = 0; i < height; i++)
            for (int j = 0; j < width; j++) heights[i, j] = Mathf.Clamp01(heights[i, j] + amt);
        td.SetHeights(startX, startZ, heights);
    }

    private void FlattenTerrain(Vector3 worldPos)
    {
        if (!terrain || originalHeights == null) return; TerrainData td = terrain.terrainData; int res = td.heightmapResolution;
        int x = Mathf.RoundToInt(((worldPos.x - terrain.transform.position.x) / td.size.x) * (res - 1));
        int z = Mathf.RoundToInt(((worldPos.z - terrain.transform.position.z) / td.size.z) * (res - 1));
        int r = Mathf.RoundToInt(brushSize);
        int startX = Mathf.Clamp(x - r, 0, res - 1); int startZ = Mathf.Clamp(z - r, 0, res - 1);
        int width = Mathf.Clamp(x + r, 0, res) - startX; int height = Mathf.Clamp(z + r, 0, res) - startZ;
        if (width <= 0 || height <= 0) return;

        float[,] restoreHeights = new float[height, width];
        for (int i = 0; i < height; i++)
            for (int j = 0; j < width; j++) restoreHeights[i, j] = originalHeights[startZ + i, startX + j];
        td.SetHeights(startX, startZ, restoreHeights);
    }

    public void CreateMountainAtPositionDynamic(Vector3 position, float size, float speed, float heightMultiplier)
    {
        if (!terrain) return;
        baseTerrainHeightAtClick = terrain.SampleHeight(position);
        ModifyHeightFreeformStyleAtDynamic(position, speed * modifyRate * heightMultiplier, size);
    }

    public void FlattenMountainAtPosition(Vector3 position, float size)
    {
        if (!terrain || originalHeights == null) return;
        TerrainData td = terrain.terrainData; int res = td.heightmapResolution;
        int centerX = Mathf.RoundToInt(((position.x - terrain.transform.position.x) / td.size.x) * (res - 1));
        int centerZ = Mathf.RoundToInt(((position.z - terrain.transform.position.z) / td.size.z) * (res - 1));
        float radiusInPixels = (size / td.size.x) * (res - 1); int r = Mathf.RoundToInt(radiusInPixels);

        int startX = Mathf.Clamp(centerX - r, 0, res - 1); int startZ = Mathf.Clamp(centerZ - r, 0, res - 1);
        int endX = Mathf.Clamp(centerX + r, 0, res - 1); int endZ = Mathf.Clamp(centerZ + r, 0, res - 1);
        int width = endX - startX + 1; int height = endZ - startZ + 1;
        if (width <= 0 || height <= 0) return;

        float[,] restoreHeights = new float[height, width];
        for (int i = 0; i < height; i++)
            for (int j = 0; j < width; j++) restoreHeights[i, j] = originalHeights[startZ + i, startX + j];
        td.SetHeights(startX, startZ, restoreHeights);

        int alphaStartX = Mathf.Clamp(Mathf.RoundToInt((float)startX / res * td.alphamapWidth), 0, td.alphamapWidth - 1);
        int alphaGridZ = Mathf.Clamp(Mathf.RoundToInt((float)startZ / res * td.alphamapHeight), 0, td.alphamapHeight - 1);
        int alphaWidth = Mathf.Clamp(Mathf.RoundToInt((float)width / res * td.alphamapWidth), 1, td.alphamapWidth - alphaStartX);
        int alphaHeight = Mathf.Clamp(Mathf.RoundToInt((float)height / res * td.alphamapHeight), 1, td.alphamapHeight - alphaGridZ);

        if (alphaWidth > 0 && alphaHeight > 0)
        {
            float[,,] alphas = td.GetAlphamaps(alphaStartX, alphaGridZ, alphaWidth, alphaHeight);
            for (int i = 0; i < alphaHeight; i++)
                for (int j = 0; j < alphaWidth; j++)
                    for (int k = 0; k < td.alphamapLayers; k++) alphas[i, j, k] = (k == grassLayerIndex) ? 1f : 0f;
            td.SetAlphamaps(alphaStartX, alphaGridZ, alphas);
        }
    }

    private void ModifyHeightFreeformStyleAtDynamic(Vector3 targetPos, float amt, float customSize)
    {
        if (!terrain) return;
        TerrainData td = terrain.terrainData; int res = td.heightmapResolution;
        float normalizedAmt = amt / td.size.y;
        float normalizedMaxHeight = (baseTerrainHeightAtClick + maxMountainHeightOffset) / td.size.y;

        int centerX = Mathf.RoundToInt(((targetPos.x - terrain.transform.position.x) / td.size.x) * (res - 1));
        int centerZ = Mathf.RoundToInt(((targetPos.z - terrain.transform.position.z) / td.size.z) * (res - 1));
        float radiusInPixels = (customSize / td.size.x) * (res - 1); int r = Mathf.RoundToInt(radiusInPixels);

        int startX = Mathf.Clamp(centerX - r, 0, res - 1); int startZ = Mathf.Clamp(centerZ - r, 0, res - 1);
        int endX = Mathf.Clamp(centerX + r, 0, res - 1); int endZ = Mathf.Clamp(centerZ + r, 0, res - 1);
        int width = endX - startX + 1; int height = endZ - startZ + 1;
        if (width <= 0 || height <= 0) return;

        float[,] heights = td.GetHeights(startX, startZ, width, height);
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                float distance = Vector2.Distance(new Vector2(centerX, centerZ), new Vector2(startX + j, startZ + i));
                if (distance <= radiusInPixels)
                {
                    float t = distance / radiusInPixels;
                    float falloff = 1f - (t * t * (3f - 2f * t));
                    heights[i, j] = Mathf.Clamp(heights[i, j] + (normalizedAmt * falloff), 0f, normalizedMaxHeight);
                }
            }
        }
        td.SetHeights(startX, startZ, heights);
        ApplyAutoCliffTexture(startX, startZ, width, height);
    }
}