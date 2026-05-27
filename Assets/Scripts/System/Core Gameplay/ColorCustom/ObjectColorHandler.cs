// ==============================
// ObjectColorHandler.cs  (Material Swap)
// ==============================

using UnityEngine;
using System.Collections.Generic;

public class ObjectColorHandler : MonoBehaviour
{
    [System.Serializable]
    public class ColorVariant
    {
        public string variantName = "Default";

        // สีของปุ่มใน UI
        public Color uiColor = Color.white;

        // Materials ที่จะ swap ไป — ใส่ให้ครบทุก renderer
        // เรียงลำดับให้ตรงกับ renderer ใน GetComponentsInChildren
        public Material[] materials;
    }

    [Header("Color Variants")]
    [SerializeField]
    private List<ColorVariant> colorVariants =
        new List<ColorVariant>();

    [Header("Settings")]
    [SerializeField]
    private int currentVariantIndex = 0;

    private Renderer[] cachedRenderers;

    void Awake()
    {
        cachedRenderers =
            GetComponentsInChildren<Renderer>();

        Debug.Log(
            "[COLOR] Renderers found: " +
            cachedRenderers.Length);
    }

    public void ApplyColor(int variantIndex)
    {
        if (colorVariants == null ||
            colorVariants.Count == 0)
        {
            Debug.LogWarning(
                "[COLOR] No variants on " +
                gameObject.name);
            return;
        }

        variantIndex = Mathf.Clamp(
            variantIndex, 0,
            colorVariants.Count - 1);

        currentVariantIndex = variantIndex;

        ColorVariant variant =
            colorVariants[variantIndex];

        if (variant.materials == null ||
            variant.materials.Length == 0)
        {
            Debug.LogWarning(
                "[COLOR] No materials in variant " +
                variant.variantName);
            return;
        }

        // swap material ทีละ renderer
        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            if (cachedRenderers[i] == null)
                continue;

            // ถ้า variant มี material ตรง index นี้ → swap
            // ถ้าไม่มี → ใช้อันสุดท้ายที่มี
            int matIndex = Mathf.Min(
                i, variant.materials.Length - 1);

            if (variant.materials[matIndex] != null)
            {
                cachedRenderers[i].material =
                    variant.materials[matIndex];
            }
        }

        Debug.Log(
            "[COLOR] " + gameObject.name +
            " => " + variant.variantName);
    }

    public void ApplyNextColor()
    {
        if (colorVariants == null ||
            colorVariants.Count == 0) return;

        ApplyColor(
            (currentVariantIndex + 1) %
            colorVariants.Count);
    }

    public void ApplyPrevColor()
    {
        if (colorVariants == null ||
            colorVariants.Count == 0) return;

        ApplyColor(
            (currentVariantIndex - 1 +
            colorVariants.Count) %
            colorVariants.Count);
    }

    // UI ใช้ uiColor แทน color เดิม
    public Color GetVariantUIColor(int index)
    {
        if (colorVariants == null ||
            index < 0 ||
            index >= colorVariants.Count)
            return Color.white;

        return colorVariants[index].uiColor;
    }

    public int GetCurrentVariantIndex()
        => currentVariantIndex;

    public int GetVariantCount()
        => colorVariants != null
        ? colorVariants.Count : 0;

    public ColorVariant GetCurrentVariant()
    {
        if (colorVariants == null ||
            colorVariants.Count == 0) return null;

        return colorVariants[currentVariantIndex];
    }

    public List<ColorVariant> GetAllVariants()
        => colorVariants;
}   