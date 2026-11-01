using System;
using UnityEngine;
using UnityEngine.UI;

public class GameTimer : MonoBehaviour
{
    [SerializeField] private float startTime = 60f;
    [SerializeField] private Text timerText;

    [Header("Смерть по таймеру")]
    [SerializeField] private PlayerDeath playerDeath;

    private float timeLeft;
    private bool timeUp;

    /// <summary>Событие: время вышло. Можно подписаться из других скриптов.</summary>
    public event Action OnTimeUp;

    public bool IsTimeUp => timeUp;

    private void Start()
    {
        timeLeft = startTime;
        UpdateDisplay();
    }

    private void Update()
    {
        if (timeUp) return;
        if (timeLeft <= 0f) return;

        timeLeft -= Time.deltaTime;
        timeLeft = Mathf.Max(0f, timeLeft);

        UpdateDisplay();

        if (timeLeft <= 0f)
        {
            timeUp = true;
            Debug.Log("[GameTimer] Время вышло!");

            OnTimeUp?.Invoke();

            if (playerDeath != null)
                playerDeath.Die();
        }
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