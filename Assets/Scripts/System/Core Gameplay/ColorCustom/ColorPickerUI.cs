// ==============================
// ColorPickerUI.cs
// ==============================

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ColorPickerUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject colorButtonPrefab;
    [SerializeField] private Transform buttonContainer;

    private ObjectColorHandler currentTarget;

    private List<GameObject> spawnedButtons =
        new List<GameObject>();

    void Awake()
    {
        Hide();
    }

    public void ShowFor(GameObject obj)
    {
        if (obj == null)
        {
            Hide();
            return;
        }

        ObjectColorHandler handler =
            obj.GetComponent<ObjectColorHandler>();

        if (handler == null)
        {
            Hide();
            return;
        }

        currentTarget = handler;

        BuildButtons();

        if (panel)
            panel.SetActive(true);
    }

    public void Hide()
    {
        currentTarget = null;

        if (panel)
            panel.SetActive(false);
    }

    private void BuildButtons()
    {
        foreach (GameObject btn in spawnedButtons)
        {
            if (btn != null)
                Destroy(btn);
        }

        spawnedButtons.Clear();

        if (currentTarget == null ||
            colorButtonPrefab == null ||
            buttonContainer == null)
            return;

        List<ObjectColorHandler.ColorVariant> variants =
            currentTarget.GetAllVariants();

        if (variants == null || variants.Count == 0)
            return;

        for (int i = 0; i < variants.Count; i++)
        {
            int index = i;

            GameObject btnObj =
                Instantiate(
                    colorButtonPrefab,
                    buttonContainer);

            // ใช้ uiColor สำหรับสีปุ่ม
            Image img =
                btnObj.GetComponent<Image>();

            if (img != null)
                img.color = variants[i].uiColor;

            Button btn =
                btnObj.GetComponent<Button>();

            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();

                btn.onClick.AddListener(() =>
                {
                    OnColorButtonClicked(index);
                });
            }

            spawnedButtons.Add(btnObj);
        }
    }

    private void OnColorButtonClicked(int index)
    {
        if (currentTarget == null)
            return;

        currentTarget.ApplyColor(index);
    }
}