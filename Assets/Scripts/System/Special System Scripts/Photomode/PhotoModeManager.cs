using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.IO;

public class PhotoModeManager : MonoBehaviour
{
    [Header("Keys")]
    public KeyCode toggleKey = KeyCode.C;
    public KeyCode captureKey = KeyCode.Space;
    public KeyCode exitKey = KeyCode.Escape; // ออกจากโหมด

    [Header("UI")]
    public GameObject[] uiObjects;     // UI ทั้งหมดในเกม
    public GameObject photoPopup;      // Panel popup
    public RawImage previewImage;      // แสดงรูป

    private bool isPhotoMode = false;
    private Texture2D capturedTexture;

    void Start()
    {
        if (photoPopup != null)
            photoPopup.SetActive(false);
    }

    void Update()
    {
        // กด C เข้า/ออกโหมด
        if (Input.GetKeyDown(toggleKey))
        {
            TogglePhotoMode();
        }

        // กด ESC ออกจากโหมด
        if (isPhotoMode && Input.GetKeyDown(exitKey))
        {
            ExitPhotoMode();
        }

        // กด Space ถ่ายรูป
        if (isPhotoMode && Input.GetKeyDown(captureKey))
        {
            Debug.Log("📸 กำลังถ่ายภาพ...");
            StartCoroutine(CapturePreview());
        }
    }

    // ===============================
    // 🎮 เข้า/ออก Photo Mode
    // ===============================
    void TogglePhotoMode()
    {
        isPhotoMode = !isPhotoMode;

        foreach (GameObject ui in uiObjects)
        {
            if (ui != null)
                ui.SetActive(!isPhotoMode);
        }

        if (isPhotoMode)
        {
            Debug.Log("📸 เข้าสู่โหมดถ่ายภาพ");

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Debug.Log("❌ ออกจากโหมดถ่ายภาพ");

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void ExitPhotoMode()
    {
        isPhotoMode = false;

        foreach (GameObject ui in uiObjects)
        {
            if (ui != null)
                ui.SetActive(true);
        }

        photoPopup.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("❌ ออกจากโหมดถ่ายภาพ");
    }

    // ===============================
    // 📸 Capture + Preview
    // ===============================
    IEnumerator CapturePreview()
    {
        yield return new WaitForEndOfFrame();

        Texture2D tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        tex.Apply();

        capturedTexture = tex;

        previewImage.texture = capturedTexture;
        photoPopup.SetActive(true);

        Debug.Log("✅ ถ่ายภาพเรียบร้อยแล้ว!");
    }

    // ===============================
    // 💾 Save
    // ===============================
    public void SavePhoto()
    {
        if (capturedTexture == null) return;

        byte[] bytes = capturedTexture.EncodeToPNG();

        string folder = Application.dataPath + "/Screenshots/";
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        string fileName = "Photo_" + System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".png";
        string path = folder + fileName;

        File.WriteAllBytes(path, bytes);

        Debug.Log("💾 บันทึกภาพแล้ว: " + path);

        ClosePopup(); // ✔ ยังอยู่ใน Photo Mode
    }

    // ===============================
    // ❌ No Save (ปิด popup อย่างเดียว)
    // ===============================
    public void CancelPhoto()
    {
        Debug.Log("❌ ไม่บันทึกภาพ");

        ClosePopup(); // ✔ ปิด panel แต่ยังอยู่ใน Photo Mode
    }

    void ClosePopup()
    {
        photoPopup.SetActive(false);
    }
}