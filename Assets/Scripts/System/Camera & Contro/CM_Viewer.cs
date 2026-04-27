using UnityEngine;

public class CM_Viewer : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float sprintSpeed = 8f;
    public float mouseSensitivity = 2f;

    [Header("Physics")]
    public float gravity = -9.81f;
    public float groundCheckDistance = 0.3f;
    public LayerMask groundLayer;

    [Header("Leaning (Q/E)")]
    public float leanAngle = 15f;
    float currentLean = 0f;

    float xRotation = 0f;
    float yRotation = 0f;
    float yVelocity;

    CharacterController controller;

    void Start()
    {
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
            // ✅ นำ HandleZoom() ออกไปแล้ว
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

        Vector3 move = transform.parent.forward * v + transform.parent.right * h;

        bool isGrounded = Physics.Raycast(transform.parent.position, Vector3.down, groundCheckDistance, groundLayer);
        if (isGrounded && yVelocity < 0)
            yVelocity = -2f;

        yVelocity += gravity * Time.deltaTime;
        move.y = yVelocity;

        if (controller != null)
            controller.Move(move * speed * Time.deltaTime);
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