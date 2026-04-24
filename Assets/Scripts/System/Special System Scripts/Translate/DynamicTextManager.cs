using UnityEngine;
using UnityEngine.Localization.Components;

public class DynamicTextManager : MonoBehaviour
{
    [Header("Localization Setup")]
    [Tooltip("ลากวัตถุ Text ที่มี Localize String Event มาใส่ตรงนี้")]
    public LocalizeStringEvent textLocalizer;

    [Header("Current Status")]
    public bool isBuilderMode = true;

    /// <summary>
    /// ฟังก์ชันสำหรับสลับโหมด Builder / Viewer
    /// เชื่อมต่อกับปุ่ม OnClick() ได้เลย
    /// </summary>
    public void ToggleBuilderViewer()
    {
        isBuilderMode = !isBuilderMode;
        UpdateText(isBuilderMode ? "MODE_BUILDER" : "MODE_VIEWER");
    }

    /// <summary>
    /// ฟังก์ชันกลางสำหรับเปลี่ยน Key เป็นอะไรก็ได้
    /// ปิ๊บสามารถเรียกใช้ผ่านสคริปต์อื่นได้ด้วย เช่น SwitchKey("YOUR_KEY")
    /// </summary>
    public void SwitchKey(string newKey)
    {
        UpdateText(newKey);
    }

    private void UpdateText(string key)
    {
        if (textLocalizer == null)
        {
            Debug.LogWarning($"<color=red>DynamicTextManager:</color> ยังไม่ได้ลาก LocalizeStringEvent มาใส่ที่ {gameObject.name} เลยครับปิ๊บ!");
            return;
        }

        // สั่งเปลี่ยน Key ในระบบ Localization
        textLocalizer.StringReference.TableEntryReference = key;

        Debug.Log($"<color=cyan>DynamicTextManager:</color> เปลี่ยนข้อความเป็น Key -> <b>{key}</b>");
    }
}