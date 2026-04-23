using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 10f;

    [Header("Rotation")]
    public float rotationSpeed = 3f;

    [Header("Zoom")]
    public float zoomSpeed = 10f;
    public float minZoom = 5f;
    public float maxZoom = 50f;

    float currentX = 0f;
    float currentY = 0f;

    void Update()
    {
        HandleMovement();
        HandleRotation();
        HandleZoom();
    }

    void HandleMovement()
    {
        // 🟢 คลิกซ้าย หรือ คลิกขวา = ขยับได้
        if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
        {
            float h = Input.GetAxis("Horizontal"); // A D
            float v = Input.GetAxis("Vertical");   // W S

            Vector3 dir = transform.forward * v + transform.right * h;
            dir.y = 0;

            transform.position += dir * moveSpeed * Time.deltaTime;
        }
    }

    void HandleRotation()
    {
        // 🔵 คลิกขวาค้าง = หมุน
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X") * rotationSpeed * 100f * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed * 100f * Time.deltaTime;

            currentX += mouseX;
            currentY -= mouseY;

            // ล็อกมุมเงย
            currentY = Mathf.Clamp(currentY, -30f, 80f);

            transform.rotation = Quaternion.Euler(currentY, currentX, 0);
        }
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        Vector3 pos = transform.position;
        pos += transform.forward * scroll * zoomSpeed;

        float distance = Vector3.Distance(pos, Vector3.zero);

        if (distance > minZoom && distance < maxZoom)
        {
            transform.position = pos;
        }
    }
}