using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class RandomModifierPickup : MonoBehaviour
{
    [SerializeField] private PlayerModifier[] modifiers;
    [SerializeField] private bool destroyAfterUse = true;

    [SerializeField] private ModifierNotification notification;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string activateTriggerName = "Activate";
    [SerializeField] private float animationDuration = 0.5f;

    [Header("Audio")]
    [SerializeField] private AudioClip firstSound;
    [SerializeField] private AudioClip secondSound;

    private AudioSource audioSource;
    private bool used;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!used)
        {
            notification = collision.gameObject.GetComponent<ModifierNotification>();
            if (Input.GetKeyDown(KeyCode.F))
            {
                Activate(collision.gameObject.GetComponent<PlayerStats>());
            }
        }
    }

    public void Activate(PlayerStats playerStats)
    {
        if (used || playerStats == null || modifiers == null || modifiers.Length == 0)
            return;

        PlayerModifier modifier = modifiers[Random.Range(0, modifiers.Length)];

        if (modifier == null)
        {
            Debug.LogWarning("В списке модификаторов есть пустой элемент.", this);
            return;
        }

        used = true;
        ApplyModifier(playerStats, modifier);

        if (notification != null)
            notification.Show(modifier);

        Debug.Log($"Получен модификатор: {modifier.modifierName}");

        // Отключаем коллайдер, чтобы нельзя было активировать повторно
        var col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;

        // Запускаем корутину для анимации и последовательного воспроизведения звуков
        StartCoroutine(ActivateSequence());
    }

    private IEnumerator ActivateSequence()
    {
        // 1. Запускаем анимацию
        if (animator != null && !string.IsNullOrEmpty(activateTriggerName))
        {
            animator.SetTrigger(activateTriggerName);
        }

        // 2. Проигрываем первый звук и ждем окончания его длительности
        if (firstSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(firstSound);
            yield return new WaitForSeconds(firstSound.length);
        }

        // 3. Проигрываем второй звук и ждем окончания его длительности
        if (secondSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(secondSound);
            yield return new WaitForSeconds(secondSound.length);
        }

        // 4. Уничтожаем объект после того, как проиграли оба звука (если включено)
        if (destroyAfterUse)
        {
            Destroy(gameObject);
        }
    }

    private void ApplyModifier(PlayerStats stats, PlayerModifier modifier)
    {
        ApplyValue(stats, modifier, modifier.amount);

        if (modifier.duration > 0f)
            StartCoroutine(RemoveAfterDuration(stats, modifier));
    }

    private IEnumerator RemoveAfterDuration(
        PlayerStats stats,
        PlayerModifier modifier)
    {
        yield return new WaitForSeconds(modifier.duration);

        if (stats != null)
            ApplyValue(stats, modifier, -modifier.amount);
    }

    private void ApplyValue(
        PlayerStats stats,
        PlayerModifier modifier,
        float amount)
    {
        switch (modifier.stat)
        {
            case ModifierStat.MaxHealth:
                stats.ModifyMaxHealth(amount);
                break;

            case ModifierStat.InfectionIncrease:
                stats.ModifyInfectionRate(amount);
                break;

            case ModifierStat.SecondsPerInfection:
                stats.ModifySecondsPerInfection(amount);
                break;
        }
    }
}