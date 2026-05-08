using UnityEngine;

public class EditTransformHandler : MonoBehaviour
{
    [Header("Gizmo Settings")]
    [SerializeField] private GameObject scalingGizmoPrefab;
    [SerializeField] private GameObject furnitureGizmoPrefab;
    [SerializeField] private float gizmoVerticalOffset = 0.05f;
    [SerializeField] private float scaleSensitivity = 2f;
    [SerializeField] private float rotationSensitivity = 150f;
    [SerializeField] private Color highlightColor = new Color(1, 0.9f, 0, 0.5f);

    [Header("Gizmo Appearance Settings")]
    [SerializeField] private float gizmoScale = 1.0f; // ขนาดปัจจุบันของตัว Gizmo
    [SerializeField] private float minGizmoScale = 0.3f; // ขนาดเล็กสุด
    [SerializeField] private float maxGizmoScale = 5.0f; // ขนาดใหญ่สุด
    [SerializeField] private float gizmoScaleStep = 0.2f; // เพิ่ม/ลดทีละเท่าไหร่

    private PlacementSystem system;
    private GameObject activeGizmo, houseGizmoInstance, furnitureGizmoInstance;
    private GameObject selectedObject;
    private Color originalColor;
    private string currentDraggingAxis = "";
    private Vector3 lastMousePos, mouseOffset;
    private float lastGroundY;

    public bool IsEditing => selectedObject != null;

    public void Initialize(PlacementSystem sys)
    {
        system = sys;
        if (scalingGizmoPrefab) { houseGizmoInstance = Instantiate(scalingGizmoPrefab); houseGizmoInstance.SetActive(false); }
        if (furnitureGizmoPrefab) { furnitureGizmoInstance = Instantiate(furnitureGizmoPrefab); furnitureGizmoInstance.SetActive(false); }
    }

    public void StartEditing(GameObject obj)
    {
        StopEditing();
        selectedObject = obj;
        lastGroundY = obj.transform.position.y;
        activeGizmo = obj.CompareTag("Furniture") ? furnitureGizmoInstance : houseGizmoInstance;

        if (activeGizmo)
        {
            activeGizmo.SetActive(true);
            UpdateGizmoPosition(); // อัปเดตตำแหน่งและขนาดทันที
        }

        Renderer r = selectedObject.GetComponentInChildren<Renderer>();
        if (r != null) { originalColor = r.material.color; r.material.color = highlightColor; }
    }

    public void HandleEditUpdate(Camera cam)
    {
        if (selectedObject == null) return;

        // --- เพิ่มระบบปรับขนาด Gizmo ด้วยปุ่ม + และ - ---
        HandleGizmoResizing();

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Vector3 mouseDelta = Input.mousePosition - lastMousePos;

        if (Input.GetMouseButtonDown(0))
        {
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
            {
                if (hit.collider.name.Contains("Axis")) currentDraggingAxis = hit.collider.name;
                else if (hit.collider.gameObject == selectedObject || hit.transform.IsChildOf(selectedObject.transform))
                {
                    currentDraggingAxis = "Move";
                    mouseOffset = selectedObject.transform.position - hit.point;
                    mouseOffset.y = 0;
                }
                else StopEditing();
            }
        }

        if (Input.GetMouseButtonUp(0)) currentDraggingAxis = "";

        if (currentDraggingAxis != "")
        {
            if (currentDraggingAxis == "Move")
            {
                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, system.groundLayer))
                {
                    lastGroundY = hit.point.y;
                    selectedObject.transform.position = Vector3.Lerp(selectedObject.transform.position, system.SnapToGrid(hit.point + mouseOffset) + Vector3.up * 0.2f, Time.deltaTime * 25f);
                }
            }
            else if (currentDraggingAxis == "Axis_Rotate")
            {
                selectedObject.transform.Rotate(Vector3.up, -mouseDelta.x * rotationSensitivity * Time.deltaTime, Space.World);
            }
            else HandleScaling(mouseDelta);

            UpdateGizmoPosition();
        }

        lastMousePos = Input.mousePosition;
        if (Input.GetMouseButtonDown(1)) StopEditing();
    }

    // ฟังก์ชันใหม่สำหรับตรวจจับการกดปุ่มปรับขนาด Gizmo
    private void HandleGizmoResizing()
    {
        bool sizeChanged = false;

        // ปุ่มบวก (รองรับทั้งแป้นตัวเลขและแป้นปกติ)
        if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            gizmoScale = Mathf.Clamp(gizmoScale + gizmoScaleStep, minGizmoScale, maxGizmoScale);
            sizeChanged = true;
        }

        // ปุ่มลบ
        if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            gizmoScale = Mathf.Clamp(gizmoScale - gizmoScaleStep, minGizmoScale, maxGizmoScale);
            sizeChanged = true;
        }

        if (sizeChanged)
        {
            UpdateGizmoPosition();
        }
    }

    private void HandleScaling(Vector3 delta)
    {
        float s = (delta.x + delta.y) * scaleSensitivity * Time.deltaTime;
        Vector3 scale = selectedObject.transform.localScale;
        if (currentDraggingAxis == "Axis_X") scale.x += s;
        else if (currentDraggingAxis == "Axis_Y") scale.y += s;
        else if (currentDraggingAxis == "Axis_Z") scale.z += s;
        else if (currentDraggingAxis == "Axis_Uniform") scale += Vector3.one * s;

        selectedObject.transform.localScale = new Vector3(Mathf.Max(scale.x, 0.1f), Mathf.Max(scale.y, 0.1f), Mathf.Max(scale.z, 0.1f));
    }

    public void UpdateGizmoPosition()
    {
        if (activeGizmo && selectedObject)
        {
            activeGizmo.transform.position = selectedObject.transform.position + Vector3.up * gizmoVerticalOffset;
            activeGizmo.transform.rotation = selectedObject.transform.rotation;

            // ปรับ Scale ของตัว Gizmo เองตามค่าที่ผู้เล่นกดปรับ
            activeGizmo.transform.localScale = Vector3.one * gizmoScale;
        }
    }

    public void StopEditing()
    {
        if (selectedObject != null)
        {
            selectedObject.transform.position = new Vector3(selectedObject.transform.position.x, lastGroundY, selectedObject.transform.position.z);
            Renderer r = selectedObject.GetComponentInChildren<Renderer>();
            if (r != null) r.material.color = originalColor;
        }
        if (activeGizmo) activeGizmo.SetActive(false);
        selectedObject = null;
        currentDraggingAxis = "";
    }
}