using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ToolManager : MonoBehaviour
{
    public enum BuildTool
    {
        None = 0, House = 1, Road = 2, Furniture = 3,
        Wall = 4, Nature = 5, Pond = 6, Eraser = 7
    }

    [Header("Current Status")]
    public BuildTool currentTool = BuildTool.None;

    public int houseIndex = 0;
    public int furnitureIndex = 0;
    public int natureIndex = 0;

    [Header("UI Setup")]
    public List<Button> toolButtons;

    [Header("Highlight Settings (Legacy - Still kept for safety)")]
    public Vector3 normalScale = Vector3.one;
    public Vector3 selectedScale = new Vector3(1.15f, 1.15f, 1.15f);
    public Color normalColor = Color.white;
    public Color selectedColor = new Color(0.7f, 1f, 0.7f);

    void Start()
    {
        for (int i = 0; i < toolButtons.Count; i++)
        {
            int index = i + 1;
            if (toolButtons[i] != null)
                toolButtons[i].onClick.AddListener(() => SelectTool(index));
        }
        UpdateButtonVisual();
    }

    void Update()
    {
        // Shortcut Keys 1-7
        for (int i = 1; i <= 7; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0 + i))
                SelectTool(i);
        }

        if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Escape))
            SelectTool(0);
    }

    public void SelectTool(int index)
    {
        BuildTool selected = (BuildTool)index;

        if (currentTool == selected)
        {
            if (currentTool == BuildTool.House) houseIndex++;
            else if (currentTool == BuildTool.Furniture) furnitureIndex++;
            else if (currentTool == BuildTool.Nature) natureIndex++;
        }
        else
        {
            currentTool = selected;
        }
        UpdateButtonVisual();
    }

    void UpdateButtonVisual()
    {
        for (int i = 0; i < toolButtons.Count; i++)
        {
            if (toolButtons[i] == null) continue;

            // ✅ ดึง Animator จากปุ่มมาใช้งาน
            Animator anim = toolButtons[i].GetComponent<Animator>();
            bool isSelected = (int)currentTool == i + 1;

            if (anim != null)
            {
                // ✅ ส่งค่า IsSelected เข้าไปใน Animator 
                // ถ้า True อนิเมชั่นจะเล่นไปข้างหน้า (เฟรม 0 -> สุด)
                // ถ้า False อนิเมชั่นจะเล่นย้อนกลับ (สุด -> เฟรม 0) ตามที่ตั้งค่าใน Animator Controller
                anim.SetBool("IsSelected", isSelected);
            }
            else
            {
                // สำรองไว้ในกรณีที่ปุ่มไหนไม่มี Animator จะได้ยังเห็นสีเขียวอยู่แบบเดิม
                Image img = toolButtons[i].GetComponent<Image>();
                toolButtons[i].transform.localScale = isSelected ? selectedScale : normalScale;
                if (img != null) img.color = isSelected ? selectedColor : normalColor;
            }
        }
    }
}