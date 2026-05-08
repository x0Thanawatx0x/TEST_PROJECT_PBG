using UnityEngine;
using System.Collections.Generic;

public class ObjectPlacementHandler : MonoBehaviour
{
    [Header("House Settings")]
    [SerializeField] private List<GameObject> housePrefabs;
    [SerializeField] private List<GameObject> housePreviews;

    [Header("Furniture Settings")]
    [SerializeField] private List<GameObject> furniturePrefabs;
    [SerializeField] private List<GameObject> furniturePreviews;

    [Header("Nature Settings")]
    [SerializeField] private List<GameObject> naturePrefabs;
    [SerializeField] private List<GameObject> naturePreviews;
    [SerializeField] private float natureSpacing = 0.5f;

    private PlacementSystem system;
    private bool isDrawingNature = false;
    private List<Vector3> naturePoints = new List<Vector3>();

    public void Initialize(PlacementSystem sys)
    {
        system = sys;
        // ป้องกัน Error โดยการเช็ค null และ Instantiate Preview ไว้รอ[cite: 1]
        InitializeList(housePreviews);
        InitializeList(furniturePreviews);
        InitializeList(naturePreviews);
        HideAllPreviews();
    }

    private void InitializeList(List<GameObject> previews)
    {
        for (int i = 0; i < previews.Count; i++)
        {
            if (previews[i] != null) previews[i] = Instantiate(previews[i]);
        }
    }

    public void HandleHousePlacement(Camera cam, int index)
    {
        if (housePrefabs.Count == 0) return;
        int i = index % housePrefabs.Count; // ป้องกัน Index Out of Range[cite: 1]

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, system.groundLayer))
        {
            if (housePreviews[i] != null)
            {
                housePreviews[i].SetActive(true);
                Vector3 pos = system.SnapToGrid(hit.point);
                housePreviews[i].transform.position = pos;
                if (Input.GetMouseButtonDown(0)) SpawnObject(housePrefabs[i], pos, "TinyHouse");
            }
        }
    }

    public void HandleMultiPlacement(Camera cam, int index)
    {
        if (furniturePrefabs.Count == 0) return;
        int i = index % furniturePrefabs.Count;
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, system.groundLayer))
        {
            if (furniturePreviews[i] != null)
            {
                furniturePreviews[i].SetActive(true);
                Vector3 pos = system.SnapToGrid(hit.point);
                furniturePreviews[i].transform.position = pos;
                if (Input.GetMouseButtonDown(0)) SpawnObject(furniturePrefabs[i], pos, "Furniture");
            }
        }
    }

    public void HandleNatureSpline(Camera cam, int index)
    {
        if (naturePrefabs.Count == 0) return;
        int i = index % naturePrefabs.Count;
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, system.groundLayer))
        {
            if (naturePreviews[i] != null)
            {
                naturePreviews[i].SetActive(true);
                naturePreviews[i].transform.position = hit.point + Vector3.up * 0.1f;

                if (Input.GetMouseButtonDown(0))
                {
                    isDrawingNature = true;
                    naturePoints.Clear();
                    naturePoints.Add(hit.point);
                    SpawnNature(naturePrefabs[i], hit.point);
                }

                if (isDrawingNature && Input.GetMouseButton(0))
                {
                    if (Vector3.Distance(naturePoints[naturePoints.Count - 1], hit.point) >= natureSpacing)
                    {
                        SpawnNature(naturePrefabs[i], hit.point);
                        naturePoints.Add(hit.point);
                    }
                }
            }
        }
        if (Input.GetMouseButtonUp(0)) isDrawingNature = false;
    }

    private void SpawnObject(GameObject prefab, Vector3 pos, string name)
    {
        GameObject obj = Instantiate(prefab, pos, Quaternion.identity);
        obj.name = name;
        obj.layer = GetLayerIndex(system.buildableLayer);
        if (!obj.GetComponent<Collider>()) obj.AddComponent<BoxCollider>();
    }

    private void SpawnNature(GameObject prefab, Vector3 pos)
    {
        GameObject obj = Instantiate(prefab, pos + Vector3.up * 0.1f, Quaternion.Euler(0, Random.Range(0, 360f), 0));
        obj.transform.localScale *= Random.Range(0.7f, 1.3f);
        obj.layer = GetLayerIndex(system.buildableLayer);
    }

    public void HideAllPreviews()
    {
        foreach (var p in housePreviews) if (p) p.SetActive(false);
        foreach (var p in furniturePreviews) if (p) p.SetActive(false);
        foreach (var p in naturePreviews) if (p) p.SetActive(false);
    }

    private int GetLayerIndex(LayerMask mask) { int v = mask.value; for (int i = 0; i < 32; i++) if (((v >> i) & 1) == 1) return i; return 0; }
}