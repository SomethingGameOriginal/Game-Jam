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

    [Tooltip("Число снарядов в каждом из трёх направлений.")]
    [SerializeField, Min(1)] private int countOfBullet = 5;

    [SerializeField, Range(0f, 45f)] private float sideShotAngle = 12f;
    [SerializeField, Min(0f)] private float timeBetweenShots = 0.12f;

    private float attackTimer;
    private bool isAttacking;
    private bool nextAttackIsStomp = true;

    protected override void Awake()
    {
        base.Awake();

        if (stompRadiusVisual != null)
        {
            stompRadiusVisual.SetActive(false);
            stompRadiusVisual.transform.localScale =
                Vector3.one * stompRadius * 2f;
        }

        attackTimer = timeBetweenAttacks;
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        if (isAttacking || !HasPlayer)
            return;

        float distance = Vector2.Distance(rb.position, PlayerPosition);

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

        // Не наносить урон несколько раз игроку с несколькими коллайдерами.
        HashSet<PlayerStats> damagedPlayers = new HashSet<PlayerStats>();

        foreach (Collider2D target in targets)
        {
            if (target == null ||
                target.transform == transform ||
                target.transform.IsChildOf(transform))
            {
                continue;
            }

            PlayerStats stats = target.GetComponentInParent<PlayerStats>();

            if (stats != null && damagedPlayers.Add(stats))
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

        if (player == null || projectilePrefab == null || countOfBullet <= 0)
        {
            isAttacking = false;
            yield break;
        }

        Transform origin = firePoint != null ? firePoint : transform;

        Vector2 toPlayer =
            (Vector2)player.position - (Vector2)origin.position;

        if (toPlayer.sqrMagnitude < 0.001f)
        {
            isAttacking = false;
            yield break;
        }

        Vector2 centerDirection = toPlayer.normalized;

        // Последовательно выпускаем countOfBullet снарядов
        // в каждом направлении: центральном и двух боковых.
        float[] angles = { 0f, -sideShotAngle, sideShotAngle };

        foreach (float angle in angles)
        {
            Vector2 direction = Rotate(centerDirection, angle);

            for (int i = 0; i < countOfBullet; i++)
            {
                if (player == null)
                    break;

                SpawnProjectile(origin, direction);

                if (i < countOfBullet - 1)
                    yield return new WaitForSeconds(timeBetweenShots);
            }
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