using UnityEngine;

public class CM_Viewer : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float sprintSpeed = 8f;
    public float mouseSensitivity = 2f;

    [Tooltip("ความสูงของการกระโดด")]
    public float jumpHeight = 1.5f;

    [Header("Physics & Ground Check")]
    public Transform groundCheckPivot; // ✅ ลาก Object "ใต้เท้า" มาใส่ที่นี่ใน Inspector
    public float groundCheckDistance = 0.3f;
    public LayerMask groundLayer;
    public float gravity = -9.81f;

    [Header("Leaning (Q/E)")]
    public float leanAngle = 15f;
    float currentLean = 0f;

    float xRotation = 0f;
    float yRotation = 0f;
    float yVelocity;

    CharacterController controller;

    void Start()
    {
        // อ้างอิง CharacterController จาก Parent ตามโครงสร้างเดิม
        controller = GetComponentInParent<CharacterController>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        yRotation = transform.parent.eulerAngles.y;
    }

    void Update()
    {
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Look();
            Move();
        }
        HandleCursor();
    }

    void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        yRotation += mouseX;

        float targetLean = 0f;
        if (Input.GetKey(KeyCode.Q)) targetLean = leanAngle;
        else if (Input.GetKey(KeyCode.E)) targetLean = -leanAngle;
        currentLean = Mathf.Lerp(currentLean, targetLean, Time.deltaTime * 5f);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, currentLean);
        transform.parent.rotation = Quaternion.Euler(0f, yRotation, 0f);
    }

    void Move()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        float speed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : moveSpeed;

        // ทิศทางการเคลื่อนที่แนวราบ
        Vector3 move = transform.parent.forward * v + transform.parent.right * h;

        // ✅ ระบบเช็คพื้นโดยใช้ Pivot Point
        // ถ้าคุณปิ๊บไม่ได้ลาก Pivot มาใส่ จะใช้ตำแหน่ง parent.position แทนเพื่อกัน Error
        Vector3 checkPos = (groundCheckPivot != null) ? groundCheckPivot.position : transform.parent.position;
        bool isGrounded = Physics.Raycast(checkPos, Vector3.down, groundCheckDistance, groundLayer);

        // วาดเส้น Debug เพื่อช่วยให้เห็นจุดเช็คพื้นในหน้า Scene (เขียว = พื้น, แดง = ลอย)
        Debug.DrawRay(checkPos, Vector3.down * groundCheckDistance, isGrounded ? Color.green : Color.red);

        if (isGrounded && yVelocity < 0)
        {
            yVelocity = -2f; // แรงกดตัวละครให้ติดพื้น
        }

        // ✅ ระบบกระโดด (Space Bar)
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            // สูตรคำนวณแรงส่ง v = sqrt(h * -2 * g)
            yVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            Debug.Log("🚀 Jump!");
        }

        // แรงโน้มถ่วง
        yVelocity += gravity * Time.deltaTime;

        // รวมแรงเคลื่อนที่ทั้งหมด
        Vector3 finalMove = move * speed;
        finalMove.y = yVelocity;

        if (controller != null)
        {
            controller.Move(finalMove * Time.deltaTime);
        }
    }

    void HandleCursor()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        if (Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}