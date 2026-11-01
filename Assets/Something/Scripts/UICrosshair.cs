using UnityEngine;

public class UICrosshair : MonoBehaviour
{
    public RectTransform rectTransform;

    private void Awake()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();
    }

    private void Start()
    {
        Cursor.visible = false;

        if (SkillTreeUI.Instance != null)
        {
            SkillTreeUI.Instance.Opened += OnTreeOpened;
            SkillTreeUI.Instance.Closed += OnTreeClosed;
        }
    }

    private void OnDestroy()
    {
        if (SkillTreeUI.Instance != null)
        {
            SkillTreeUI.Instance.Opened -= OnTreeOpened;
            SkillTreeUI.Instance.Closed -= OnTreeClosed;
        }
    }

    private void Update()
    {
        if (!enabled) return;

        // Прицел следует за мышью в screen-space.
        // Для Overlay Canvas: позиция = мышь, для Camera/World — через canvas.
        var canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            rectTransform.position = Input.mousePosition;
        else if (canvas != null)
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                Input.mousePosition,
                canvas.worldCamera,
                out var local);
        else
            rectTransform.position = Input.mousePosition;
    }

    private void OnTreeOpened()
    {
        enabled = false;
        if (rectTransform != null) rectTransform.gameObject.SetActive(false);
    }

    private void OnTreeClosed()
    {
        if (rectTransform != null) rectTransform.gameObject.SetActive(true);
        enabled = true;
    }
}