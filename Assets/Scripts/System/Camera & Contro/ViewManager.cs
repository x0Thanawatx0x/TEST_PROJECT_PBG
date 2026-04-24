using System.Collections;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings; // ✅ เพิ่มเพื่อใช้คำสั่งเปลี่ยนภาษาผ่านคีย์บอร์ด

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
        // ⌨️ กด V เพื่อสลับโหมด
        if (Input.GetKeyDown(KeyCode.V) && !isSwitching)
        {
            ToggleMode();
        }

        // ⌨️ ✅ เพิ่มเติม: กด L เพื่อสลับภาษา (Locale) ทันทีโดยไม่ต้องใช้เมาส์
        if (Input.GetKeyDown(KeyCode.L))
        {
            LocaleSelector selector = Object.FindFirstObjectByType<LocaleSelector>();
            if (selector != null)
            {
                // สลับ ID ระหว่าง 0 (EN) กับ 1 (TH)
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
                // ถ้าปล่อยปุ่ม Ctrl ให้กลับไปล็อกเมาส์เหมือนเดิม
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
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

        if (builderController != null) builderController.enabled = false;
        if (viewerController != null) viewerController.enabled = false;

        if (currentMode == GameMode.Builder)
        {
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

        Debug.Log("โหมด: " + currentMode);
    }
}