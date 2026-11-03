using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss2D : EnemyChase2D
{
    [Header("Босс — дистанция атаки")]
    [SerializeField, Min(0f)] private float attackRange = 4f;
    [SerializeField, Min(0f)] private float timeBetweenAttacks = 0.5f;

    [Header("Топот")]
    [SerializeField, Min(0f)] private float stompRadius = 2.5f;
    [SerializeField, Min(0f)] private float stompDamage = 20f;
    [SerializeField, Min(0f)] private float stompWindup = 0.7f;
    [SerializeField] private LayerMask stompTargetLayer;
    [SerializeField] private GameObject stompRadiusVisual;

    [Header("Очередь снарядов")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField, Min(0f)] private float projectileSpeed = 8f;
    [SerializeField, Min(0f)] private float projectileDamage = 10f;

    [Tooltip("Сколько пуль разлетается по кругу за один залп.")]
    [SerializeField, Min(1)] private int radialCount = 12;

    [Tooltip("Задержка между соседними пулями залпа. 0 — все одновременно.")]
    [SerializeField, Min(0f)] private float timeBetweenShots = 0.05f;

    private float attackTimer;
    private bool isAttacking;
    private bool nextAttackIsStomp = true;

    protected override void Awake()
    {
        base.Awake();

        if (stompRadiusVisual != null)
        {
            stompRadiusVisual.SetActive(false);
            stompRadiusVisual.transform.localScale = Vector3.one * stompRadius;
        }

        attackTimer = timeBetweenAttacks;
    }

    // ⬇️ КЛЮЧЕВОЕ: пока игрок в радиусе атаки — стоим и не преследуем.
    // Вне радиуса — ведём себя как обычный преследователь.
    protected override void OnPlayerVisible()
    {
        float distance = Vector2.Distance(rb.position, PlayerPosition);

        if (distance <= attackRange)
        {
            StopMoving();
            return;
        }

        // Игрок далеко — пусть базовый класс преследует.
        base.OnPlayerVisible();
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        if (isAttacking || !HasPlayer)
            return;

        float distance = Vector2.Distance(rb.position, PlayerPosition);

        // ⬇️ КЛЮЧЕВОЕ: пока игрок в радиусе атаки — жёстко стоим.
        // Это перекрывает любое движение, которое мог задать базовый FixedUpdate.
        if (distance <= attackRange)
            StopMoving();

        if (distance > attackRange)
            return;

        StopMoving();

        attackTimer -= Time.fixedDeltaTime;

        if (attackTimer > 0f)
            return;

        attackTimer = timeBetweenAttacks;

        if (nextAttackIsStomp)
            StartCoroutine(GroundStomp());
        else
            StartCoroutine(FireBurst());

        nextAttackIsStomp = !nextAttackIsStomp;
    }

    private IEnumerator GroundStomp()
    {
        isAttacking = true;
        StopMoving();

        if (stompRadiusVisual != null)
            stompRadiusVisual.SetActive(true);

        yield return new WaitForSeconds(stompWindup);


        Collider2D[] targets = Physics2D.OverlapCircleAll(
            rb.position,
            stompRadius,
            stompTargetLayer
        );

        Debug.Log(targets);

        //HashSet<PlayerStats> damagedPlayers = new HashSet<PlayerStats>();

        Debug.Log($"targets.Length = {targets.Length}, radius = {stompRadius}, layerMask = {stompTargetLayer.value}");

        foreach (Collider2D target in targets)
        {
            Debug.Log("50 ehjyf");

            PlayerStats stats = target.GetComponent<PlayerStats>();

            Debug.Log("50 ehjyf");

            if (stats != null)
                stats.TakeDamage(Mathf.RoundToInt(stompDamage));
        }

        if (stompRadiusVisual != null)
            stompRadiusVisual.SetActive(false);

        isAttacking = false;
    }

    private IEnumerator FireBurst()
    {
        isAttacking = true;
        StopMoving();

        if (projectilePrefab == null || radialCount <= 0)
        {
            isAttacking = false;
            yield break;
        }

        Transform origin = firePoint != null ? firePoint : transform;

        // Равномерно распределяем пули по кругу.
        float step = 360f / radialCount;

        for (int i = 0; i < radialCount; i++)
        {
            float angle = i * step;
            Vector2 direction = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad)
            );

            SpawnProjectile(origin, direction);

            if (timeBetweenShots > 0f && i < radialCount - 1)
                yield return new WaitForSeconds(timeBetweenShots);
        }

        isAttacking = false;
    }

    private void SpawnProjectile(Transform origin, Vector2 direction)
    {
        GameObject projectile = Instantiate(
            projectilePrefab,
            origin.position,
            Quaternion.FromToRotation(Vector3.right, direction)
        );

        BossProjectile2D projectileScript =
            projectile.GetComponent<BossProjectile2D>();

        if (projectileScript != null)
        {
            projectileScript.Initialize(
                direction,
                projectileSpeed,
                projectileDamage
            );
        }
        else
        {
            Debug.LogError(
                "На префабе снаряда отсутствует BossProjectile2D.",
                projectile
            );
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.1f, 0.25f);
        Gizmos.DrawSphere(transform.position, stompRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stompRadius);

        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}