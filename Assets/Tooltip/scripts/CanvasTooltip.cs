using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Canvas подсказка. Вызывается через CanvasTooltip.Show("текст", sprite).
/// </summary>
public class CanvasTooltip : MonoBehaviour
{
    public static CanvasTooltip Instance { get; private set; }

    [Header("UI ссылки")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform tooltipRect;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image background;

    [Header("Настройки")]
    [SerializeField] private float fadeDuration = 0.2f;
    [SerializeField] private float defaultDuration = 3f;
    [SerializeField] private Vector2 screenOffset = new Vector2(0, -150f);

    private Coroutine _hideCoroutine;
    private Coroutine _fadeCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Показать подсказку.
    /// </summary>
    /// <param name="text">Текст (обязательно)</param>
    /// <param name="icon">Картинка (опционально, null — без картинки)</param>
    /// <param name="duration">Время показа. Если <= 0 — использовать defaultDuration</param>
    public static void Show(string text, Sprite icon = null, float duration = -1f)
    {
        if (Instance == null)
        {
            Debug.LogWarning("[CanvasTooltip] Не найден на сцене.");
            return;
        }
        Instance.ShowInternal(text, icon, duration);
    }

    /// <summary>Скрыть подсказку принудительно.</summary>
    public static void Hide()
    {
        if (Instance != null) Instance.HideInternal();
    }

    private void ShowInternal(string text, Sprite icon, float duration)
    {
        if (_hideCoroutine != null) StopCoroutine(_hideCoroutine);
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);

        gameObject.SetActive(true);

        messageText.text = text;

        if (icon != null)
        {
            iconImage.sprite = icon;
            iconImage.gameObject.SetActive(true);
        }
        else
        {
            iconImage.gameObject.SetActive(false);
        }

        // Позиция — например, снизу экрана (можно заменить на позицию курсора/объекта)
        if (tooltipRect != null)
            tooltipRect.anchoredPosition = screenOffset;

        if (duration <= 0f) duration = defaultDuration;
        _hideCoroutine = StartCoroutine(HideAfter(duration));
        _fadeCoroutine = StartCoroutine(FadeTo(1f));
    }

    private void HideInternal()
    {
        if (_hideCoroutine != null) StopCoroutine(_hideCoroutine);
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(FadeTo(0f, () => gameObject.SetActive(false)));
    }

    private IEnumerator HideAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        HideInternal();
    }

    private IEnumerator FadeTo(float target, System.Action onDone = null)
    {
        float start = canvasGroup.alpha;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, target, t / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = target;
        onDone?.Invoke();
    }
}