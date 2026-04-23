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

    [Header("UI Setup")]
    public List<Button> toolButtons;

    [Header("Highlight Settings")]
    public Vector3 normalScale = Vector3.one;
    public Vector3 selectedScale = new Vector3(1.2f, 1.2f, 1.2f);

    public Color normalColor = Color.white;
    public Color selectedColor = Color.green;

    private void Start()
    {
        // ผูกปุ่มกับ Tool
        for (int i = 0; i < toolButtons.Count; i++)
        {
            int index = i + 1;

            if (toolButtons[i] != null)
            {
                toolButtons[i].onClick.AddListener(() => SelectTool(index));
            }
        }

        // อัปเดต UI ตอนเริ่ม
        UpdateButtonVisual();
    }

    private void Update()
    {
        HandleKeyboardInput();
    }

    private void HandleKeyboardInput()
    {
        for (int i = 1; i <= 7; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0 + i))
            {
                SelectTool(i);
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SelectTool(0);
        }
    }

    public void SelectTool(int index)
    {
        currentTool = (BuildTool)index;

        // 🔥 อัปเดตหน้าตา UI
        UpdateButtonVisual();

        string toolNameThai = GetToolNameThai(currentTool);
        string inputSource = Input.anyKeyDown ? "คีย์บอร์ด" : "เมาส์";

        Debug.Log($"<color=#66FF66>[ระบบ]</color> คุณเลือกหมวดหมู่: <b>{toolNameThai}</b> (กดผ่าน {inputSource})");
    }

    void UpdateButtonVisual()
    {
        for (int i = 0; i < toolButtons.Count; i++)
        {
            if (toolButtons[i] == null) continue;

            Image img = toolButtons[i].GetComponent<Image>();

            // ถ้าเป็นปุ่มที่เลือก
            if ((int)currentTool == i + 1)
            {
                toolButtons[i].transform.localScale = selectedScale;

                if (img != null)
                    img.color = selectedColor;
            }
            else
            {
                toolButtons[i].transform.localScale = normalScale;

                if (img != null)
                    img.color = normalColor;
            }
        }
    }

    private string GetToolNameThai(BuildTool tool)
    {
        switch (tool)
        {
            case BuildTool.House: return "สร้างบ้าน";
            case BuildTool.Road: return "ถนน";
            case BuildTool.Furniture: return "เฟอร์นิเจอร์";
            case BuildTool.Wall: return "กำแพง";
            case BuildTool.Nature: return "ดอกไม้ต้นไม้";
            case BuildTool.Pond: return "บ่อน้ำ";
            case BuildTool.Eraser: return "ยางลบ";
            default: return "ยกเลิกการเลือก";
        }
    }
}