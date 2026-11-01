using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerDeath : MonoBehaviour
{
    [Header("Анимация смерти")]
    [SerializeField] private Animator animator;
    [SerializeField] private string deathTriggerName = "Death";

    [Header("Тайминги")]
    [Tooltip("Через сколько секунд после смерти перезапустить сцену")]
    [SerializeField] private float restartDelay = 0;

    [Header("Блокировка управления")]
    [Tooltip("Компоненты, которые нужно выключить при смерти (например, PlayerMovement, GroundSlamAbility)")]
    [SerializeField] private MonoBehaviour[] componentsToDisable;

    private bool isDead;

    public bool IsDead => isDead;

    /// <summary>
    /// Вызвать при любой причине смерти: хп=0, заражение=100, таймер=0.
    /// Безопасно вызывать повторно — сработает только один раз.
    /// </summary>
    public void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("[PlayerDeath] Игрок умер. Причина смерти зафиксирована.");

        // Отключаем управление
        foreach (var comp in componentsToDisable)
        {
            if (comp != null)
                comp.enabled = false;
        }

        // Останавливаем физику/движение (если есть Rigidbody2D)
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        // Запускаем анимацию
        if (animator != null && !string.IsNullOrEmpty(deathTriggerName))
        {
            animator.ResetTrigger(deathTriggerName);
            animator.SetTrigger(deathTriggerName);
        }
        else
        {
            Debug.LogWarning("[PlayerDeath] Animator или имя триггера не заданы — анимация не запущена.");
        }

        StartCoroutine(RestartRoutine());
    }

    private IEnumerator RestartRoutine()
    {
        yield return new WaitForSeconds(restartDelay);

        Debug.Log("[PlayerDeath] Перезапуск сцены.");

        // Сбрасываем timeScale на случай, если где-то было замедление
        Time.timeScale = 1f;

        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }
}