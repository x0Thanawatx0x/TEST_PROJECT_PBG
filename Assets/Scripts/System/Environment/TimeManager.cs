using UnityEngine;

public class TimeManager : MonoBehaviour
{
    [Header("Skybox Files")]
    public Material daySkybox;    // ลากไฟล์ 'Extended Day' มาใส่
    public Material nightSkybox;  // ลากไฟล์ 'Extended Night' มาใส่

    [Header("Light Settings")]
    public Light sunLight;
    public Gradient sunColor;

    [Range(0, 1)] public float currentTime;
    public float daySpeed = 0.1f;

    void Update()
    {
        currentTime += Time.deltaTime * daySpeed;
        if (currentTime > 1) currentTime = 0;

        UpdateEnvironment();
    }

    void UpdateEnvironment()
    {
        // 1. จัดการแสงดวงอาทิตย์
        float sunAngle = currentTime * 360f - 90f;
        sunLight.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0);
        sunLight.color = sunColor.Evaluate(currentTime);

        // 2. ระบบสลับไฟล์ Skybox ตามช่วงเวลา (Logic การเปลี่ยนท้องฟ้า)
        // ถ้าเวลาอยู่ระหว่าง 0.2 ถึง 0.8 ให้เป็นกลางวัน นอกเหนือจากนั้นเป็นกลางคืน
        if (currentTime > 0.2f && currentTime < 0.8f)
        {
            if (RenderSettings.skybox != daySkybox)
            {
                RenderSettings.skybox = daySkybox; // เปลี่ยนเป็นไฟล์กลางวัน
            }
        }
        else
        {
            if (RenderSettings.skybox != nightSkybox)
            {
                RenderSettings.skybox = nightSkybox; // เปลี่ยนเป็นไฟล์กลางคืน
            }
        }

        // 3. บังคับให้ Unity 6 อัปเดตแสงสะท้อน (สำคัญมาก!)[cite: 1, 3]
        if (Time.frameCount % 10 == 0)
        {
            DynamicGI.UpdateEnvironment();
        }
    }
}