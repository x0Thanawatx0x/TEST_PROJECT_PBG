// ==============================
// EditTransformHandler.cs
// ==============================

using UnityEngine;
using UnityEngine.EventSystems;

public class EditTransformHandler : MonoBehaviour
{
    [Header("Gizmo Settings")]
    // บ้าน ใช้ prefab นี้
    [SerializeField] private GameObject scalingGizmoPrefab;
    // เฟอร์นิเจอร์ ใช้ prefab นี้
    [SerializeField] private GameObject furnitureGizmoPrefab;
    // wall node (pillar) ใช้ prefab นี้
    [SerializeField] private GameObject wallNodeGizmoPrefab;

    [SerializeField]
    private float gizmoVerticalOffset = 0.05f;

    // offset แยกสำหรับ wall node
    [SerializeField]
    private float wallNodeGizmoVerticalOffset = 1.0f;

    [SerializeField]
    private float scaleSensitivity = 2f;

    [SerializeField]
    private float rotationSensitivity = 150f;

    // highlight เฉพาะ object ที่ไม่มี ObjectColorHandler
    [SerializeField]
    private Color highlightColor =
        new Color(1, 0.9f, 0, 0.5f);

    [Header("Gizmo Appearance Settings")]
    [SerializeField] private float gizmoScale = 1.0f;

    [SerializeField]
    private float minGizmoScale = 0.3f;

    [SerializeField]
    private float maxGizmoScale = 5.0f;

    [SerializeField]
    private float gizmoScaleStep = 0.2f;

    [Header("Wall Settings")]
    [SerializeField]
    private string wallTag = "Wall";

    [Header("Color Picker")]
    // ลาก ColorPickerUI มาใส่ — ถ้าไม่มีก็ไม่ error
    [SerializeField]
    private ColorPickerUI colorPickerUI;

    private PlacementSystem system;

    private GameObject activeGizmo;
    private GameObject houseGizmoInstance;
    private GameObject furnitureGizmoInstance;
    private GameObject wallNodeGizmoInstance;

    private GameObject selectedObject;

    private Color originalColor;
    private bool didHighlight = false;

    private string currentDraggingAxis = "";

    private Vector3 lastMousePos;
    private Vector3 mouseOffset;

    private float lastGroundY;

    private SplinePlacementHandler splineHandler;

    public bool IsEditing => selectedObject != null;

    public void Initialize(PlacementSystem sys)
    {
        system = sys;

        if (scalingGizmoPrefab)
        {
            houseGizmoInstance =
                Instantiate(scalingGizmoPrefab);

            houseGizmoInstance.SetActive(false);
        }

        if (furnitureGizmoPrefab)
        {
            furnitureGizmoInstance =
                Instantiate(furnitureGizmoPrefab);

            furnitureGizmoInstance.SetActive(false);
        }

        if (wallNodeGizmoPrefab)
        {
            wallNodeGizmoInstance =
                Instantiate(wallNodeGizmoPrefab);

            wallNodeGizmoInstance.SetActive(false);
        }

        splineHandler =
            system.GetComponent<SplinePlacementHandler>();

        Debug.Log("[EDIT] Initialized");
    }

    public void StartEditing(GameObject obj)
    {
        if (obj == null)
            return;

        StopEditing();

        selectedObject = obj;
        didHighlight = false;

        lastGroundY =
            obj.transform.position.y;

        // เลือก gizmo ตาม tag
        if (obj.CompareTag(wallTag))
            activeGizmo = wallNodeGizmoInstance;
        else if (obj.CompareTag("Furniture"))
            activeGizmo = furnitureGizmoInstance;
        else
            activeGizmo = houseGizmoInstance;

        if (activeGizmo)
        {
            activeGizmo.SetActive(true);
            UpdateGizmoPosition();
        }

        // highlight เฉพาะ object ที่ไม่มี ObjectColorHandler
        // ถ้ามี ObjectColorHandler (บ้าน) → ไม่แตะสี
        // เพื่อไม่ทับสีที่ผู้ใช้เลือกไว้
        ObjectColorHandler colorHandler =
            obj.GetComponent<ObjectColorHandler>();

        if (colorHandler == null)
        {
            Renderer r =
                obj.GetComponentInChildren<Renderer>();

            if (r != null)
            {
                originalColor = r.material.color;
                r.material.color = highlightColor;
                didHighlight = true;
            }
        }

        lastMousePos = Input.mousePosition;

        // แสดง color picker เฉพาะ TinyHouse
        if (colorPickerUI != null)
        {
            if (obj.CompareTag("TinyHouse"))
                colorPickerUI.ShowFor(obj);
            else
                colorPickerUI.Hide();
        }

        Debug.Log("[SELECTED] => " + selectedObject.name);
    }

