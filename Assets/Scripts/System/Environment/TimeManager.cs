using UnityEngine;
using UnityEngine.EventSystems;

public class TimeManager : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IDragHandler
{
    [Header("Clock UI")]
    public RectTransform clockFace;
    public RectTransform handleAnchor;

    [Header("Shadow System")]
    public RectTransform shadowAnchor;

    [Tooltip("ความเร็วของเงาตาม")]
    public float shadowSmoothness = 4f;

    [Header("Drag Settings")]
    public float dragSensitivity = 0.5f;
    public float rotationSmoothness = 8f;
    public float inertiaDamping = 3f;      // ✅ หยุดช้าพอดี — ปรับได้ 2-5

    private bool isDragging = false;

    private float accumulatedAngle;
    private float displayAngle;
    private float angularVelocity;
    private Vector2 lastMouseDirection;

    private float _smoothDampVelocity = 0f;
    private float _lastAngleDelta = 0f;    // ✅ เก็บ delta ล่าสุดไว้ทำ momentum

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

    private float worldTime;
    public float daySpeed = 0.01f;

    [Header("Lighting")]
    public AnimationCurve lightIntensity;

    void Start()
    {
        worldTime = currentTime;
        accumulatedAngle = 0f;
        displayAngle = 0f;

        UpdateUI();

        if (shadowAnchor != null)
            shadowAnchor.localRotation = handleAnchor.localRotation;
    }

    void Update()
    {
        // =========================
        // ROTATION SYSTEM
        // =========================
        if (!isDragging)
        {
            // ✅ inertia พาต่อตอนปล่อยมือ แล้วค่อยๆ หยุด
            angularVelocity = Mathf.Lerp(
                angularVelocity,
                0f,
                Time.deltaTime * inertiaDamping
            );
            accumulatedAngle += angularVelocity * Time.deltaTime;
        }

        // ✅ display smooth ตาม accumulated
        displayAngle = Mathf.SmoothDampAngle(
            displayAngle,
            accumulatedAngle,
            ref _smoothDampVelocity,
            0.04f
        );

        // =========================
        // TIME CONVERT — องศา → เวลา โดยตรง
        // =========================
        currentTime = Mathf.Repeat(-accumulatedAngle / 360f, 1f);

        // =========================
        // AUTO TIME ALWAYS RUNNING (เฉพาะตอนไม่ได้ drag)
        // =========================
        if (!isDragging)
        {
            worldTime += Time.deltaTime * daySpeed;
            worldTime = Mathf.Repeat(worldTime, 1f);

            accumulatedAngle -= daySpeed * Time.deltaTime * 360f;
        }

        // =========================
        // SKYBOX SYSTEM
        // =========================
        UpdateSkybox(currentTime);
        UpdateEnvironment(currentTime);
        UpdateUI();
    }

    // =========================
    // SKYBOX SWITCH (5 STATES — แบ่งเท่ากัน 20% ต่ออัน)
    // =========================
    void UpdateSkybox(float t)
    {
        if (t < 0.2f)
            SetSkybox(morningSkybox);
        else if (t < 0.4f)
            SetSkybox(noonSkybox);
        else if (t < 0.6f)
            SetSkybox(eveningSkybox);
        else if (t < 0.8f)
            SetSkybox(duskSkybox);
        else
            SetSkybox(nightSkybox);

        if (Time.frameCount % 10 == 0)
            DynamicGI.UpdateEnvironment();
    }

    void SetSkybox(Material mat)
    {
        if (RenderSettings.skybox != mat)
            RenderSettings.skybox = mat;
    }

    void UpdateUI()
    {
        if (handleAnchor != null)
            handleAnchor.localRotation = Quaternion.Euler(0, 0, displayAngle);

        if (shadowAnchor != null)
        {
            shadowAnchor.localRotation = Quaternion.Lerp(
                shadowAnchor.localRotation,
                handleAnchor.localRotation,
                Time.deltaTime * shadowSmoothness
            );
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isDragging = true;
        _smoothDampVelocity = 0f;
        angularVelocity = 0f;
        _lastAngleDelta = 0f;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            clockFace,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint
        );

        lastMouseDirection = localPoint.normalized;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;

        // ✅ ส่ง momentum จาก delta สุดท้ายไปให้ inertia
        angularVelocity = _lastAngleDelta * dragSensitivity * 60f;
        angularVelocity = Mathf.Clamp(angularVelocity, -720f, 720f);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            clockFace,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint))
        {
            Vector2 currentDirection = localPoint.normalized;

            float angleDelta = Vector2.SignedAngle(
                lastMouseDirection,
                currentDirection
            );

            // ✅ ตามเม้าท์ 1:1
            accumulatedAngle += angleDelta * dragSensitivity;

            // ✅ เก็บ delta ไว้ทำ momentum ตอนปล่อย
            _lastAngleDelta = angleDelta;

            lastMouseDirection = currentDirection;
        }
    }

    void UpdateEnvironment(float time)
    {
        float sunAngle = time * 360f - 90f;

        sunLight.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0);
        sunLight.color = sunColor.Evaluate(time);

        if (lightIntensity != null)
            sunLight.intensity = lightIntensity.Evaluate(time);
    }
}