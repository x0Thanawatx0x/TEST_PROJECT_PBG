using System.Collections;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;

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
    [SerializeField] private PhotoModeManager photoMode; // ✅ เชื่อมต่อเพื่อเช็คว่ากำลังถ่ายรูปอยู่ไหม

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
        // ✅ กฎเหล็ก: ถ้าอยู่ใน Photo Mode จะกด V สลับโหมดไม่ได้ (เพื่อป้องกัน UI ทับซ้อน)
        // แต่ยังสามารถขยับตัว/ขยับกล้องในโหมดนั้นๆ เพื่อถ่ายรูปได้ปกติ
        if (photoMode != null && photoMode.isPhotoMode)
        {
            return; // หยุดการเช็คปุ่ม V และ L ในเฟรมนี้
        }

        // ⌨️ กด V เพื่อสลับโหมด (ทำงานเฉพาะตอนไม่ได้เปิดโหมดถ่ายรูป)
        if (Input.GetKeyDown(KeyCode.V) && !isSwitching)
        {
            ToggleMode();
        }

        // ⌨️ กด L เพื่อสลับภาษา
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

        // 🖱️ พิเศษ: ในโหมด Viewer ถ้ากด Ctrl ค้างไว้ ให้โชว์เมาส์
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
        // ✅ ป้องกันการกดผ่านปุ่ม UI บนหน้าจอขณะถ่ายรูปด้วย
        if (photoMode != null && photoMode.isPhotoMode) return;

        if (!isSwitching)
            StartCoroutine(SmoothSwitch());
    }

    IEnumerator SmoothSwitch()
    {
        isSwitching = true;

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
            t = t * t * (3f - 2f * t);
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

            if (builderController != null) builderController.enabled = true;
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
            if (viewerController != null) viewerController.enabled = true;

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
    }
}