using UnityEngine;
using UnityEngine.UI;

public class UICrosshair : MonoBehaviour
{
    public RectTransform rectTransform;

    private Image crosshairImage;
    private CanvasGroup crosshairCanvasGroup;
    private bool subscribedToTitres;

    private void Awake()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (rectTransform != null)
        {
            crosshairImage = rectTransform.GetComponent<Image>();
            crosshairCanvasGroup = rectTransform.GetComponent<CanvasGroup>();
        }
    }

    private void Start()
    {
        // Древо навыков — как было.
        if (SkillTreeUI.Instance != null)
        {
            SkillTreeUI.Instance.Opened += OnTreeOpened;
            SkillTreeUI.Instance.Closed += OnTreeClosed;
        }

        // Титры — по той же схеме.
        if (StartTitries.Instance != null)
        {
            StartTitries.Instance.Opened += OnTitresOpened;
            StartTitries.Instance.Closed += OnTitresClosed;
            subscribedToTitres = true;
        }

        // Стартовое состояние.
        bool titresActive = StartTitries.Instance != null && StartTitries.Instance.IsActive;
        SetCrosshairVisible(!titresActive);

        if (!titresActive)
            Cursor.visible = false;
    }

    private void OnDestroy()
    {
        if (SkillTreeUI.Instance != null)
        {
            SkillTreeUI.Instance.Opened -= OnTreeOpened;
            SkillTreeUI.Instance.Closed -= OnTreeClosed;
        }

        if (subscribedToTitres && StartTitries.Instance != null)
        {
            StartTitries.Instance.Opened -= OnTitresOpened;
            StartTitries.Instance.Closed -= OnTitresClosed;
        }
    }

    private void Update()
    {
        if (StartTitries.Instance != null && StartTitries.Instance.IsActive)
            return;

        var canvas = GetComponentInParent<Canvas>();

        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            rectTransform.position = Input.mousePosition;
        }
        else if (canvas != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                Input.mousePosition,
                canvas.worldCamera,
                out var local);

            rectTransform.localPosition = local;
        }
        else
        {
            rectTransform.position = Input.mousePosition;
        }
    }

    private void OnTitresOpened()
    {
        SetCrosshairVisible(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnTitresClosed()
    {
        SetCrosshairVisible(true);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnTreeOpened() => SetCrosshairVisible(false);

    private void OnTreeClosed()
    {
        if (StartTitries.Instance != null && StartTitries.Instance.IsActive) return;

        SetCrosshairVisible(true);
        Cursor.visible = false;
    }

    private void SetCrosshairVisible(bool visible)
    {
        if (rectTransform == null) return;

        // Как в древе навыков — не убиваем компонент, а скрываем визуал.
        if (crosshairImage != null)
            crosshairImage.enabled = visible;

        if (crosshairCanvasGroup != null)
        {
            crosshairCanvasGroup.alpha = visible ? 1f : 0f;
            crosshairCanvasGroup.blocksRaycasts = visible;
            crosshairCanvasGroup.interactable = visible;
        }
    }
}