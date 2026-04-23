using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    // ▶️ เริ่มเกม
    public void StartGame()
    {
        SceneManager.LoadScene("GameScene"); // เปลี่ยนชื่อ Scene ตามของจริง
    }

    // ⚙️ ตั้งค่า
    public void OpenSettings()
    {
        Debug.Log("Open Settings");
      
    }

    // ❌ ออกจากเกม
    public void QuitGame()
    {
        Debug.Log("Quit Game");
        Application.Quit();
    }
}