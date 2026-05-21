// ==============================
// ObjectColorHandler.cs
// ==============================
// ใช้กับ object ที่มี tag "TinyHouse"
// เรียก ApplyColor() จาก UI หรือ script อื่น
// ออกแบบให้ upgrade เป็น material swap ได้ในอนาคต

using UnityEngine;
using System.Collections.Generic;

public class ObjectColorHandler : MonoBehaviour
{
    // -------------------------------------------------------
    // ColorVariant — เก็บข้อมูล 1 ตัวเลือกสี
    // อนาคตเพิ่ม material ตรงนี้ได้เลย
    // -------------------------------------------------------
    [System.Serializable]
    public class ColorVariant
    {
        public string variantName = "Default";
        public Color color = Color.white;

        // TODO: เพิ่มตรงนี้เมื่อมีโมเดลจริง
        // public Material material;
    }

    [Header("Color Variants")]
    [SerializeField]
    private List<ColorVariant> colorVariants =
        new List<ColorVariant>();

    [Header("Settings")]
    // index ของ variant ที่เลือกอยู่ตอนนี้
    [SerializeField]
    private int currentVariantIndex = 0;

    // cache renderers เพื่อไม่ต้อง GetComponent ทุกครั้ง
    private Renderer[] cachedRenderers;

    // เก็บ material instance แยกต่อ object
    // ไม่ให้แก้ shared material
    private List<Material> instanceMaterials =
        new List<Material>();

    void Awake()
    {
        CacheAndInstanceMaterials();
    }

    // -------------------------------------------------------
    // เรียกตอน apply สีครั้งแรก หรือ reset
    // -------------------------------------------------------
    private void CacheAndInstanceMaterials()
    {
        cachedRenderers =
            GetComponentsInChildren<Renderer>();

        instanceMaterials.Clear();

        foreach (Renderer r in cachedRenderers)
        {
            // [Unity 6 Guard] ป้องกันในกรณีที่ Renderer ไม่มี material คอนฟิกไว้ล่วงหน้า
            if (r.material == null) continue;

            // สร้าง instance material แยก
            // ไม่กระทบ prefab หรือ object อื่น
            Material inst =
                new Material(r.material);

            r.material = inst;

            instanceMaterials.Add(inst);
        }
    }

    // -------------------------------------------------------
    // ApplyColor — เรียกจาก UI
    // -------------------------------------------------------
    public void ApplyColor(int variantIndex)
    {
        if (colorVariants == null ||
            colorVariants.Count == 0)
        {
            Debug.LogWarning(
                "[COLOR] No variants defined on " +
                gameObject.name);
            return;
        }

        // [Safe-Guard] ตรวจสอบความชัวร์ว่าแคชแมททีเรียลไม่ได้หลุดจากการโหลดเฟรมแรก
        if (instanceMaterials == null || instanceMaterials.Count == 0 || cachedRenderers == null || cachedRenderers.Length == 0)
        {
            CacheAndInstanceMaterials();
        }

        variantIndex = Mathf.Clamp(
            variantIndex,
            0,
            colorVariants.Count - 1);

        currentVariantIndex = variantIndex;

        Color targetColor =
            colorVariants[variantIndex].color;

        foreach (Material mat in instanceMaterials)
        {
            if (mat != null)
                mat.color = targetColor;
        }

        Debug.Log(
            "[COLOR] " + gameObject.name +
            " => " + colorVariants[variantIndex].variantName);
    }

    // overload — เรียกด้วย Color โดยตรง
    public void ApplyColor(Color color)
    {
        // [Safe-Guard] รันซ้ำเผื่อกรณีวัตถุเพิ่งสปอว์นกลางคันแล้วแคชไม่ทัน
        if (instanceMaterials == null || instanceMaterials.Count == 0)
        {
            CacheAndInstanceMaterials();
        }

        foreach (Material mat in instanceMaterials)
        {
            if (mat != null)
                mat.color = color;
        }
    }

    // -------------------------------------------------------
    // ApplyNextColor / ApplyPrevColor
    // ใช้กับปุ่ม < > บน UI ได้เลย
    // -------------------------------------------------------
    public void ApplyNextColor()
    {
        if (colorVariants == null ||
            colorVariants.Count == 0)
            return;

        int next =
            (currentVariantIndex + 1) %
            colorVariants.Count;

        ApplyColor(next);
    }

    public void ApplyPrevColor()
    {
        if (colorVariants == null ||
            colorVariants.Count == 0)
            return;

        int prev =
            (currentVariantIndex - 1 +
            colorVariants.Count) %
            colorVariants.Count;

        ApplyColor(prev);
    }

    // -------------------------------------------------------
    // Getters
    // -------------------------------------------------------
    public int GetCurrentVariantIndex()
        => currentVariantIndex;

    public int GetVariantCount()
        => colorVariants != null
        ? colorVariants.Count
        : 0;

    public ColorVariant GetCurrentVariant()
    {
        if (colorVariants == null ||
            colorVariants.Count == 0)
            return null;

        return colorVariants[currentVariantIndex];
    }

    public List<ColorVariant> GetAllVariants()
        => colorVariants;

    // -------------------------------------------------------
    // [Memory Clean Up] ล้างขยะความจำเมื่อบ้านหลังนี้ถูกกดลบหรือทำลาย
    // -------------------------------------------------------
    private void OnDestroy()
    {
        if (instanceMaterials != null)
        {
            foreach (Material mat in instanceMaterials)
            {
                if (mat != null)
                {
                    // ทำลายอินสแตนซ์ทิ้งเพื่อคืนค่า Ram/Vram ให้ Unity 6
                    Destroy(mat);
                }
            }
            instanceMaterials.Clear();
        }
    }

    // -------------------------------------------------------
    // TODO: SwapMaterial() — อัปเกรดตรงนี้ในอนาคต
    // -------------------------------------------------------
    // public void SwapMaterial(int variantIndex)
    // {
    //      Material mat = colorVariants[variantIndex].material;
    //      foreach (Renderer r in cachedRenderers)
    //          r.material = mat;
    // }
}