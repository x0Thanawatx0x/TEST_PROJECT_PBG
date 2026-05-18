using UnityEngine;
using TMPro;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance;

    [SerializeField] private RectTransform tooltipRect;
    [SerializeField] private TextMeshProUGUI tooltipText;
    [SerializeField] private Canvas canvas;

    [SerializeField] private Vector2 offset = new Vector2(15f, -15f);

    private void Awake()
    {
        Instance = this;

        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!gameObject.activeSelf) return;

        FollowMouse();
    }

    private void FollowMouse()
    {
        Vector2 pos;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            Input.mousePosition,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera,
            out pos
        );

        tooltipRect.anchoredPosition = pos + offset;
    }

    public void ShowTooltip(string text)
    {
        tooltipText.text = text;

        gameObject.SetActive(true);

        FollowMouse();
    }

    public void HideTooltip()
    {
        gameObject.SetActive(false);
    }
}