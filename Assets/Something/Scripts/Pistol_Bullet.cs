using UnityEngine;

public class Pistol_Bullet : MonoBehaviour
{
    [Header("Полёт")]
    public float speed = 10f;

    [Header("Урон")]
    public int damage = 1;
    public string enemyTag = "Enemy";

    [Header("Размножение")]
    public int cloneCount = 3;
    public float cloneSpreadAngle = 60f;
    public float cloneSpeedMultiplier = 1f;
    public float cloneLifetime = 3f;

    [Tooltip("Отступ точки спавна клонов от центра врага (в юнитах).")]
    public float cloneSpawnOffset = 0.8f;

    [Tooltip("Дополнительный отступ каждого клона вдоль его направления полёта.")]
    public float cloneForwardOffset = 0.3f;

    [Header("Рикошет")]
    public int maxBounces = 3;
    [Range(0f, 1f)] public float bounceSpeedMultiplier = 1f;
    public float minBounceSpeed = 1f;

    [Header("Игрок")]
    [Tooltip("Наносить ли урон игроку.")]
    public bool damagePlayer = true;

    private bool isClone;
    private int bouncesLeft;

    Rigidbody2D rb;
    Vector2 direction;
    float currentSpeed;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currentSpeed = speed;
        bouncesLeft = maxBounces;
    }

    public void Shoot(Vector2 shootDirection)
    {
        direction = shootDirection.normalized;
    }

    public void MarkAsClone()
    {
        isClone = true;
    }

    public void SetBounces(int count)
    {
        maxBounces = count;
        bouncesLeft = count;
    }

    void FixedUpdate()
    {
        rb.linearVelocity = direction * currentSpeed;
    }

    private bool leftShooterCollider;

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.GetComponentInParent<PlayerStats>() != null)
            leftShooterCollider = true;
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 1) Игрок — приоритет.
        PlayerStats playerStats = collision.GetComponentInParent<PlayerStats>();
        if (playerStats != null)
        {
            // Пока пуля ещё внутри игрока — игнорируем.
            if (!leftShooterCollider)
                return;

            if (damagePlayer)
                playerStats.TakeDamage(damage);

            Destroy(gameObject);
            return;
        }

        // 2) Враг — по компоненту, а не по тегу.
        EnemyChase2D enemy = collision.GetComponentInParent<EnemyChase2D>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);

            if (!isClone)
                SpawnClones(collision);

            Destroy(gameObject);
            return;
        }

        // 3) Стена — рикошет.
        if (collision.CompareTag("Wall"))
        {
            Bounce(collision);
            return;
        }
    }

    private void Bounce(Collider2D wall)
    {
        if (bouncesLeft <= 0 || currentSpeed < minBounceSpeed)
        {
            Destroy(gameObject);
            return;
        }

        bouncesLeft--;

        Vector2 normal = GetContactNormal(wall);

        if (normal == Vector2.zero)
        {
            Destroy(gameObject);
            return;
        }

        direction = Vector2.Reflect(direction, normal).normalized;
        currentSpeed *= bounceSpeedMultiplier;

        rb.position += normal * 0.02f;
        rb.linearVelocity = direction * currentSpeed;
    }

    private Vector2 GetContactNormal(Collider2D wall)
    {
        Vector2 bulletPos = transform.position;
        Vector2 closest = wall.ClosestPoint(bulletPos);
        Vector2 normal = bulletPos - closest;

        if (normal.sqrMagnitude < 0.0001f)
            normal = -direction;

        return normal.normalized;
    }

    private void SpawnClones(Collider2D enemyCollider)
    {
        if (cloneCount <= 0)
            return;

        Vector2 enemyCenter = enemyCollider.bounds.center;

        // Радиус спавна = отступ от центра врага.
        // Если хочешь гарантированно снаружи коллайдера — бери max(cloneSpawnOffset, bounds.extents.magnitude + 0.1f).
        float spawnRadius = cloneSpawnOffset;

        for (int i = 0; i < cloneCount; i++)
        {
            // Равномерно распределяем углы по кругу.
            float angle = 360f * i / cloneCount;

            // Направление от центра врага наружу — под этим углом.
            Vector2 outward = Rotate(Vector2.right, angle).normalized;

            // Позиция на окружности вокруг врага.
            Vector3 spawnPos = enemyCenter + outward * spawnRadius
                             + outward * cloneForwardOffset;

            // Клон летит наружу (во все стороны).
            Vector2 cloneDir = outward;

            GameObject clone = Instantiate(gameObject, spawnPos, Quaternion.identity);

            Pistol_Bullet cloneBullet = clone.GetComponent<Pistol_Bullet>();
            if (cloneBullet != null)
            {
                cloneBullet.MarkAsClone();
                cloneBullet.speed = speed * cloneSpeedMultiplier;
                cloneBullet.currentSpeed = cloneBullet.speed;
                cloneBullet.Shoot(cloneDir);

                cloneBullet.SetBounces(0);

                if (cloneLifetime > 0f)
                    Destroy(clone, cloneLifetime);
            }
        }
    }

    private static Vector2 Rotate(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(rad);
        float cos = Mathf.Cos(rad);

        return new Vector2(
            v.x * cos - v.y * sin,
            v.x * sin + v.y * cos
        ).normalized;
    }
}