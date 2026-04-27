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
    public int furnitureIndex = 0;
    public int natureIndex = 0;

    [Header("UI Setup")]
    public List<Button> toolButtons;

    [Header("Highlight Settings")]
    public Vector3 normalScale = Vector3.one;
    public Vector3 selectedScale = new Vector3(1.2f, 1.2f, 1.2f);
    public Color normalColor = Color.white;
    public Color selectedColor = Color.green;

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
            if (currentTool == BuildTool.Furniture) furnitureIndex++;
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
            if ((int)currentTool == i + 1)
            {
                toolButtons[i].transform.localScale = selectedScale;
                if (img != null) img.color = selectedColor;
            }
            else
            {
                toolButtons[i].transform.localScale = normalScale;
                if (img != null) img.color = normalColor;
            }
        }
    }
}