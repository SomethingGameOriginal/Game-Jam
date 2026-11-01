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

    [Tooltip("Сколько отскоков у клонов. -1 = столько же, сколько у оригинала.")]
    public int cloneBounces = -1;

    [Header("Рикошет")]
    public int maxBounces = 3;
    [Range(0f, 1f)] public float bounceSpeedMultiplier = 1f;
    public float minBounceSpeed = 1f;

    [Tooltip("На сколько сдвинуть пулю от стены после отскока (юниты).")]
    public float bounceSeparation = 0.1f;

    [Header("Игрок")]
    public bool damagePlayer = true;

    private bool isClone;
    private int bouncesLeft;
    private bool leftShooterCollider;

    private float extraDamage = 0f;

    Rigidbody2D rb;
    Vector2 direction;
    float currentSpeed;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currentSpeed = speed;
        bouncesLeft = maxBounces;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var bonus = player.GetComponent<BulletDamageBonus>();
            if (bonus != null) extraDamage = bonus.bonusDamage;
        }
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

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.GetComponentInParent<PlayerStats>() != null)
            leftShooterCollider = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 1) Игрок.
        PlayerStats playerStats = collision.GetComponentInParent<PlayerStats>();
        if (playerStats != null)
        {
            if (!leftShooterCollider)
                return;

            if (damagePlayer)
                playerStats.TakeDamage(damage);

            Destroy(gameObject);
            return;
        }

        // 2) Враг.
        EnemyChase2D enemy = collision.GetComponentInParent<EnemyChase2D>();
        if (enemy != null)
        {
            int totalDamage = damage + Mathf.RoundToInt(extraDamage);
            enemy.TakeDamage(totalDamage);

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

        // Выводим пулю из коллайдера, чтобы OnTriggerEnter2D сработал снова.
        Vector2 newPos = rb.position + normal * bounceSeparation;

        // Дополнительно проверяем, что новая позиция не внутри стены.
        // Если внутри — сдвигаем сильнее.
        if (wall.OverlapPoint(newPos))
        {
            newPos = rb.position + normal * (bounceSeparation * 4f);
        }

        rb.position = newPos;
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
        float spawnRadius = cloneSpawnOffset;

        for (int i = 0; i < cloneCount; i++)
        {
            float angle = 360f * i / cloneCount;
            Vector2 outward = Rotate(Vector2.right, angle).normalized;

            Vector3 spawnPos = enemyCenter + outward * spawnRadius
                             + outward * cloneForwardOffset;

            Vector2 cloneDir = outward;

            GameObject clone = Instantiate(gameObject, spawnPos, Quaternion.identity);

            Pistol_Bullet cloneBullet = clone.GetComponent<Pistol_Bullet>();
            if (cloneBullet != null)
            {
                cloneBullet.MarkAsClone();
                cloneBullet.speed = speed * cloneSpeedMultiplier;
                cloneBullet.currentSpeed = cloneBullet.speed;
                cloneBullet.Shoot(cloneDir);

                // Клоны рикошетят столько же, сколько оригинал (или cloneBounces).
                int bounces = cloneBounces >= 0 ? cloneBounces : maxBounces;
                cloneBullet.SetBounces(bounces);

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