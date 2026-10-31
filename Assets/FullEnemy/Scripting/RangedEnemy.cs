using UnityEngine;

public class RangedEnemy : EnemyChase2D
{
    [Header("Дистанция")]
    [SerializeField, Min(0f)] private float desiredDistance = 5f;
    [SerializeField, Min(0f)] private float distanceTolerance = 0.5f;
    [SerializeField, Min(0f)] private float retreatStep = 2f;

    [Header("Стрельба")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField, Min(0.01f)] private float fireRate = 1f;
    [SerializeField, Min(0f)] private float projectileSpeed = 8f;
    [SerializeField, Min(0)] private int projectileDamage = 1;
    [SerializeField] private LayerMask obstacleLayers;

    private float fireTimer;

    protected override void FixedUpdate()
    {
        UpdateStuckTimer();

        if (!FindPlayer())
        {
            // При потере игрока ищем его у последней известной позиции.
            UpdateSearchMovement();
            return;
        }

        Vector2 toPlayer = PlayerPosition - rb.position;
        float distance = toPlayer.magnitude;

        if (distance > desiredDistance + distanceTolerance)
        {
            MoveInDirection(GetMoveDirection(PlayerPosition));
        }
        else if (distance < desiredDistance - distanceTolerance)
        {
            MoveInDirection(GetRetreatDirection(-toPlayer));
        }
        else
        {
            StopMoving();
        }

        fireTimer -= Time.fixedDeltaTime;

        if (fireTimer <= 0f)
        {
            if (HasClearShot(out Vector2 shotDirection))
                Shoot(shotDirection);

            fireTimer = 1f / Mathf.Max(0.01f, fireRate);
        }
    }

    private bool HasClearShot(out Vector2 direction)
    {
        Vector2 origin = firePoint != null
            ? (Vector2)firePoint.position
            : rb.position;

        Vector2 offset = PlayerPosition - origin;

        if (offset.sqrMagnitude < 0.001f)
        {
            direction = Vector2.zero;
            return false;
        }

        direction = offset.normalized;

        RaycastHit2D hit = Physics2D.Raycast(
            origin,
            direction,
            offset.magnitude,
            obstacleLayers
        );

        return hit.collider == null;
    }

    private void Shoot(Vector2 direction)
    {
        if (projectilePrefab == null)
            return;

        Vector3 spawnPosition = firePoint != null
            ? firePoint.position
            : transform.position;

        GameObject projectile = Instantiate(
            projectilePrefab,
            spawnPosition,
            Quaternion.identity
        );

        EnemyProjectile bullet = projectile.GetComponent<EnemyProjectile>();

        if (bullet != null)
        {
            bullet.Launch(direction, projectileSpeed, projectileDamage);
        }
        else
        {
            Debug.LogError(
                "На Projectile Prefab отсутствует EnemyProjectile.",
                projectile
            );
        }
    }
}