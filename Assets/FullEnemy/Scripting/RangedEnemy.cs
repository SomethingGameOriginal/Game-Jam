using UnityEngine;

public class RangedEnemy : EnemyChase2D
{
    [Header("Дистанция")]
    [SerializeField] private float desiredDistance = 5f;
    [SerializeField] private float distanceTolerance = 0.5f;

    [Header("Стрельба")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRate = 1f;
    [SerializeField] private float projectileSpeed = 8f;

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

        if (distance > desiredDistance + distanceTolerance)
            MoveInDirection(GetMoveDirection(PlayerPosition));
        else if (distance < desiredDistance - distanceTolerance)
            MoveInDirection(GetMoveDirection(PlayerPosition));
        else
            StopMoving();

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