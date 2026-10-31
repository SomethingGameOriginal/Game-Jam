using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ModifierNotification : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;

    [SerializeField] private float fadeDuration = 0.35f;
    [SerializeField] private float visibleDuration = 2.5f;

    private Coroutine currentRoutine;

    private void Awake()
    {
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    public void Show(PlayerModifier modifier)
    {
        if (modifier == null) return;

        gameObject.SetActive(true);

        icon.sprite = modifier.icon;
        icon.enabled = modifier.icon != null;
        titleText.text = modifier.modifierName;
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

        gameObject.SetActive(false);
        currentRoutine = null;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
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