    public void HandleEditUpdate(Camera cam)
    {
        if (selectedObject == null)
            return;

        HandleGizmoResizing();

        Ray ray =
            cam.ScreenPointToRay(Input.mousePosition);

        Vector3 mouseDelta =
            Input.mousePosition - lastMousePos;

        bool isWallNode =
            selectedObject.CompareTag(wallTag);

        // ---- START DRAG ----
        if (Input.GetMouseButtonDown(0))
        {
            // ถ้า pointer อยู่บน UI (เช่น กดปุ่มสี) → ข้ามทั้งหมด
            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject())
            {
                lastMousePos = Input.mousePosition;
                return;
            }

            RaycastHit[] hits =
                Physics.RaycastAll(ray, Mathf.Infinity);

            System.Array.Sort(
                hits,
                (a, b) => a.distance.CompareTo(b.distance));

            bool hitSelf = false;

            foreach (RaycastHit hit in hits)
            {
                GameObject hitObj =
                    hit.collider.transform.root.gameObject;

                Debug.Log("[RAY HIT] => " + hitObj.name);

                // ---- เช็ค gizmo handle ก่อน ----
                // ถ้าคลิกโดน GizmoAxisHandle → เซ็ต axis
                GizmoAxisHandle handle =
                    hit.collider
                        .GetComponentInParent<GizmoAxisHandle>();

                if (handle != null &&
                    activeGizmo != null &&
                    hit.transform.IsChildOf(activeGizmo.transform))
                {
                    currentDraggingAxis = handle.axisName;

                    mouseOffset =
                        selectedObject.transform.position
                        - hit.point;

                    mouseOffset.y = 0;

                    hitSelf = true;

                    Debug.Log(
                        "[DRAG AXIS] => " +
                        handle.axisName);

                    break;
                }

                // ---- คลิกโดน object ตัวเอง → Move ----
                if (hitObj == selectedObject ||
                    hit.transform.IsChildOf(
                        selectedObject.transform))
                {
                    currentDraggingAxis = "Move";

                    mouseOffset =
                        selectedObject.transform.position
                        - hit.point;

                    mouseOffset.y = 0;

                    hitSelf = true;

                    Debug.Log(
                        "[DRAG START] => " +
                        selectedObject.name);

                    break;
                }
            }

            // คลิกที่อื่น → เปลี่ยน selection หรือ deselect
            if (!hitSelf)
            {
                TrySelectOther(cam);
                return;
            }
        }

        // ---- END DRAG ----
        if (Input.GetMouseButtonUp(0))
        {
            Debug.Log("[DRAG END]");
            currentDraggingAxis = "";
        }

        // ---- DRAGGING ----
        if (currentDraggingAxis != "")
        {
            if (currentDraggingAxis == "Move")
            {
                if (Physics.Raycast(
                    ray,
                    out RaycastHit hit,
                    Mathf.Infinity,
                    system.groundLayer))
                {
                    lastGroundY = hit.point.y;

                    if (isWallNode && splineHandler != null)
                    {
                        Vector3 targetPosition =
                            hit.point + mouseOffset;

                        targetPosition.y = hit.point.y;

                        Debug.Log(
                            "[MOVE NODE] => " +
                            targetPosition);

                        splineHandler.MoveNodeDynamicCheck(
                            selectedObject,
                            targetPosition);
                    }
                    else
                    {
                        Vector3 targetPosition =
                            system.SnapToGrid(
                                hit.point + mouseOffset);

                        targetPosition.y = hit.point.y;

                        selectedObject.transform.position =
                            Vector3.MoveTowards(
                                selectedObject.transform.position,
                                targetPosition,
                                50f * Time.deltaTime);
                    }
                }
            }
            else if (currentDraggingAxis == "Axis_Rotate")
            {
                if (!isWallNode)
                {
                    selectedObject.transform.Rotate(
                        Vector3.up,
                        -mouseDelta.x *
                        rotationSensitivity *
                        Time.deltaTime,
                        Space.World);
                }
            }
            else
            {
                // Axis_X, Axis_Y, Axis_Z, Axis_Uniform
                if (!isWallNode)
                {
                    HandleScaling(mouseDelta);
                }
            }

            UpdateGizmoPosition();
        }

