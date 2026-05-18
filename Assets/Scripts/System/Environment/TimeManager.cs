using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class TimeManager : MonoBehaviour
{
    [Header("Slider UI")]
    public Slider timeSlider;

    [Header("Icon Settings")]
    public Image timeIcon;
    public Sprite sunSprite;      // 0.00 - 0.30 (ตอนเช้า)
    public Sprite eveningSprite;  // 0.31 - 0.65 (ตอนเย็น)
    public Sprite moonSprite;     // 0.66 - 1.00 (กลางคืน)

    [Header("Clock UI (Optional)")]
    public RectTransform handleAnchor;

    [Header("Shadow System")]
    public RectTransform shadowAnchor;
    [Tooltip("ความเร็วของเงาตาม")]
    public float shadowSmoothness = 4f;

    [Header("Smooth Settings")]
    public float rotationSmoothness = 8f;

    // =========================
    // SKYBOX (5 STATES ONLY)
    // =========================
    [Header("Skybox (5 States)")]
    public Material morningSkybox;
    public Material noonSkybox;
    public Material eveningSkybox;
    public Material duskSkybox;
    public Material nightSkybox;

    [Header("Light Settings")]
    public Light sunLight;
    public Gradient sunColor;

    [Header("Time Logic")]
    [Range(0, 1)]
    public float currentTime;
    public float daySpeed = 0.01f;
    public bool autoPassTime = true;

    [Header("Lighting")]
    public AnimationCurve lightIntensity;

    private float displayTime;
    private float _smoothTimeVelocity = 0f;

    void Start()
    {
        if (timeSlider != null)
        {
            timeSlider.minValue = 0f;
            timeSlider.maxValue = 1f;
            timeSlider.value = currentTime;
            timeSlider.onValueChanged.AddListener(OnSliderValueChanged);
        }

        displayTime = currentTime;

        if (shadowAnchor != null && handleAnchor != null)
            shadowAnchor.localRotation = handleAnchor.localRotation;

        UpdateIcon(currentTime);
    }

    void Update()
    {
        // =========================
        // AUTO TIME LOGIC
        // =========================
        if (autoPassTime)
        {
            currentTime += Time.deltaTime * daySpeed;
            currentTime = Mathf.Repeat(currentTime, 1f);

            if (timeSlider != null)
            {
                timeSlider.onValueChanged.RemoveListener(OnSliderValueChanged);
                timeSlider.value = currentTime;
                timeSlider.onValueChanged.AddListener(OnSliderValueChanged);
            }
        }

        displayTime = Mathf.SmoothDamp(displayTime, currentTime, ref _smoothTimeVelocity, 0.05f);

        // =========================
        // UPDATES
        // =========================
        UpdateSkybox(displayTime);
        UpdateEnvironment(displayTime);
        UpdateUI(displayTime);
        UpdateIcon(currentTime);
    }

    public void OnSliderValueChanged(float value)
    {
        currentTime = value;
    }

    // =========================
    // ☀️/🌇/🌙 ICON SYSTEM (ปรับตามช่วงเวลาที่คุณปิ๊บกำหนด)
    // =========================
    void UpdateIcon(float t)
    {
        if (timeIcon == null || sunSprite == null || eveningSprite == null || moonSprite == null) return;

        // 0 -> 0.30 ตอนเช้า
        if (t <= 0.30f)
        {
            if (timeIcon.sprite != sunSprite) timeIcon.sprite = sunSprite;
        }
        // 0.31 -> 0.65 ตอนเย็น
        else if (t > 0.30f && t <= 0.65f)
        {
            if (timeIcon.sprite != eveningSprite) timeIcon.sprite = eveningSprite;
        }
        // 0.66 -> 1 เป็นกลางคืน
        else
        {
            if (timeIcon.sprite != moonSprite) timeIcon.sprite = moonSprite;
        }
    }

    // =========================
    // SKYBOX SWITCH (5 STATES)
    // =========================
    void UpdateSkybox(float t)
    {
        if (t < 0.2f) SetSkybox(morningSkybox);
        else if (t < 0.4f) SetSkybox(noonSkybox);
        else if (t < 0.6f) SetSkybox(eveningSkybox);
        else if (t < 0.8f) SetSkybox(duskSkybox);
        else SetSkybox(nightSkybox);

        if (Time.frameCount % 10 == 0)
            DynamicGI.UpdateEnvironment();
    }

    void SetSkybox(Material mat)
    {
        if (RenderSettings.skybox != mat)
            RenderSettings.skybox = mat;
    }

    void UpdateUI(float t)
    {
        if (handleAnchor != null)
        {
            float targetAngle = -t * 360f;
            handleAnchor.localRotation = Quaternion.Euler(0, 0, targetAngle);
        }

        if (shadowAnchor != null && handleAnchor != null)
        {
            shadowAnchor.localRotation = Quaternion.Lerp(
                shadowAnchor.localRotation,
                handleAnchor.localRotation,
                Time.deltaTime * shadowSmoothness
            );
        }
    }

    void UpdateEnvironment(float t)
    {
        float sunAngle = t * 360f - 90f;
        sunLight.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0);
        sunLight.color = sunColor.Evaluate(t);

        if (lightIntensity != null)
            sunLight.intensity = lightIntensity.Evaluate(t);
    }
}