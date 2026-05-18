using UnityEngine;

[CreateAssetMenu(fileName = "NewItemData", menuName = "PlanBuilder/ItemData")]
public class ItemData : ScriptableObject
{
    public string itemName;

    [Header("Real Object")]
    public GameObject prefab;

    [Header("Preview Object")]
    public GameObject previewPrefab;

    [Header("UI")]
    public Sprite icon;

    public string category;
}