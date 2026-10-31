using UnityEngine;
using UnityEngine.UI;

public class GameTimer : MonoBehaviour
{
    [SerializeField] private float startTime = 60f;
    [SerializeField] private Text timerText;

    private float timeLeft;

    private void Start()
    {
        timeLeft = startTime;
        UpdateDisplay();
    }

    private void Update()
    {
        if (timeLeft <= 0f)
            return;

        timeLeft -= Time.deltaTime;
        timeLeft = Mathf.Max(0f, timeLeft);

        UpdateDisplay();

        if (timeLeft <= 0f)
        {
            Debug.Log("Время вышло!");
            // Здесь вызовите Game Over или остановку игры.
        }
    }

    public void AddTime(float seconds)
    {
        if (seconds > 0f)
            timeLeft += seconds;

        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (timerText != null)
        {
            int seconds = Mathf.CeilToInt(timeLeft);
            timerText.text = $"{seconds / 60:00}:{seconds % 60:00}";
        }
    }
}