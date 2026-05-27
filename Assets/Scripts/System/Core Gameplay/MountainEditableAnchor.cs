using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class MountainEditableAnchor : MonoBehaviour
{
    private TerrainModifierHandler terrainHandler;
    private Vector3 lastPosition;

    [Header("📐 Mountain Dimensions")]
    [SerializeField] private float mountainBaseSize = 8f;
    [SerializeField] private float mountainHeightMultiplier = 1f;
    private float mountainSpeed = 5f;
    private bool isPlacedSuccessfully = false;

    private LineRenderer lineRenderer;

    [Header("🚀 Handle Height Offsets")]
    [SerializeField] private float topHandleHeightOffset = 4.5f;
    [SerializeField] private float radiusHandleHeightOffset = 3.0f;

    [Header("✨ Visualizer & Fade Settings")]
    [SerializeField] private Material lineMaterial;
    [SerializeField] private Color normalColor = new Color(1f, 0.6f, 0f, 0.4f);
    [SerializeField] private Color highlightColor = new Color(1f, 0.9f, 0.2f, 0.9f);
    [SerializeField] private float fadeSpeed = 10f;
    [SerializeField] private int circleSegments = 40;

    [Header("🧱 Custom Cozy Handle Prefabs")]
    [SerializeField] private GameObject topHandlePrefab;
    [SerializeField] private GameObject radiusHandlePrefab;

    private Transform topHandleInstance;
    private Transform radiusHandleInstance;
    private Renderer topRenderer;
    private Renderer radiusRenderer;

    private Color currentRuntimeColor;
    private Vector3 targetHandleScale = Vector3.zero;
    private Vector3 currentHandleScale = Vector3.zero;

    private bool isSelected = false;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.startWidth = 0.15f;
        lineRenderer.endWidth = 0.15f;
        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = true;

        if (lineMaterial != null) lineRenderer.material = lineMaterial;

        currentRuntimeColor = normalColor;
        CreateCozyHandlesFromPrefabs();
    }

    private void LateUpdate()
    {
        if (isPlacedSuccessfully)
        {
            PositionHandles();
            AnimateGizmoVisuals();
        }
    }

    private void AnimateGizmoVisuals()
    {
        Color targetColor = isSelected ? highlightColor : normalColor;
        targetHandleScale = isSelected ? Vector3.one : Vector3.zero;

        currentRuntimeColor = Color.Lerp(currentRuntimeColor, targetColor, Time.deltaTime * fadeSpeed);
        currentHandleScale = Vector3.Lerp(currentHandleScale, targetHandleScale, Time.deltaTime * fadeSpeed);

        if (lineRenderer != null)
        {
            lineRenderer.startColor = currentRuntimeColor;
            lineRenderer.endColor = currentRuntimeColor;
        }

        if (topHandleInstance != null)
        {
            topHandleInstance.localScale = currentHandleScale;
            if (topRenderer != null) topRenderer.material.color = currentRuntimeColor;
        }

        if (radiusHandleInstance != null)
        {
            radiusHandleInstance.localScale = currentHandleScale;
            if (radiusRenderer != null) radiusRenderer.material.color = currentRuntimeColor;
        }
    }

    public void SetupAnchor(TerrainModifierHandler handler, float size, float speed)
    {
        terrainHandler = handler;
        mountainBaseSize = size;
        mountainSpeed = speed;
        lastPosition = transform.position;
        isPlacedSuccessfully = true;

        SelectMountain(true);
        UpdateScale(mountainBaseSize, mountainHeightMultiplier);
    }

    public void UpdateScale(float newSize, float newHeightMultiplier)
    {
        if (terrainHandler == null || !isPlacedSuccessfully) return;

        terrainHandler.FlattenMountainAtPosition(transform.position, mountainBaseSize);
        mountainBaseSize = Mathf.Max(newSize, 1f);
        mountainHeightMultiplier = Mathf.Max(newHeightMultiplier, 0.1f);
        terrainHandler.CreateMountainAtPositionDynamic(transform.position, mountainBaseSize, mountainSpeed, mountainHeightMultiplier);

        DrawRuntimeCircle();
    }

    private void CreateCozyHandlesFromPrefabs()
    {
        if (topHandlePrefab != null)
        {
            GameObject topGo = Instantiate(topHandlePrefab);
            topGo.transform.SetParent(transform);
            topGo.transform.localPosition = Vector3.zero;
            topGo.transform.localRotation = Quaternion.identity;
            topGo.transform.localScale = Vector3.zero;

            topHandleInstance = topGo.transform;
            topRenderer = topGo.GetComponent<Renderer>();

            var topDrag = topGo.GetComponent<LocalMountainHandleDragger>();
            if (topDrag == null) topDrag = topGo.AddComponent<LocalMountainHandleDragger>();
            topDrag.Initialize(this, true);
        }

        if (radiusHandlePrefab != null)
        {
            GameObject radGo = Instantiate(radiusHandlePrefab);
            radGo.transform.SetParent(transform);
            radGo.transform.localPosition = Vector3.zero;
            radGo.transform.localRotation = Quaternion.identity;
            radGo.transform.localScale = Vector3.zero;

            radiusHandleInstance = radGo.transform;
            radiusRenderer = radGo.GetComponent<Renderer>();

            var radDrag = radGo.GetComponent<LocalMountainHandleDragger>();
            if (radDrag == null) radDrag = radGo.AddComponent<LocalMountainHandleDragger>();
            radDrag.Initialize(this, false);
        }
    }

    public void SelectMountain(bool state)
    {
        isSelected = state;
        if (isSelected)
        {
            Debug.Log($"<color=gold><b>[Mountain Selected]</b></color> เปิดระบบแกนควบคุมปรับสเกลของภูเขาพิกัด {transform.position} แล้วนะปิ๊บ!");
        }
    }

    private void PositionHandles()
    {
        if (topHandleInstance == null || radiusHandleInstance == null) return;

        float currentPeakHeight = transform.position.y;
        if (Terrain.activeTerrain != null)
        {
            currentPeakHeight = Terrain.activeTerrain.SampleHeight(transform.position) + Terrain.activeTerrain.transform.position.y;
        }

        topHandleInstance.position = new Vector3(transform.position.x, currentPeakHeight + topHandleHeightOffset, transform.position.z);

        Vector3 radiusEdgePos = transform.position + new Vector3(mountainBaseSize, 0f, 0f);
        if (Terrain.activeTerrain != null)
        {
            radiusEdgePos.y = Terrain.activeTerrain.SampleHeight(radiusEdgePos) + Terrain.activeTerrain.transform.position.y;
        }

        radiusHandleInstance.position = radiusEdgePos + new Vector3(0f, radiusHandleHeightOffset, 0f);
    }

    private void DrawRuntimeCircle()
    {
        if (lineRenderer == null) return;
        lineRenderer.positionCount = circleSegments;
        Vector3 center = transform.position;

        for (int i = 0; i < circleSegments; i++)
        {
            float angle = (i / (float)circleSegments) * Mathf.PI * 2f;
            float x = Mathf.Cos(angle) * mountainBaseSize;
            float z = Mathf.Sin(angle) * mountainBaseSize;
            Vector3 pointPos = center + new Vector3(x, 0.2f, z);

            if (Terrain.activeTerrain != null)
            {
                pointPos.y = Terrain.activeTerrain.SampleHeight(pointPos) + Terrain.activeTerrain.transform.position.y + 0.1f;
            }
            lineRenderer.SetPosition(i, pointPos);
        }
    }

    public void OnPositionChanged()
    {
        if (terrainHandler == null || !isPlacedSuccessfully) return;
        terrainHandler.FlattenMountainAtPosition(lastPosition, mountainBaseSize);
        terrainHandler.CreateMountainAtPositionDynamic(transform.position, mountainBaseSize, mountainSpeed, mountainHeightMultiplier);
        lastPosition = transform.position;
        UpdateScale(mountainBaseSize, mountainHeightMultiplier);
    }

    public void DemolishMountain()
    {
        if (terrainHandler != null) terrainHandler.FlattenMountainAtPosition(transform.position, mountainBaseSize);
        isPlacedSuccessfully = false;
        Destroy(gameObject);
    }

    public float GetCurrentSize() => mountainBaseSize;
    public float GetCurrentHeightMultiplier() => mountainHeightMultiplier;
    public bool IsSelected() => isSelected;
}