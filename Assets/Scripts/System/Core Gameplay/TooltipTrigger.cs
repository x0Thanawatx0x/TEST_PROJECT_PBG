using UnityEngine;
using UnityEngine.EventSystems;

public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [TextArea]
    public string content; // ข้อความที่จะโชว์

    public void OnPointerEnter(PointerEventData eventData)
    {
        TooltipManager.Instance.ShowTooltip(content);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipManager.Instance.HideTooltip();
    }

    // กรณีปุ่มถูกปิดการใช้งาน (Disable) ขณะเมาส์วางอยู่ ให้ซ่อน Tooltip ด้วย
    private void OnDisable()
    {
        if (TooltipManager.Instance != null)
            TooltipManager.Instance.HideTooltip();
    }
}