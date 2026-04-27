using UnityEngine;
using System.Collections.Generic;

public class PlacementSystem : MonoBehaviour
{
    [Header("General Settings")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask buildableLayer;
    [SerializeField] private float gridSize = 1f;
    [SerializeField] private ToolManager toolManager;

    [Header("House (Key 1)")]
    [SerializeField] private GameObject housePrefab;
    [SerializeField] private GameObject housePreview;

    [Header("Road Painting (Key 2)")]
    [SerializeField] private int roadLayerIndex = 1;
    [SerializeField] private float paintOpacity = 1f;

    [Header("Furniture List (Key 3)")]
    [SerializeField] private List<GameObject> furniturePrefabs;
    [SerializeField] private List<GameObject> furniturePreviews;

    [Header("Nature List (Key 5)")]
    [SerializeField] private List<GameObject> naturePrefabs;
    [SerializeField] private List<GameObject> naturePreviews;

    [Header("Pond (Key 6)")]
    [SerializeField] private Terrain terrain;
    [SerializeField] private float brushSize = 2f;
    [SerializeField] private float moldSpeed = 5f;
    [SerializeField] private GameObject pondPreview;

    private Camera mainCam;
    private float currentRotation = 0f;

    // ✅ เพิ่ม: เก็บ terrain เดิม
    private float[,] originalHeights;

    void Start()
    {
        mainCam = Camera.main;

        // ✅ เก็บค่าพื้นเดิม
        if (terrain != null)
        {
            originalHeights = terrain.terrainData.GetHeights(
                0, 0,
                terrain.terrainData.heightmapResolution,
                terrain.terrainData.heightmapResolution
            );
        }

        if (housePreview) housePreview = Instantiate(housePreview);
        if (pondPreview) pondPreview = Instantiate(pondPreview);

        for (int i = 0; i < furniturePreviews.Count; i++)
            if (furniturePreviews[i]) furniturePreviews[i] = Instantiate(furniturePreviews[i]);

        for (int i = 0; i < naturePreviews.Count; i++)
            if (naturePreviews[i]) naturePreviews[i] = Instantiate(naturePreviews[i]);

        HideAllPreviews();
    }

    void Update()
    {
        if (toolManager == null) return;
        if (Input.GetKeyDown(KeyCode.R)) currentRotation += 90f;

        HideAllPreviews();

        switch (toolManager.currentTool)
        {
            case ToolManager.BuildTool.House: HandleSinglePlacement(housePrefab, housePreview); break;
            case ToolManager.BuildTool.Road: HandleRoadPainting(); break;
            case ToolManager.BuildTool.Furniture: HandleMultiPlacement(furniturePrefabs, furniturePreviews, toolManager.furnitureIndex); break;
            case ToolManager.BuildTool.Nature: HandleMultiPlacement(naturePrefabs, naturePreviews, toolManager.natureIndex); break;
            case ToolManager.BuildTool.Pond: HandlePondSystem(); break;
            case ToolManager.BuildTool.Eraser: HandleEraser(); break;
        }
    }

    // ✅ ยางลบอัปเกรด
    void HandleEraser()
    {
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayer))
        {
            if (pondPreview)
            {
                pondPreview.SetActive(true);
                pondPreview.transform.position = hit.point + Vector3.up * 0.1f;
                pondPreview.transform.localScale = new Vector3(brushSize * 2, 1, brushSize * 2);
            }

            if (Input.GetMouseButton(0))
            {
                EraseRoad(hit.point);
                FlattenTerrain(hit.point); // 🔥 ใช้ของใหม่
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, buildableLayer))
            {
                Destroy(hit.collider.gameObject);
                Debug.Log("ลบวัตถุแล้วครับปิ๊บ!");
            }
        }
    }

    // 🌱 ลบถนน
    void EraseRoad(Vector3 worldPos)
    {
        if (!terrain) return;

        TerrainData td = terrain.terrainData;

        int mapX = Mathf.RoundToInt((worldPos.x - terrain.transform.position.x) / td.size.x * td.alphamapWidth);
        int mapZ = Mathf.RoundToInt((worldPos.z - terrain.transform.position.z) / td.size.z * td.alphamapHeight);

        int radius = Mathf.RoundToInt(brushSize);

        int startX = Mathf.Clamp(mapX - radius, 0, td.alphamapWidth - 1);
        int startZ = Mathf.Clamp(mapZ - radius, 0, td.alphamapHeight - 1);

        int width = Mathf.Clamp(radius * 2, 1, td.alphamapWidth - startX);
        int height = Mathf.Clamp(radius * 2, 1, td.alphamapHeight - startZ);

        float[,,] maps = td.GetAlphamaps(startX, startZ, width, height);

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                for (int k = 0; k < td.alphamapLayers; k++)
                    maps[y, x, k] = (k == 0) ? 1 : 0;

        td.SetAlphamaps(startX, startZ, maps);
    }

    // 💧 ลบบ่อ → กลับ original จริง
    void FlattenTerrain(Vector3 point)
    {
        if (!terrain || originalHeights == null) return;

        TerrainData td = terrain.terrainData;

        int mapX = Mathf.RoundToInt((point.x - terrain.transform.position.x) / td.size.x * td.heightmapResolution);
        int mapZ = Mathf.RoundToInt((point.z - terrain.transform.position.z) / td.size.z * td.heightmapResolution);

        int r = (int)brushSize;

        int startX = Mathf.Clamp(mapX - r, 0, td.heightmapResolution - 1);
        int startZ = Mathf.Clamp(mapZ - r, 0, td.heightmapResolution - 1);

        int w = Mathf.Clamp(r * 2, 1, td.heightmapResolution - startX);
        int h = Mathf.Clamp(r * 2, 1, td.heightmapResolution - startZ);

        float[,] heights = td.GetHeights(startX, startZ, w, h);

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                heights[y, x] = originalHeights[startZ + y, startX + x];

        td.SetHeights(startX, startZ, heights);
    }

    // --- ของเดิมทั้งหมด ---
    void HandleSinglePlacement(GameObject prefab, GameObject preview)
    {
        if (!prefab || !preview) return;
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer))
        {
            preview.SetActive(true);
            Vector3 pos = SnapToGrid(hit.point);
            preview.transform.position = pos;
            preview.transform.rotation = Quaternion.Euler(0, currentRotation, 0);
            if (Input.GetMouseButtonDown(0)) SpawnObject(prefab, pos);
        }
    }

    void HandleMultiPlacement(List<GameObject> prefabs, List<GameObject> previews, int subIndex)
    {
        if (prefabs.Count == 0 || previews.Count == 0) return;
        int index = subIndex % prefabs.Count;
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer))
        {
            GameObject preview = previews[index];
            if (preview)
            {
                preview.SetActive(true);
                Vector3 pos = SnapToGrid(hit.point);
                preview.transform.position = pos;
                preview.transform.rotation = Quaternion.Euler(0, currentRotation, 0);
                if (Input.GetMouseButtonDown(0)) SpawnObject(prefabs[index], pos);
            }
        }
    }

    void SpawnObject(GameObject prefab, Vector3 pos)
    {
        GameObject newObj = Instantiate(prefab, pos, Quaternion.Euler(0, currentRotation, 0));
        newObj.layer = LayerMaskToLayer(buildableLayer);
    }

    void HandleRoadPainting() { Ray ray = mainCam.ScreenPointToRay(Input.mousePosition); if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer)) { if (pondPreview) { pondPreview.SetActive(true); pondPreview.transform.position = hit.point + Vector3.up * 0.1f; pondPreview.transform.localScale = new Vector3(brushSize * 2, 1, brushSize * 2); } if (Input.GetMouseButton(0)) PaintTerrain(hit.point); } }
    void PaintTerrain(Vector3 worldPos) { if (!terrain) return; TerrainData td = terrain.terrainData; int mapX = Mathf.RoundToInt((worldPos.x - terrain.transform.position.x) / td.size.x * td.alphamapWidth); int mapZ = Mathf.RoundToInt((worldPos.z - terrain.transform.position.z) / td.size.z * td.alphamapHeight); int radius = Mathf.RoundToInt(brushSize); int startX = Mathf.Clamp(mapX - radius, 0, td.alphamapWidth - 1); int startZ = Mathf.Clamp(mapZ - radius, 0, td.alphamapHeight - 1); int width = Mathf.Clamp(radius * 2, 1, td.alphamapWidth - startX); int height = Mathf.Clamp(radius * 2, 1, td.alphamapHeight - startZ); float[,,] maps = td.GetAlphamaps(startX, startZ, width, height); for (int y = 0; y < height; y++) for (int x = 0; x < width; x++) for (int k = 0; k < td.alphamapLayers; k++) maps[y, x, k] = (k == roadLayerIndex) ? paintOpacity : 0; td.SetAlphamaps(startX, startZ, maps); }
    void HandlePondSystem() { Ray ray = mainCam.ScreenPointToRay(Input.mousePosition); if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer)) { if (pondPreview) { pondPreview.SetActive(true); pondPreview.transform.position = hit.point + Vector3.up * 0.1f; pondPreview.transform.localScale = new Vector3(brushSize * 2, 1, brushSize * 2); } if (Input.GetMouseButton(0)) ModifyTerrain(hit.point); } }
    Vector3 SnapToGrid(Vector3 point) { float x = Mathf.Round(point.x / gridSize) * gridSize; float z = Mathf.Round(point.z / gridSize) * gridSize; return new Vector3(x, point.y, z); }
    void ModifyTerrain(Vector3 point) { if (!terrain) return; TerrainData td = terrain.terrainData; int mapX = Mathf.RoundToInt((point.x - terrain.transform.position.x) / td.size.x * td.heightmapResolution); int mapZ = Mathf.RoundToInt((point.z - terrain.transform.position.z) / td.size.z * td.heightmapResolution); int r = (int)brushSize; int startX = Mathf.Clamp(mapX - r, 0, td.heightmapResolution - 1); int startZ = Mathf.Clamp(mapZ - r, 0, td.heightmapResolution - 1); int w = Mathf.Clamp(r * 2, 1, td.heightmapResolution - startX); int h = Mathf.Clamp(r * 2, 1, td.heightmapResolution - startZ); float[,] heights = td.GetHeights(startX, startZ, w, h); for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) { heights[y, x] -= moldSpeed * 0.01f * Time.deltaTime; if (heights[y, x] < 0) heights[y, x] = 0; } td.SetHeights(startX, startZ, heights); }
    void HideAllPreviews() { if (housePreview) housePreview.SetActive(false); if (pondPreview) pondPreview.SetActive(false); foreach (var p in furniturePreviews) if (p) p.SetActive(false); foreach (var p in naturePreviews) if (p) p.SetActive(false); }

    private int LayerMaskToLayer(LayerMask layerMask)
    {
        int layerNumber = 0;
        int layer = layerMask.value;
        while (layer > 0) { layer >>= 1; layerNumber++; }
        return layerNumber - 1;
    }
}