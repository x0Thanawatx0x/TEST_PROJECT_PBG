using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem; // มารับตั๋วระบบใหม่ของ Unity 6 ตรงนี้
using System.Collections.Generic;

public class ToolManager : MonoBehaviour
{
    public enum BuildTool
    {
        None = 0, House = 1, Road = 2, Furniture = 3,
        Wall = 4, Nature = 5, Pond = 6, Eraser = 7,
        Mountain = 8,
        HouseGen = 9 // ✨ เพิ่มประเภทที่ 9 สล็อตสร้างบ้านของพวกเรา
    }

    [Header("Current Status")]
    public BuildTool currentTool = BuildTool.None;

    public int houseIndex = 0;
    public int furnitureIndex = 0;
    public int natureIndex = 0;

    [Header("UI Setup")]
    public List<Button> toolButtons;

    [Header("Highlight Settings (Legacy)")]
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
        // 1. Shortcut Keys 1-8 ระบบดั้งเดิมของปิ๊บ
        for (int i = 1; i <= 8; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0 + i))
                SelectTool(i);
        }
        if (Input.GetKeyDown(KeyCode.Keypad8)) SelectTool(8);
        if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Escape)) SelectTool(0);

        // 2. 🌟 [ระบบ Toggle เลข 9] กดสลับเปิด-ปิดจบใน Class นี้เลยตามสั่ง
        if (Keyboard.current.digit9Key.wasPressedThisFrame)
        {
            if (currentTool == BuildTool.HouseGen)
            {
                SelectTool(0); // ถ้าเปิดอยู่แล้ว กด 9 อีกทีจะปิดสลับเป็น None (มือเปล่า)
            }
            else
            {
                SelectTool(9); // ถ้าปิดอยู่ กด 9 จะเข้าโหมดสล็อตสร้างบ้านทันที
            }
        }
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
            Animator anim = toolButtons[i].GetComponent<Animator>();
            bool isSelected = (int)currentTool == i + 1;

            if (anim != null) anim.SetBool("IsSelected", isSelected);
            else
            {
                Image img = toolButtons[i].GetComponent<Image>();
                toolButtons[i].transform.localScale = isSelected ? selectedScale : normalScale;
                if (img != null) img.color = isSelected ? selectedColor : normalColor;
            }
        }
    }
}