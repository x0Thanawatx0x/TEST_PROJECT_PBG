using UnityEngine;

[AddComponentMenu("PlanBuilder/Environment/Wall Fade Controller")]
public class WallFadeController : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("ลาก Main Camera มาใส่ที่นี่")]
    public Transform playerCamera;

    [Header("Fade Parameters")]
    [Range(0.5f, 10f)]
    public float fadeDistance = 2.5f; // ระยะที่เริ่มจางตามแผนงาน

    [Header("Shader Settings")]
    [Tooltip("ชื่อ Reference ใน Shader Graph ต้องตรงกันเป๊ะ")]
    public string shaderReferenceName = "_FadeAmount";

    private Renderer wallRenderer;
    private MaterialPropertyBlock propBlock;
    private int fadeAmountID;

    void Start()
    {
        wallRenderer = GetComponent<Renderer>();
        propBlock = new MaterialPropertyBlock();
        fadeAmountID = Shader.PropertyToID(shaderReferenceName);

        // ตรวจสอบกล้องอัตโนมัติถ้าลืมลากใส่[cite: 1]
        if (playerCamera == null && Camera.main != null)
        {
            playerCamera = Camera.main.transform;
        }
    }

    void Update()
    {
        if (playerCamera == null || wallRenderer == null) return;

        // คำนวณระยะห่างระหว่างกล้องกับวัตถุ
        float distance = Vector3.Distance(playerCamera.position, transform.position);

        // คำนวณค่า Alpha (0 = ใกล้มาก/จางหาย, 1 = ไกล/ชัดเจน)
        // ปรับ Logic ให้เข้ากับ Dither Node ใน Shader Graph ของปิ๊บ[cite: 1]
        float alpha = Mathf.Clamp01(distance / fadeDistance);

        // ใช้ MaterialPropertyBlock เพื่อ Performance ที่ดีกว่า (ไม่สร้าง Material Instance ใหม่)
        wallRenderer.GetPropertyBlock(propBlock);
        propBlock.SetFloat(fadeAmountID, alpha);
        wallRenderer.SetPropertyBlock(propBlock);
    }

    // ช่วยให้เห็นระยะ Fade ในหน้า Scene View
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, fadeDistance);
    }
}