// ==============================
// ColorPickerUI.cs
// ==============================
// ใส่ไว้บน GameObject เดียวกับ Panel UI
// เรียก ShowFor() จาก EditTransformHandler
// หรือจาก PlacementSystem ตอน StartEditing

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ColorPickerUI : MonoBehaviour
{
    [Header("References")]
    // panel ที่ซ่อน/แสดง
    [SerializeField] private GameObject panel;

    // prefab ของปุ่มสีแต่ละอัน
    [SerializeField] private GameObject colorButtonPrefab;

    // container สำหรับวาง button
    [SerializeField] private Transform buttonContainer;

    private ObjectColorHandler currentTarget;

    private List<GameObject> spawnedButtons =
        new List<GameObject>();

    void Awake()
    {
        Hide();
    }

    // -------------------------------------------------------
    // ShowFor — เรียกตอน select TinyHouse
    // -------------------------------------------------------
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
            // object นี้ไม่มี color handler → ซ่อน UI
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

    // -------------------------------------------------------
    // สร้างปุ่มตาม variant ที่มี
    // -------------------------------------------------------
    private void BuildButtons()
    {
        // ลบปุ่มเก่าทิ้ง
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

        for (int i = 0; i < variants.Count; i++)
        {
            int index = i; // capture ลง closure

            GameObject btnObj =
                Instantiate(
                    colorButtonPrefab,
                    buttonContainer);

            // ตั้งสีของปุ่มให้ตรงกับ variant
            Image img =
                btnObj.GetComponent<Image>();

            if (img != null)
                img.color = variants[i].color;

            // ผูก onClick
            Button btn =
                btnObj.GetComponent<Button>();

            if (btn != null)
            {
                btn.onClick.AddListener(() =>
                {
                    OnColorButtonClicked(index);
                });
            }

            // tooltip ชื่อ variant (optional)
            // Text label = btnObj.GetComponentInChildren<Text>();
            // if (label) label.text = variants[i].variantName;

            spawnedButtons.Add(btnObj);
        }
    }

    private void OnColorButtonClicked(int index)
    {
        if (currentTarget == null)
            return;

        currentTarget.ApplyColor(index);

        Debug.Log(
            "[COLOR UI] Applied variant " +
            index);
    }
}