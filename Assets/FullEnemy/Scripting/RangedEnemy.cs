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
    [SerializeField] private int projectileDamage = 1;

    [SerializeField] private LayerMask obstacleLayers;

    private float fireTimer;

    private bool HasClearShot(out Vector2 direction)
    {
        Vector2 origin = firePoint != null
            ? (Vector2)firePoint.position
            : rb.position;

        Vector2 target = PlayerPosition;
        Vector2 offset = target - origin;

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

    protected override void FixedUpdate()
    {
        base.FixedUpdate(); // обновляет таймер застревания в EnemyChase2D

        // Убедись, что EnemyChase2D.Awake() уже нашёл Rigidbody2D.
        if (!FindPlayer())
        {
            StopMoving();
            return;
        }

        Vector2 toPlayer = PlayerPosition - rb.position;
        float distance = toPlayer.magnitude;

        if (distance > desiredDistance + distanceTolerance)
        {
            // Сначала учитываем дистанцию между врагами, затем обход препятствий.
            Vector2 desiredMove = GetMoveDirection(PlayerPosition);
            MoveInDirection(FindDirection(desiredMove));
        }
        else if (distance < desiredDistance - distanceTolerance)
        {
            // Отступаем от игрока, также проверяя препятствия.
            MoveInDirection(FindDirection(-toPlayer.normalized));
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
            bullet.Launch(direction, projectileSpeed, projectileDamage);
        else
            Debug.LogError("На Projectile Prefab отсутствует EnemyProjectile", projectile);
    }
}