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
    public float minFOV = 20f;
    public float maxFOV = 60f;

    private float rotationX = 0f;
    private float rotationY = 0f;
    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();

        // ✅ เริ่มต้นแบบโชว์เมาส์ปกติ (ไม่ล็อค)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // ตั้งค่าองศาเริ่มต้น
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
        // ✅ ใช้ WASD ในการ Pan กล้อง (เลื่อนไปตามระนาบ)
        float h = Input.GetAxis("Horizontal"); // A D
        float v = Input.GetAxis("Vertical");   // W S

        // คำนวณทิศทางโดยให้เลื่อนขนานไปกับพื้น (y = 0)
        Vector3 forward = transform.forward;
        forward.y = 0;
        Vector3 right = transform.right;
        right.y = 0;

        Vector3 moveDir = (forward.normalized * v) + (right.normalized * h);
        transform.position += moveDir * moveSpeed * Time.deltaTime;
    }

    void HandleRotation()
    {
        // ✅ ต้องคลิกขวา (MouseButton 1) ค้างไว้เท่านั้นถึงจะหมุนกล้องได้
        if (Input.GetMouseButton(1))
        {
            // ซ่อนเมาส์ตอนกำลังหมุน (Optional: ถ้าอยากให้เมาส์หายไปตอนหมุนให้ปลดคอมเมนต์)
            // Cursor.visible = false;

            float mouseX = Input.GetAxis("Mouse X") * sensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

            rotationX += mouseX;
            rotationY -= mouseY;
            rotationY = Mathf.Clamp(rotationY, minViewAngle, maxViewAngle);

            transform.rotation = Quaternion.Euler(rotationY, rotationX, 0);
        }
        else
        {
            // ปล่อยคลิกขวาแล้วโชว์เมาส์
            // Cursor.visible = true;
        }
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            // ซูมแบบเลื่อนตัวกล้องเข้าไปหาจุดที่มอง (ถนัดมือกว่าสำหรับแนว Builder)
            Vector3 zoomDir = transform.forward * scroll * zoomSpeed;
            transform.position += zoomDir;

            // (Optional) ถ้ากลัวกล้องทะลุพื้น ใส่ Clamp ระยะความสูง Y ไว้ได้ครับ
            // float clampedY = Mathf.Clamp(transform.position.y, 5f, 50f);
            // transform.position = new Vector3(transform.position.x, clampedY, transform.position.z);
        }
    }
}