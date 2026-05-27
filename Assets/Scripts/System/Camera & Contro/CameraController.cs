using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Movement (WASD)")]
    public float moveSpeed = 15f;

    [Header("Rotation (Right Click)")]
    public float sensitivity = 2f;
    public float minViewAngle = -60f;
    public float maxViewAngle = 80f;

    [Header("Zoom (Mouse Wheel)")]
    public float zoomSpeed = 20f;
    public float minZoomDistance = 2f;
    public float maxZoomDistance = 200f;

    private float rotationX = 0f;
    private float rotationY = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Vector3 rot = transform.localRotation.eulerAngles;
        rotationX = rot.y;
        rotationY = rot.x;
    }

    void Update()
    {
        HandleRotation();
        HandleMovement();
        HandleZoom();
    }

    void HandleMovement()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 forward = transform.forward;
        forward.y = 0;

        Vector3 right = transform.right;
        right.y = 0;

        Vector3 moveDir = (forward.normalized * v) + (right.normalized * h);
        transform.position += moveDir * moveSpeed * Time.deltaTime;
    }

    void HandleRotation()
    {
        // 🎯 [ADDED] จังหวะเฟรมแรกสุดที่กดคลิกขวาลงไปดื้อๆ ให้ปล่อยโฟกัสวัตถุชั่วคราวเพื่อความปลอดภัย
        if (Input.GetMouseButtonDown(1))
        {
            Ray rayCheck = Camera.main.ScreenPointToRay(Input.mousePosition);
            Physics.Raycast(rayCheck, out RaycastHit hit, Mathf.Infinity);
        }

        // 🎯 เมื่อผู้เล่นกด "คลิกขวาค้างไว้" (ต้องการหมุนมุมกล้อง)
        if (Input.GetMouseButton(1))
        {
            // ✨ [ADDED SAFETY FIX] บังคับล็อกเมาส์ไว้กลางจอชั่วคราว เพื่อดึงค่าอินพุตให้ไม่เป็น 0 ทื่อๆ
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            float mouseX = Input.GetAxis("Mouse X") * sensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

            rotationX += mouseX;
            rotationY -= mouseY;
            rotationY = Mathf.Clamp(rotationY, minViewAngle, maxViewAngle);

            transform.rotation = Quaternion.Euler(rotationY, rotationX, 0);
        }
        // 🎯 [ADDED] เมื่อผู้เล่น "ปล่อยนิ้ว" จากคลิกขวา ให้คืนเมาส์กลับมาลอยอิสระทันที
        else if (Input.GetMouseButtonUp(1))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll == 0) return;

        Vector3 direction = transform.forward;
        float moveAmount = scroll * zoomSpeed;
        Vector3 targetPosition = transform.position + direction * moveAmount;

        // =================================================
        // 🌿 TINY GLADE STYLE COLLISION (ลอจิกดั้งเดิมของปิ๊บ)
        // =================================================
        Ray ray = new Ray(transform.position, direction);
        RaycastHit hit;
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);

        if (Physics.Raycast(ray, out hit, distanceToTarget))
        {
            float safeDistance = hit.distance - 0.3f;
            if (safeDistance < 0f)
                safeDistance = 0f;

            targetPosition = transform.position + direction * Mathf.Min(moveAmount, safeDistance);
        }

        transform.position = targetPosition;

        // =================================================
        // HEIGHT CLAMP (กันหลุดโลก - ลоจิกดั้งเดิมของปิ๊บ)
        // =================================================
        float clampedY = Mathf.Clamp(transform.position.y, minZoomDistance, maxZoomDistance);
        transform.position = new Vector3(
            transform.position.x,
            clampedY,
            transform.position.z
        );
    }
}