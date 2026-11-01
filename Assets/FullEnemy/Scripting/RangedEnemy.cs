using UnityEngine;

public class RangedEnemy : EnemyChase2D
{
    [Header("Дистанция")]
    [SerializeField] private float desiredDistance = 5f;
    [SerializeField] private float distanceTolerance = 0.5f;
    [SerializeField, Min(0f)] private float minApproachDistance = 1.5f;

    [Header("Стрельба")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRate = 1f;
    [SerializeField] private float projectileSpeed = 8f;
    [SerializeField] private int projectileDamage = 1;

    [Header("Препятствия для выстрела")]
    [Tooltip("Слои стен/препятствий. Должно быть НЕ пусто, иначе HasClearShot всегда false.")]
    [SerializeField] private LayerMask shootObstacleLayers;

    private float fireTimer;

    protected override void FixedUpdate()
    {
        bool seeing = FindPlayer();


        if (!seeing)
        {
            StopMoving();
            return;
        }

        UpdateStuckTimer();
        OnPlayerVisible();
    }

    protected override void OnPlayerVisible()
    {
        if (player == null)
        {
            StopMoving();
            return;
        }

        Vector2 playerPos = player.position;
        Vector2 toPlayer = playerPos - rb.position;
        float distance = toPlayer.magnitude;

        bool hasShot = HasClearShot(out Vector2 shotDir);

        // --- Движение ---
        if (distance > desiredDistance + distanceTolerance)
        {
            // Слишком далеко — сближаемся.
            MoveInDirection(GetMoveDirection(playerPos));
        }
        else if (hasShot && distance < desiredDistance - distanceTolerance)
        {
            // Слишком близко, но стрелять можем — отступаем.
            MoveInDirection(GetRetreatDirection(-toPlayer));
        }
        else if (!hasShot && distance > minApproachDistance)
        {
            // Стены мешают — подходим, чтобы найти угол.
            MoveInDirection(GetMoveDirection(playerPos));
        }
        else
        {
            StopMoving();
        }

        // --- Стрельба ---
        if (hasShot)
        {
            fireTimer -= Time.fixedDeltaTime;

            if (fireTimer <= 0f)
            {
                Shoot(shotDir);
                fireTimer = 1f / Mathf.Max(0.01f, fireRate);
            }
        }
        else
        {
            fireTimer = 0f;
        }
    }

    private bool HasClearShot(out Vector2 direction)
    {
        direction = Vector2.zero;

        if (player == null)
            return false;

        Vector2 origin = firePoint != null
            ? (Vector2)firePoint.position
            : rb.position;

        Vector2 offset = (Vector2)player.position - origin;

        if (offset.sqrMagnitude < 0.001f)
            return false;

        direction = offset.normalized;

        // Если слой препятствий не задан — считаем, что стрелять можно
        // (иначе враг вообще никогда не выстрелит при пустом поле).
        LayerMask mask = shootObstacleLayers.value != 0
            ? shootObstacleLayers
            : obstacleLayer;

        if (mask.value == 0)
            return true;

        RaycastHit2D[] hits = Physics2D.RaycastAll(
            origin,
            direction,
            offset.magnitude,
            mask
        );

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null)
                continue;

            Transform hitTransform = hit.collider.transform;

            // Свои коллайдеры игнорируем.
            if (hitTransform == transform || hitTransform.IsChildOf(transform))
                continue;

            // Игрок — не препятствие.
            if (hitTransform == player || hitTransform.IsChildOf(player))
                continue;

            // Попали в стену.
            return false;
        }

        return true;
    }

    private void Shoot(Vector2 direction)
    {
        if (projectilePrefab == null || firePoint == null)
        {
            Debug.LogWarning($"[RangedEnemy] Не назначен {(projectilePrefab == null ? "Projectile Prefab" : "Fire Point")} на {name}", this);
            return;
        }

        GameObject projectile = Instantiate(
            projectilePrefab,
            firePoint.position,
            Quaternion.identity
        );

        EnemyProjectile bullet = projectile.GetComponent<EnemyProjectile>();

        if (bullet != null)
            bullet.Launch(direction, projectileSpeed, projectileDamage);
        else
            Debug.LogError("На Projectile Prefab отсутствует EnemyProjectile", projectile);
    }
}