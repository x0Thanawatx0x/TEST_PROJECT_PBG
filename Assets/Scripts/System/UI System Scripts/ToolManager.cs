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

    // เพิ่ม houseIndex เพื่อให้ PlacementSystem เรียกใช้งานได้โดยไม่ Error[cite: 1]
    public int houseIndex = 0;
    public int furnitureIndex = 0;
    public int natureIndex = 0;

    [Header("UI Setup")]
    public List<Button> toolButtons;

    [Header("Highlight Settings")]
    public Vector3 normalScale = Vector3.one;
    public Vector3 selectedScale = new Vector3(1.15f, 1.15f, 1.15f); // ลดความใหญ่ลงนิดหน่อยให้ดูละมุนแบบ Cozy
    public Color normalColor = Color.white;
    public Color selectedColor = new Color(0.7f, 1f, 0.7f); // สีเขียวอ่อนแบบพาสเทล[cite: 3]

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
            // ถ้ากดซ้ำ จะเป็นการวนเปลี่ยนโมเดล (Cycle Models) ตามสไตล์ Tiny Glade
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
            Image img = toolButtons[i].GetComponent<Image>();

            bool isSelected = (int)currentTool == i + 1;

            // ใช้ความนุ่มนวลในการเปลี่ยน Visual (ถ้าจะให้ดีควรใช้ Tween)[cite: 2]
            toolButtons[i].transform.localScale = isSelected ? selectedScale : normalScale;
            if (img != null) img.color = isSelected ? selectedColor : normalColor;
        }
    }
}