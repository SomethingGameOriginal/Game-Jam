using UnityEngine;

public class SniperEnemy : EnemyChase2D
{
    [Header("Дистанция снайпера")]
    [SerializeField] private float preferredDistance = 9f;
    [SerializeField] private float minDistance = 7f;
    [SerializeField] private float maxDistance = 12f;

    [Header("Стрельба")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRate = 0.5f;
    [SerializeField] private float projectileSpeed = 12f;

    [SerializeField] private LayerMask obstacleLayers;

    private float fireTimer;

    private bool HasClearShot()
    {
        Vector2 origin = firePoint != null
            ? firePoint.position
            : transform.position;

        Vector2 target = PlayerPosition;
        Vector2 direction = target - origin;
        float distance = direction.magnitude;

        RaycastHit2D hit = Physics2D.Raycast(
            origin,
            direction.normalized,
            distance,
            obstacleLayers
        );

        return hit.collider == null;
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        if (!HasPlayer)
        {
            StopMoving();
            return;
        }

        Vector2 toPlayer = PlayerPosition - rb.position;
        float distance = toPlayer.magnitude;

        if (distance < minDistance)
        {
            // Отступает от игрока.
            Vector2 retreatTarget = rb.position - toPlayer.normalized * 3f;
            MoveInDirection(GetDirectionTo(retreatTarget));
        }
        else if (distance > maxDistance)
        {
            // Слишком далеко — подходит.
            MoveInDirection(GetDirectionTo(PlayerPosition));
        }
        else if (distance < preferredDistance - 0.5f)
        {
            // Понемногу отходит, чтобы сохранить предпочитаемую дистанцию.
            Vector2 retreatTarget = rb.position - toPlayer.normalized * 2f;
            MoveInDirection(GetDirectionTo(retreatTarget));
        }
        else
        {
            StopMoving();
        }

        fireTimer -= Time.fixedDeltaTime;

        if (fireTimer <= 0f)
        {
            if (HasClearShot())
                Shoot(GetDirectionTo(PlayerPosition));
            fireTimer = 1f / Mathf.Max(0.01f, fireRate);
        }
    }

    private void Shoot(Vector2 direction)
    {
        if (projectilePrefab == null || firePoint == null) return;

        GameObject projectile = Instantiate(
            projectilePrefab,
            firePoint.position,
            Quaternion.identity
        );

        EnemyProjectile bullet = projectile.GetComponent<EnemyProjectile>();

        if (bullet != null)
            bullet.Launch(direction, projectileSpeed, 1);
        else
            Debug.LogError("На Projectile Prefab отсутствует EnemyProjectile", projectile);
    }
}