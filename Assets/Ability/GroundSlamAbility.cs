using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerStats))]
public class GroundSlamAbility : MonoBehaviour
{
    [Header("Доступ")]
    [Tooltip("Минимальное заражение для активации способности")]
    [SerializeField] private float requiredInfection = 50f;
    [SerializeField] private KeyCode activationKey = KeyCode.C;

    [Header("Удар по земле")]
    [SerializeField] private float damageRadius = 4f;
    [SerializeField] private int damage = 2;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float cooldown = 3f;
    [SerializeField] private float shockwaveDuration = 0.25f;

    [Header("Побочный эффект — рост заражения")]
    [SerializeField] private float infectionPerTick = 2f;
    [SerializeField] private float secondsPerTick = 1f;
    [SerializeField] private float effectDuration = 5f;

    private PlayerStats stats;
    private float cooldownTimer;
    private bool isActive;

    /// <summary>Флаг, что способность уже получена.</summary>
    public bool IsUnlocked { get; private set; }

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
        IsUnlocked = true; // ВРЕМЕННО
    }

    private void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        if (!IsUnlocked) return;

        if (Input.GetKeyDown(activationKey) && CanActivate())
            Activate();
    }

    private bool CanActivate()
    {
        return !isActive
            && cooldownTimer <= 0f
            && stats.Infection >= requiredInfection;
    }

    /// <summary>
    /// Выдать способность игроку и показать уведомление.
    /// Вызывать из скрипта, из Timeline (Signal) или при подборе предмета.
    /// </summary>
    public void Unlock()
    {
        if (IsUnlocked) return;

        IsUnlocked = true;

        // Показываем сообщение через уже существующий ModifierNotification
        ModifierNotification notification =
            FindAnyObjectByType<ModifierNotification>();

        if (notification != null)
        {
            PlayerModifier dummy = ScriptableObject.CreateInstance<PlayerModifier>();
            dummy.modifierName = "Способность: Удар по земле";
            dummy.description =
                $"Нажмите {activationKey}, чтобы ударить по земле. " +
                $"Требуется заражение {requiredInfection}+. " +
                $"Увеличивает заражение на {infectionPerTick} каждые {secondsPerTick} сек.";
            // icon оставьте null или назначьте свой — dummy.icon = myIcon;
            notification.Show(dummy);
        }
        else
        {
            Debug.Log("[GroundSlamAbility] Способность получена, но ModifierNotification не найден.");
        }
    }

    private void Activate()
    {
        isActive = true;
        cooldownTimer = cooldown;

        StartCoroutine(SlamRoutine());
        StartCoroutine(InfectionPenaltyRoutine());
    }

    private IEnumerator SlamRoutine()
    {
        // Простой визуальный «толчок» (можно заменить на реальный эффект/анимацию)
        Debug.Log("[GroundSlamAbility] Удар по земле!");

        Vector2 center = transform.position;

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            center,
            damageRadius,
            enemyLayer
        );

        foreach (Collider2D hit in hits)
        {
            if (hit == null) continue;

            // Пробуем найти EnemyChase2D (или наследника — RangedEnemy и т.п.)
            EnemyChase2D enemy = hit.GetComponentInParent<EnemyChase2D>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                continue;
            }

            // Запасной вариант — любой компонент с методом TakeDamage(int)
            // если у вас несколько типов врагов.
        }

        yield return new WaitForSeconds(shockwaveDuration);
        isActive = false;
    }

    private IEnumerator InfectionPenaltyRoutine()
    {
        float elapsed = 0f;

        while (elapsed < effectDuration)
        {
            yield return new WaitForSeconds(secondsPerTick);
            elapsed += secondsPerTick;

            // Наносим «урон» заражению в минус (т.е. увеличиваем)
            // через публичный метод ReduceInfection с отрицательным аргументом
            // — либо добавьте отдельный метод AddInfection в PlayerStats.
            stats.ReduceInfection(-infectionPerTick);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, damageRadius);
    }
}