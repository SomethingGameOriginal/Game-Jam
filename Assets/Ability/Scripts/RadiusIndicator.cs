using UnityEngine;

public class RadiusIndicator : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private GroundSlamAbility ability;
    [SerializeField] private Transform player;
    [SerializeField] private Transform radiusVisual;
    [SerializeField] private SpriteRenderer radiusRenderer;

    [Header("Анимация")]
    [Tooltip("Animator на объекте-круге. Если не задан — используется простое расширение скейлом.")]
    [SerializeField] private Animator animator;
    [Tooltip("Имя триггера в Animator, который запускает анимацию удара")]
    [SerializeField] private string slamTriggerName = "Slam";
    [Tooltip("Длительность твоей анимации (сек) — когда скрывать круг после запуска")]
    [SerializeField] private float animationDuration = 0.4f;

    [Header("Fallback (если Animator не задан)")]
    [SerializeField] private float expandDuration = 0.18f;
    [SerializeField] private float fadeOutDuration = 0.25f;
    [SerializeField] private float overshoot = 1.1f;
    [SerializeField] private Color activeColor = new Color(1f, 0.2f, 0f, 0.6f);

    private bool animating;

    private void Awake()
    {
        // Круг скрыт по умолчанию — показываем только при ударе
        if (radiusRenderer != null)
            radiusRenderer.enabled = false;
    }

    private void Update()
    {
        if (player == null || radiusVisual == null) return;

        // Круг всегда следует за игроком
        radiusVisual.position = player.position;
    }

    /// <summary>
    /// Запустить анимацию удара. Вызывается из GroundSlamAbility.
    /// </summary>
    public void PlaySlamAnimation()
    {
        if (animating) return;
        animating = true;

        if (radiusRenderer != null)
            radiusRenderer.enabled = true;

        if (animator != null)
            StartCoroutine(PlayAnimatorRoutine());
        else
            StartCoroutine(FallbackRoutine());
    }

    private System.Collections.IEnumerator PlayAnimatorRoutine()
    {
        // Устанавливаем финальный масштаб (сам визуал анимируется Animator'ом)
        SetRadiusScale(ability.DamageRadius);

        animator.ResetTrigger(slamTriggerName);
        animator.SetTrigger(slamTriggerName);

        yield return new WaitForSeconds(animationDuration);

        if (radiusRenderer != null)
            radiusRenderer.enabled = false;

        animating = false;
    }

    private System.Collections.IEnumerator FallbackRoutine()
    {
        if (radiusRenderer != null)
            radiusRenderer.color = activeColor;

        float maxRadius = ability.DamageRadius * overshoot;

        // Расширение 0 -> maxRadius
        float t = 0f;
        while (t < expandDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / expandDuration);
            float r = Mathf.Lerp(0f, maxRadius, 1f - (1f - k) * (1f - k));
            SetRadiusScale(r);
            yield return null;
        }

        // Затухание maxRadius -> damageRadius
        t = 0f;
        Color startColor = radiusRenderer.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / fadeOutDuration);
            float r = Mathf.Lerp(maxRadius, ability.DamageRadius, k);
            SetRadiusScale(r);
            radiusRenderer.color = Color.Lerp(startColor, endColor, k);
            yield return null;
        }

        if (radiusRenderer != null)
            radiusRenderer.enabled = false;

        animating = false;
    }

    private void SetRadiusScale(float r)
    {
        radiusVisual.localScale = new Vector3(r * 2f, r * 2f, 1f);
    }
}