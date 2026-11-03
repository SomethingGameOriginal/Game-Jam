using System;
using UnityEngine;
using UnityEngine.UI;

public class GameTimer : MonoBehaviour
{
    [SerializeField] private float startTime = 60f;
    [SerializeField] private Text timerText;

    [Header("Смерть по таймеру")]
    [SerializeField] private PlayerDeath playerDeath;

    [Header("Дебаг")]
    [SerializeField] private bool debug = false;

    private float timeLeft;
    private bool timeUp;
    private bool initialized;

    public event Action OnTimeUp;

    public bool IsTimeUp => timeUp;
    public bool Use = false;

    // =========================================================
    //  LIFECYCLE
    // =========================================================
    private void Start()
    {
        if (SkillTreeManager.Instance != null)
            SkillTreeManager.Instance.Changed += OnSkillsChanged;

        ResetTimer();
        initialized = true;
    }

    private void OnDestroy()
    {
        if (SkillTreeManager.Instance != null)
            SkillTreeManager.Instance.Changed -= OnSkillsChanged;
    }

    private void OnSkillsChanged()
    {
        // если таймер ещё не запущен — обновляем стартовое время
        // (после старта бонус StartingTime уже не должен влиять)
        if (!initialized || timeUp) return;
        if (timeLeft > startTime + 0.01f) return; // уже шёл — не трогаем
        // (опционально) можно вообще не пересчитывать после старта — см. комментарий ниже
    }

    private void ResetTimer()
    {
        float bonus = SkillTreeManager.Instance != null
            ? SkillTreeManager.Instance.GetBonus(SkillBonusType.StartingTime)
            : 0f;

        timeLeft = Mathf.Max(0f, startTime + bonus);
        timeUp = false;

        UpdateDisplay();

        if (debug)
            Debug.Log($"[GameTimer] Стартовое время: {timeLeft} " +
                      $"(base={startTime}, bonus={bonus})");
    }

    // =========================================================
    //  UPDATE
    // =========================================================
    private void Update()
    {
        if (timeUp) return;

        if (timeLeft <= 0f && !Use)
        {
            timeUp = true;
            Debug.Log("[GameTimer] Время вышло!");

            OnTimeUp?.Invoke();

            if (playerDeath != null)
                playerDeath.Die(DeathCause.Timer);

            Use = true;
            return;
        }

        if (timeLeft <= 0f) return;

        timeLeft -= Time.deltaTime;
        timeLeft = Mathf.Max(0f, timeLeft);

        UpdateDisplay();
    }

    public void AddTime(float seconds)
    {
        if (seconds > 0f && !timeUp)
            timeLeft += seconds;
        UpdateDisplay();
    }

    public void LessTime(float seconds)
    {
        if (seconds > 0f && !timeUp)
            timeLeft -= seconds;
        UpdateDisplay();
    }

    public float GetTime() => timeLeft;

    private void UpdateDisplay()
    {
        if (timerText != null)
        {
            int seconds = Mathf.CeilToInt(timeLeft);
            timerText.text = $"{seconds / 60:00}:{seconds % 60:00}";
        }
    }
}