        lastMousePos = Input.mousePosition;

        // คลิกขวา → หยุด edit
        if (Input.GetMouseButtonDown(1))
        {
            StopEditing();
        }
    }

    // คลิกที่ object อื่นขณะ edit อยู่ → เปลี่ยน selection ได้เลย
    private void TrySelectOther(Camera cam)
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        RaycastHit[] hits = Physics.RaycastAll(ray, 500f);
        System.Array.Sort(
            hits,
            (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            GameObject clicked = hit.collider.gameObject;

            if (clicked.CompareTag(wallTag))
            {
                StartEditing(clicked);
                return;
            }

            GameObject target = clicked;
            if (target.transform.parent != null)
                target = target.transform.parent.gameObject;

            if (target.CompareTag("TinyHouse") ||
                target.CompareTag("Furniture") ||
                target.CompareTag("Player"))
            {
                StartEditing(target);
                return;
            }
        }

        // คลิกโดน ground หรือ empty → หยุด edit
        StopEditing();
    }

    private void HandleGizmoResizing()
    {
        bool sizeChanged = false;

        if (Input.GetKeyDown(KeyCode.Equals) ||
            Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            gizmoScale =
                Mathf.Clamp(
                    gizmoScale + gizmoScaleStep,
                    minGizmoScale,
                    maxGizmoScale);

            sizeChanged = true;
        }

        if (Input.GetKeyDown(KeyCode.Minus) ||
            Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            gizmoScale =
                Mathf.Clamp(
                    gizmoScale - gizmoScaleStep,
                    minGizmoScale,
                    maxGizmoScale);

            sizeChanged = true;
        }

        if (sizeChanged)
            UpdateGizmoPosition();
    }

    private void HandleScaling(Vector3 delta)
    {
        float s =
            (delta.x + delta.y)
            * scaleSensitivity
            * Time.deltaTime;

        Vector3 scale =
            selectedObject.transform.localScale;

        if (currentDraggingAxis == "Axis_X")
            scale.x += s;
        else if (currentDraggingAxis == "Axis_Y")
            scale.y += s;
        else if (currentDraggingAxis == "Axis_Z")
            scale.z += s;
        else if (currentDraggingAxis == "Axis_Uniform")
            scale += Vector3.one * s;

        selectedObject.transform.localScale =
            new Vector3(
                Mathf.Max(scale.x, 0.1f),
                Mathf.Max(scale.y, 0.1f),
                Mathf.Max(scale.z, 0.1f));
    }

    public void UpdateGizmoPosition()
    {
        if (activeGizmo && selectedObject)
        {
            float offset =
                selectedObject.CompareTag(wallTag)
                ? wallNodeGizmoVerticalOffset
                : gizmoVerticalOffset;

            activeGizmo.transform.position =
                selectedObject.transform.position +
                Vector3.up * offset;

            activeGizmo.transform.rotation =
                selectedObject.transform.rotation;

            activeGizmo.transform.localScale =
                Vector3.one * gizmoScale;
        }
    }

    public void StopEditing()
    {
        if (selectedObject != null)
        {
            // reset highlight เฉพาะ object ที่เราได้ highlight ไว้
            // object ที่มี ObjectColorHandler จะไม่ถูกแตะสีเลย
            if (didHighlight)
            {
                Renderer r =
                    selectedObject
                        .GetComponentInChildren<Renderer>();

                if (r != null)
                    r.material.color = originalColor;
            }

            Debug.Log("[STOP EDITING] => " + selectedObject.name);
        }

        if (activeGizmo)
            activeGizmo.SetActive(false);

        selectedObject = null;
        currentDraggingAxis = "";
        didHighlight = false;

        if (colorPickerUI != null)
            colorPickerUI.Hide();
    }
}