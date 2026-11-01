using UnityEngine;

public class CreditsTrigger : MonoBehaviour
{
    [Tooltip("Панель с титрами (включается при входе игрока)")]
    public GameObject creditsPanel;

    [System.Obsolete]
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // Отключаем скрипт движения целиком
        var player = other.GetComponent<PlayerController>();
        if (player != null)
            player.enabled = false;

        // Останавливаем физику, чтобы игрок не «доехал» по инерции
        var rb = other.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        player.GetComponent<StartTitries>().StartTeatres();

        // Показываем титры
        if (creditsPanel != null)
            creditsPanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
    }
}