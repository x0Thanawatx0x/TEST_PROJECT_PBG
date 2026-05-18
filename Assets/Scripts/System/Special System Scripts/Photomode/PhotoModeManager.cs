using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.IO;

public class PhotoModeManager : MonoBehaviour
{
    [Header("Keys")]
    public KeyCode toggleKey = KeyCode.C;
    public KeyCode captureKey = KeyCode.Space;
    public KeyCode exitKey = KeyCode.Escape;

    [Header("Camera & Visuals")]
    public Camera photoCamera;
    public float minFOV = 20f;
    public float maxFOV = 60f;
    public Image flashImage;

    [Header("UI Groups")]
    public GameObject[] uiObjects;
    public GameObject photoModeHUD;
    public GameObject photoPopup;
    public RawImage previewImage;

    [HideInInspector] public bool isPhotoMode = false;
    private bool isShowingPreview = false;
    private float originalFOV;
    private Texture2D capturedTexture;

    void Start()
    {
        if (photoPopup != null) photoPopup.SetActive(false);
        if (photoModeHUD != null) photoModeHUD.SetActive(false);
        if (flashImage != null) flashImage.canvasRenderer.SetAlpha(0f);

        if (photoCamera != null) originalFOV = photoCamera.fieldOfView;
    }

    void Update()
    {
        // ✅ เช็คการเข้าโหมด
        if (Input.GetKeyDown(toggleKey))
        {
            if (isShowingPreview) { CancelPhoto(); } // ถ้ามีรูปค้างอยู่ ให้ปิดก่อนออกจากโหมด
            TogglePhotoMode();
        }

        // ถ้าอยู่ใน Photo Mode
        if (isPhotoMode)
        {
            // กด ESC เพื่อออกจาก Photo Mode
            if (Input.GetKeyDown(exitKey))
            {
                ExitPhotoMode();
                return;
            }

            // ถ้าไม่ได้โชว์รูป Preview (กำลังเดินหามุม)
            if (!isShowingPreview)
            {
                HandleCameraControls(); // ปรับซูม FOV

                // ✅ เพิ่มกฎเหล็ก: ใน Update ห้ามยุ่งกับ Cursor.lockState เด็ดขาด!
                // ปล่อยให้ CM_Viewer หรือ ViewManager คุมไป

                // กด Space ถ่ายรูป
                if (Input.GetKeyDown(captureKey))
                {
                    Debug.Log("📸 แชะ!");
                    StartCoroutine(CapturePreview());
                }
            }
        }
    }

    // ✅ เพิ่มฟังก์ชันเช็คสถานะ
    public bool IsPhotoModeActive()
    {
        return isPhotoMode;
    }

    void HandleCameraControls()
    {
        if (photoCamera == null) return;
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            photoCamera.fieldOfView = Mathf.Clamp(photoCamera.fieldOfView - scroll * 50f, minFOV, maxFOV);
        }
    }

    void TogglePhotoMode()
    {
        isPhotoMode = !isPhotoMode;
        foreach (GameObject ui in uiObjects)
        {
            if (ui != null) ui.SetActive(!isPhotoMode);
        }
        if (photoModeHUD != null) photoModeHUD.SetActive(isPhotoMode);

        if (isPhotoMode)
        {
            Debug.Log("📸 เข้าสู่โหมดถ่ายภาพ (ขยับตัวหามุมได้ปกติ)");
            // ✅ ห้ามเซต Cursor.lockState ตรงนี้
        }
        else
        {
            ExitPhotoMode();
        }
    }

    void ExitPhotoMode()
    {
        isPhotoMode = false;
        if (photoCamera != null) photoCamera.fieldOfView = originalFOV;
        foreach (GameObject ui in uiObjects)
        {
            if (ui != null) ui.SetActive(true);
        }
        if (photoModeHUD != null) photoModeHUD.SetActive(false);
        if (photoPopup != null) photoPopup.SetActive(false);
        isShowingPreview = false;

        Debug.Log("❌ ออกจากโหมดถ่ายภาพ");

        // ✅ ไม่ต้องคืนค่า Cursor ที่นี่ เดี๋ยว ViewManager จัดการเองตอน V กลับมา
    }

    IEnumerator CapturePreview()
    {
        // 1. ซ่อน UI เฉพาะตอนถ่าย
        if (photoModeHUD != null) photoModeHUD.SetActive(false);
        photoPopup.SetActive(false);

        // ✅ 2. ปลดล็อก Cursor ทันทีเพื่อเตรียมให้ผู้เล่นกดปุ่มใน Popup
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        yield return new WaitForEndOfFrame();

        // 3. ถ่ายภาพ
        Texture2D tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        tex.Apply();

        capturedTexture = tex;
        previewImage.texture = capturedTexture;

        // 4. แฟลช
        if (flashImage != null)
        {
            flashImage.canvasRenderer.SetAlpha(1f);
            flashImage.CrossFadeAlpha(0f, 0.5f, false);
        }

        // 5. โชว์ Popup
        photoPopup.SetActive(true);
        isShowingPreview = true;
    }

    public void SavePhoto()
    {
        if (capturedTexture == null) return;
        byte[] bytes = capturedTexture.EncodeToPNG();
        string folder = Application.dataPath + "/Screenshots/";
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
        string fileName = "PlanBuilder_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
        File.WriteAllBytes(folder + fileName, bytes);
        ClosePopup();
    }

    public void CancelPhoto() { ClosePopup(); }

    void ClosePopup()
    {
        photoPopup.SetActive(false);
        isShowingPreview = false;

        // ✅ ถ้านังอยู่ใน Photo Mode ให้ล็อก Cursor กลับไปเพื่อเดินต่อ
        if (isPhotoMode)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (photoModeHUD != null) photoModeHUD.SetActive(true);
        }
    }
}