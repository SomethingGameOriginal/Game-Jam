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
    [SerializeField] private int projectileDamage = 1;

    private float fireTimer;

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

            // Игнорируем коллайдеры самого снайпера.
            if (hitTransform == transform || hitTransform.IsChildOf(transform))
                continue;

            // Любой другой коллайдер на obstacleLayer блокирует выстрел.
            return false;
        }

        return true;
    }

    protected override void OnPlayerVisible()
    {
        Vector2 playerPos = player.position;
        Vector2 toPlayer = playerPos - rb.position;
        float distance = toPlayer.magnitude;
        bool hasShot = HasClearShot(out Vector2 shotDirection);

        if (distance < minDistance)
        {
            Vector2 retreatTarget = rb.position - toPlayer.normalized * 3f;
            MoveInDirection(GetDirectionTo(retreatTarget));
        }
        else if (distance > maxDistance)
        {
            MoveInDirection(GetDirectionTo(playerPos));
        }
        else if (distance < preferredDistance - 0.5f)
        {
            Vector2 retreatTarget = rb.position - toPlayer.normalized * 2f;
            MoveInDirection(GetDirectionTo(retreatTarget));
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
                Shoot(shotDirection);
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
            Debug.LogError(
                "На Projectile Prefab отсутствует EnemyProjectile",
                projectile
            );
    }
}