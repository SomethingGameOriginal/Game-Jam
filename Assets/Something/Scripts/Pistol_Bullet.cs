using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
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

    public float cloneSpawnOffset = 0.8f;
    public float cloneForwardOffset = 0.3f;
    public int cloneBounces = -1;

    [Header("Рикошет")]
    public int maxBounces = 3;
    [Range(0f, 1f)]
    public float bounceSpeedMultiplier = 1f;
    public float minBounceSpeed = 1f;
    public float bounceSeparation = 0.1f;

    [Header("Игрок")]
    public bool damagePlayer = true;

    private bool isClone;
    private int bouncesLeft;
    private bool leftShooterCollider;
    private bool initialized;

    private float extraDamage;
    private Rigidbody2D rb;
    private Vector2 direction;
    private float currentSpeed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currentSpeed = speed;
        bouncesLeft = maxBounces;
    }

    public void Initialize(Vector2 shootDirection, float bonusDamage)
    {
        extraDamage = bonusDamage;
        Shoot(shootDirection);
        initialized = true;
    }

    public void Shoot(Vector2 shootDirection)
    {
        direction = shootDirection.normalized;
    }

    public void MarkAsClone() => isClone = true;

    public void SetBounces(int count)
    {
        maxBounces = count;
        bouncesLeft = count;
    }

    private void FixedUpdate()
    {
        if (rb != null && direction.sqrMagnitude > 0f)
            rb.linearVelocity = direction * currentSpeed;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.GetComponentInParent<PlayerStats>() != null)
            leftShooterCollider = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
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

        EnemyChase2D enemy = collision.GetComponentInParent<EnemyChase2D>();

        if (enemy != null)
        {
            // float → int
            int totalDamage = Mathf.RoundToInt(GetDamage());
            enemy.TakeDamage(totalDamage);

            if (!isClone)
                SpawnClones(collision);

            Destroy(gameObject);
            return;
        }

        if (collision.CompareTag("Wall"))
            Bounce(collision);
    }

    private float GetDamage()
    {
        float bonus = SkillTreeManager.Instance != null
            ? SkillTreeManager.Instance.GetBonus(SkillBonusType.Damage)
            : 0f;

        return damage + bonus;
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

        Vector2 newPosition = rb.position + normal * bounceSeparation;
        if (wall.OverlapPoint(newPosition))
            newPosition = rb.position + normal * (bounceSeparation * 4f);

        rb.position = newPosition;
        rb.linearVelocity = direction * currentSpeed;
    }

    private Vector2 GetContactNormal(Collider2D wall)
    {
        Vector2 bulletPosition = transform.position;
        Vector2 closestPoint = wall.ClosestPoint(bulletPosition);
        Vector2 normal = bulletPosition - closestPoint;

        if (normal.sqrMagnitude < 0.0001f)
            normal = -direction;

        return normal.normalized;
    }

    private void SpawnClones(Collider2D enemyCollider)
    {
        if (cloneCount <= 0)
            return;

        Vector2 enemyCenter = enemyCollider.bounds.center;

        for (int i = 0; i < cloneCount; i++)
        {
            float angle = 360f * i / cloneCount;
            Vector2 cloneDirection = Rotate(Vector2.right, angle);

            Vector3 spawnPosition =
                enemyCenter +
                cloneDirection * (cloneSpawnOffset + cloneForwardOffset);

            GameObject cloneObject = Instantiate(
                gameObject,
                spawnPosition,
                Quaternion.identity
            );

            Pistol_Bullet cloneBullet =
                cloneObject.GetComponent<Pistol_Bullet>();

            if (cloneBullet == null)
                continue;

            cloneBullet.MarkAsClone();
            cloneBullet.speed = speed * cloneSpeedMultiplier;
            cloneBullet.currentSpeed = cloneBullet.speed;

            cloneBullet.Initialize(cloneDirection, extraDamage);

            int bounces = cloneBounces >= 0 ? cloneBounces : maxBounces;
            cloneBullet.SetBounces(bounces);

            if (cloneLifetime > 0f)
                Destroy(cloneObject, cloneLifetime);
        }
    }

    private static Vector2 Rotate(Vector2 vector, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);

        return new Vector2(
            vector.x * cos - vector.y * sin,
            vector.x * sin + vector.y * cos
        ).normalized;
    }
}