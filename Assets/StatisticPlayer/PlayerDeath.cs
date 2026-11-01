using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class PlayerDeath : MonoBehaviour
{
    [Header("Анимация смерти")]
    [SerializeField] private Animator animator;
    [SerializeField] private string deathTriggerName = "Death";

    [Header("Блокировка управления")]
    [SerializeField] private MonoBehaviour[] componentsToDisable;

    [Header("UI смерти")]
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private TMP_Text deathCauseText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button menuButton;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Анимация появления панели")]
    [SerializeField] private bool animatePanel = true;
    [SerializeField] private float slideDuration = 0.6f;
    [SerializeField] private float slideOvershoot = 0.15f;
    [SerializeField] private float startOffsetY = 1200f;

    [Header("Пауза при смерти")]
    [SerializeField] private bool pauseOnDeath = true;

    [Header("Дебаг")]
    [SerializeField] private bool debug = true;

    private bool isDead;
    private DeathCause lastCause;
    private string lastCauseDetails;

    private RectTransform panelRect;
    private Vector2 panelTargetPos;

    public bool IsDead => isDead;
    public DeathCause LastCause => lastCause;
    public string LastCauseDetails => lastCauseDetails;

    public static event System.Action DeathShown;
    public static event System.Action DeathHidden;

    // =========================================================
    //  LIFECYCLE
    // =========================================================
    private void Awake()
    {
        if (deathPanel != null)
        {
            deathPanel.SetActive(false);
            panelRect = deathPanel.GetComponent<RectTransform>();
            if (panelRect != null)
                panelTargetPos = panelRect.anchoredPosition;
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(OnRestartPressed);
        }

        if (menuButton != null)
        {
            menuButton.onClick.RemoveAllListeners();
            menuButton.onClick.AddListener(OnMenuPressed);
        }
    }

    // =========================================================
    //  СМЕРТЬ
    // =========================================================
    public void Die() => Die(DeathCause.Unknown, null);

    public void Die(DeathCause cause) => Die(cause, null);

    public void Die(DeathCause cause, string details)
    {
        if (isDead) return;
        isDead = true;

        lastCause = cause;
        lastCauseDetails = details;

        string causeLabel = FormatCause(cause, details);
        Dbg($"Игрок умер: {causeLabel}");

        // отключаем управление
        foreach (var comp in componentsToDisable)
        {
            if (comp != null) comp.enabled = false;
        }

        // останавливаем физику
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // анимация
        if (animator != null && !string.IsNullOrEmpty(deathTriggerName))
        {
            animator.ResetTrigger(deathTriggerName);
            animator.SetTrigger(deathTriggerName);
        }

        // пауза
        if (pauseOnDeath)
            Time.timeScale = 0f;

        // курсор
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // панель
        if (deathPanel != null)
        {
            deathPanel.SetActive(true);

            if (animatePanel)
                StartCoroutine(AnimatePanelDrop());
        }
        else
        {
            Debug.LogError("[PlayerDeath] deathPanel не назначен");
        }

        // текст причины
        if (deathCauseText != null)
            deathCauseText.text = $"Причина смерти: {causeLabel}";
        else
            Dbg("deathCauseText не назначен — причина не отобразится");

        DeathShown?.Invoke();
    }

    private string FormatCause(DeathCause cause, string details)
    {
        string baseLabel = cause switch
        {
            DeathCause.Damage => "от урона",
            DeathCause.Infection => "от заражения",
            DeathCause.Timer => "время вышло",
            DeathCause.Falling => "от падения",
            DeathCause.Enemy => "от врага",
            DeathCause.PlayerBullet => "от собственной пули",
            DeathCause.Self => "самоубийство",
            _ => "неизвестно"
        };

        if (!string.IsNullOrEmpty(details))
            return $"{baseLabel} ({details})";

        return baseLabel;
    }

    // =========================================================
    //  АНИМАЦИЯ ПАДЕНИЯ ПАНЕЛИ
    // =========================================================
    private IEnumerator AnimatePanelDrop()
    {
        if (panelRect == null)
            panelRect = deathPanel.GetComponent<RectTransform>();

        if (panelRect == null)
            yield break;

        Vector2 startPos = panelTargetPos + new Vector2(0f, startOffsetY);
        panelRect.anchoredPosition = startPos;

        float t = 0f;
        while (t < slideDuration)
        {
            t += Time.unscaledDeltaTime;

            float k = Mathf.Clamp01(t / slideDuration);

            // ease-out cubic
            float eased = 1f - Mathf.Pow(1f - k, 3f);

            // лёгкий overshoot в конце
            float overshoot = 0f;
            if (slideOvershoot > 0f)
            {
                float p = (k - 0.7f) / 0.3f;
                if (p > 0f && p < 1f)
                    overshoot = Mathf.Sin(p * Mathf.PI) * slideOvershoot * startOffsetY * 0.1f;
            }

            panelRect.anchoredPosition =
                Vector2.LerpUnclamped(startPos, panelTargetPos, eased) -
                new Vector2(0f, overshoot);

            yield return null;
        }

        panelRect.anchoredPosition = panelTargetPos;
    }

    // =========================================================
    //  КНОПКИ
    // =========================================================
    private void OnRestartPressed()
    {
        Dbg("Перезапуск сцены");
        RestoreTimeAndCursor();
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }

    private void OnMenuPressed()
    {
        Dbg($"Переход в меню: {mainMenuSceneName}");
        RestoreTimeAndCursor();
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void RestoreTimeAndCursor()
    {
        Time.timeScale = 1f;
        DeathHidden?.Invoke();
    }

    private void Dbg(string msg)
    {
        if (debug) Debug.Log($"[PlayerDeath] {msg}");
    }
}

public enum DeathCause
{
    Unknown,
    Damage,
    Infection,
    Timer,
    Falling,
    Enemy,
    PlayerBullet,
    Self
}