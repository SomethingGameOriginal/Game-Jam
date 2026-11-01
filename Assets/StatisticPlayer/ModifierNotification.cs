using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ModifierNotification : MonoBehaviour
{
    [Header("Визуал (дочерний объект, который включается/выключается)")]
    [SerializeField] private GameObject visualRoot;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Контент")]
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("Тайминги")]
    [SerializeField] private float fadeDuration = 0.35f;
    [SerializeField] private float visibleDuration = 2.5f;

    private Coroutine currentRoutine;

    private void Awake()
    {
        // Скрываем только визуал, сам скрипт остаётся активным.
        if (visualRoot != null)
            visualRoot.SetActive(false);

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    public void Show(PlayerModifier modifier)
    {
        if (modifier == null) return;

        if (visualRoot != null)
            visualRoot.SetActive(true);

        if (icon != null)
        {
            icon.sprite = modifier.icon;
            icon.enabled = modifier.icon != null;
        }

        if (titleText != null)
            titleText.text = modifier.modifierName;

        if (descriptionText != null)
            descriptionText.text = modifier.description;

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        yield return Fade(0f, 1f, fadeDuration);
        yield return new WaitForSeconds(visibleDuration);
        yield return Fade(1f, 0f, fadeDuration);

        if (visualRoot != null)
            visualRoot.SetActive(false);

        currentRoutine = null;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (canvasGroup == null) yield break;

        if (duration <= 0f)
        {
            canvasGroup.alpha = to;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = to;
    }
}