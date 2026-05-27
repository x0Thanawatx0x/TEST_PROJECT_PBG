using System.Collections;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization.Components;

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
    public LocalizeStringEvent modeTextLocalizer;

    [Header("Tool UI")]
    public GameObject toolUI;

    [Header("Dependencies to Reset")]
    [SerializeField] private ToolManager toolManager;
    [SerializeField] private PlacementSystem placementSystem;
    [SerializeField] private PhotoModeManager photoMode;

    [Header("UI Text (Editable)")]
    public string builderText = "โหมด: Builder 🏠";
    public string viewerText = "โหมด: Viewer 🚶";

    public Color builderColor = Color.green;
    public Color viewerColor = Color.cyan;

    [Header("Transition")]
    public float transitionTime = 0.8f;

    private bool isSwitching = false;

    void Start()
    {
        // ✨ บังคับระบบเวลาของโลกให้ทำงานปกติ ป้องกันบั๊กแช่แข็งแอนิเมชันกล้องค้าง
        Time.timeScale = 1f;

        SetMode(GameMode.Builder);

        if (switchButton != null)
            switchButton.onClick.AddListener(ToggleMode);
    }

    void Update()
    {
        if (photoMode != null && photoMode.isPhotoMode)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.V) && !isSwitching)
        {
            ToggleMode();
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            LocaleSelector selector = Object.FindFirstObjectByType<LocaleSelector>();
            if (selector != null)
            {
                int currentID = PlayerPrefs.GetInt("LocaleKey", 0);
                int nextID = (currentID == 0) ? 1 : 0;
                selector.ChangeLocale(nextID);
            }
        }

        if (currentMode == GameMode.Viewer && !isSwitching)
        {
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    public void ToggleMode()
    {
        if (photoMode != null && photoMode.isPhotoMode) return;

        if (!isSwitching)
            StartCoroutine(SmoothSwitch());
    }

    IEnumerator SmoothSwitch()
    {
        isSwitching = true;

        // ปิดคอนโทรลเลอร์ชั่วคราวระหว่างกล้องกำลังสไลด์นุ่มๆ ข้ามโหมด
        if (builderController != null) builderController.enabled = false;
        if (viewerController != null) viewerController.enabled = false;

        if (currentMode == GameMode.Builder)
        {
            ResetBuildModeVisuals();

            if (builderCam != null)
                builderCam.Priority = 0;

            yield return null;
            yield return StartCoroutine(MoveCamera(mainCamera.transform, viewerCamera.transform));
            SetMode(GameMode.Viewer);
        }
        else
        {
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

    private void ResetBuildModeVisuals()
    {
        if (toolManager != null)
        {
            toolManager.SelectTool(0);
        }

        if (placementSystem != null)
        {
            placementSystem.SendMessage("UpdateToolLogic", SendMessageOptions.DontRequireReceiver);
        }
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
            t = t * t * (3f - 2f * t); // Smoothstep
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
            mainCamera.SetActive(true);
            viewerCamera.SetActive(false);

            if (builderCam != null)
                builderCam.Priority = 10;

            // ✨ [FIXED SAFETY] ปลดล็อกสคริปต์กล้องให้กลับมาทำงาน ห้ามค้างคาเฟรมเด็ดขาด
            if (builderController != null)
            {
                builderController.enabled = true;
                builderController.gameObject.SetActive(true);
            }
            if (viewerController != null) viewerController.enabled = false;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (toolUI != null)
                toolUI.SetActive(true);

            if (modeText != null)
            {
                modeText.text = builderText;
                modeText.color = builderColor;
            }

            if (modeTextLocalizer != null)
            {
                modeTextLocalizer.StringReference.TableEntryReference = "MODE_BUILDER";
            }
        }
        else
        {
            mainCamera.SetActive(false);
            viewerCamera.SetActive(true);

            if (builderCam != null)
                builderCam.Priority = 0;

            if (builderController != null) builderController.enabled = false;

            // ✨ [FIXED SAFETY] เปิดระบบขยับกล้องฝั่ง Viewer ให้ตื่นขึ้นมาทำงานทันที
            if (viewerController != null)
            {
                viewerController.enabled = true;
                viewerController.gameObject.SetActive(true);
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (toolUI != null)
                toolUI.SetActive(false);

            if (modeText != null)
            {
                modeText.text = viewerText;
                modeText.color = viewerColor;
            }

            if (modeTextLocalizer != null)
            {
                modeTextLocalizer.StringReference.TableEntryReference = "MODE_VIEWER";
            }
        }

        Debug.Log($"<color=lime>[ViewManager]</color> สลับสถานะสำเร็จ! โหมดปัจจุบันคือ: {currentMode}");
    }
}