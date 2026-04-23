using System.Collections;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.UI;
using TMPro;

public class ViewManager : MonoBehaviour
{
    public enum GameMode { Builder, Viewer }

    [Header("Settings")]
    public GameMode currentMode = GameMode.Builder;

    [Header("Cameras")]
    public GameObject mainCamera;
    public GameObject viewerCamera;

    [Header("Builder Cinemachine")]
    public CinemachineCamera builderCam;

    [Header("Controllers")]
    public MonoBehaviour builderController;
    public MonoBehaviour viewerController;

    [Header("UI")]
    public Button switchButton;
    public TextMeshProUGUI modeText;

    [Header("Tool UI")]
    public GameObject toolUI; // 🔥 Panel ของ ToolManager

    [Header("UI Text (Editable)")]
    public string builderText = "โหมด: Builder 🏠";
    public string viewerText = "โหมด: Viewer 🚶";

    public Color builderColor = Color.green;
    public Color viewerColor = Color.cyan;

    [Header("Transition")]
    public float transitionTime = 0.8f;

    bool isSwitching = false;

    void Start()
    {
        SetMode(GameMode.Builder);

        if (switchButton != null)
            switchButton.onClick.AddListener(ToggleMode);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.V) && !isSwitching)
        {
            ToggleMode();
        }
    }

    public void ToggleMode()
    {
        if (!isSwitching)
            StartCoroutine(SmoothSwitch());
    }

    IEnumerator SmoothSwitch()
    {
        isSwitching = true;

        // 🔒 ปิด controller กันรบกวน
        if (builderController != null) builderController.enabled = false;
        if (viewerController != null) viewerController.enabled = false;

        if (currentMode == GameMode.Builder)
        {
            // 🏠 → 🚶

            if (builderCam != null)
                builderCam.Priority = 0;

            yield return null;

            yield return StartCoroutine(MoveCamera(mainCamera.transform, viewerCamera.transform));

            SetMode(GameMode.Viewer);
        }
        else
        {
            // 🚶 → 🏠

            mainCamera.SetActive(true);
            viewerCamera.SetActive(false);

            yield return null;

            yield return StartCoroutine(MoveCamera(mainCamera.transform, builderCam.transform));

            yield return new WaitForSeconds(0.05f);

            if (builderCam != null)
                builderCam.Priority = 10;

            SetMode(GameMode.Builder);
        }

        isSwitching = false;
    }

    IEnumerator MoveCamera(Transform from, Transform to)
    {
        float time = 0f;

        Vector3 startPos = from.position;
        Quaternion startRot = from.rotation;

        Vector3 targetPos = to.position;
        Quaternion targetRot = to.rotation;

        while (time < transitionTime)
        {
            time += Time.deltaTime;

            float t = time / transitionTime;
            t = t * t * (3f - 2f * t); // ease in-out

            from.position = Vector3.Lerp(startPos, targetPos, t);
            from.rotation = Quaternion.Slerp(startRot, targetRot, t);

            yield return null;
        }

        from.position = targetPos;
        from.rotation = targetRot;
    }

    void SetMode(GameMode mode)
    {
        currentMode = mode;

        if (currentMode == GameMode.Builder)
        {
            // 🎥 กล้อง
            mainCamera.SetActive(true);
            viewerCamera.SetActive(false);

            if (builderCam != null)
                builderCam.Priority = 10;

            // 🎮 Controller
            if (builderController != null) builderController.enabled = true;
            if (viewerController != null) viewerController.enabled = false;

            // 🖱 เมาส์
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // 🟢 แสดง Tool UI
            if (toolUI != null)
                toolUI.SetActive(true);

            // 📝 UI Text
            if (modeText != null)
            {
                modeText.text = builderText;
                modeText.color = builderColor;
            }
        }
        else
        {
            // 🎥 กล้อง
            mainCamera.SetActive(false);
            viewerCamera.SetActive(true);

            if (builderCam != null)
                builderCam.Priority = 0;

            // 🎮 Controller
            if (builderController != null) builderController.enabled = false;
            if (viewerController != null) viewerController.enabled = true;

            // 🔒 เมาส์
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // ❌ ซ่อน Tool UI
            if (toolUI != null)
                toolUI.SetActive(false);

            // 📝 UI Text
            if (modeText != null)
            {
                modeText.text = viewerText;
                modeText.color = viewerColor;
            }
        }

        Debug.Log("โหมด: " + currentMode);
    }
}