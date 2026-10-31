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

    [SerializeField] private LayerMask obstacleLayers;

    private float fireTimer;

    /// <summary>
    /// Есть ли чистая линия до игрока (без стен между firePoint и игроком).
    /// Враги и собственные коллайдеры выстрел НЕ блокируют.
    /// </summary>
    private bool HasClearShot(out Vector2 direction)
    {
        Vector2 origin = firePoint != null
            ? (Vector2)firePoint.position
            : rb.position;

        Vector2 offset = (Vector2)player.position - origin;

        if (offset.sqrMagnitude < 0.001f)
        {
            direction = Vector2.zero;
            return false;
        }

        direction = offset.normalized;

        if (obstacleLayer.value == 0)
            return false;

        RaycastHit2D[] hits = Physics2D.RaycastAll(
            origin,
            direction,
            offset.magnitude,
            obstacleLayer
        );

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null)
                continue;

            Transform hitTransform = hit.collider.transform;

            // Не учитываем коллайдеры самого врага.
            if (hitTransform == transform || hitTransform.IsChildOf(transform))
                continue;

            // Попали в стену — чистого выстрела нет.
            return false;
        }

        return true;
    }

    protected override void OnPlayerVisible()
    {
        Vector2 playerPos = player.position;
        Vector2 toPlayer = playerPos - rb.position;
        float distance = toPlayer.magnitude;
        bool hasShot = HasClearShot(out Vector2 shotDir);

        if (distance > desiredDistance + distanceTolerance)
        {
            MoveInDirection(GetMoveDirection(playerPos));
        }
        else if (hasShot && distance < desiredDistance - distanceTolerance)
        {
            MoveInDirection(GetRetreatDirection(-toPlayer));
        }
        else if (!hasShot && distance > minApproachDistance)
        {
            MoveInDirection(GetMoveDirection(playerPos));
        }
        else
        {
            StopMoving();
        }

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

    private void Shoot(Vector2 direction)
    {
        if (projectilePrefab == null || firePoint == null)
            return;

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