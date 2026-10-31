using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyChase2D : MonoBehaviour
{
    [Header("Общее")]
    [SerializeField] protected Transform player;
    [SerializeField] protected float moveSpeed = 3f;
    [SerializeField] private int maxHealth = 3;

    [Header("Обход препятствий")]
    [SerializeField] protected LayerMask obstacleLayer;
    [SerializeField] protected float obstacleCheckDistance = 1f;
    [SerializeField] protected float obstacleRadius = 0.3f;
    [SerializeField, Range(15f, 90f)] protected float scanAngle = 60f;
    [SerializeField, Range(3, 31)] protected int scanRays = 15;

    [Header("Расширенный поиск")]
    [SerializeField, Range(0f, 1f)] private float sideBias = 0.3f;
    [SerializeField] private float stuckTimeToReverse = 0.6f;

    protected Rigidbody2D rb;
    protected Vector2 lastDirection;

    private Vector2 previousPosition;
    private float stuckTimer;
    private bool preferLeft = true;

    [Header("Обнаружение игрока")]
    [SerializeField] protected LayerMask playerLayer;
    [SerializeField] protected float sightRadius = 8f;

    [Header("Дистанция между врагами")]
    [SerializeField] protected float enemySpacing = 1.2f;
    [SerializeField] protected LayerMask enemyLayer;

    protected bool FindPlayer()
    {
        if (player != null)
            return Vector2.Distance(transform.position, player.position) <= sightRadius;

        Collider2D hit = Physics2D.OverlapCircle(
            transform.position,
            sightRadius,
            playerLayer
        );

        if (hit == null)
            return false;

        PlayerStats stats = hit.GetComponentInParent<PlayerStats>();
        player = stats != null ? stats.transform : hit.transform;

        return true;
    }

    protected Vector2 GetSpacingDirection()
    {
        Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(
            transform.position,
            enemySpacing,
            enemyLayer
        );

        Vector2 direction = Vector2.zero;

        foreach (Collider2D enemy in nearbyEnemies)
        {
            if (enemy == null || enemy.transform == transform)
                continue;

            Vector2 away = (Vector2)transform.position - (Vector2)enemy.transform.position;
            float distance = away.magnitude;

            if (distance > 0.001f)
                direction += away.normalized / distance;
        }

        return direction.sqrMagnitude > 0.001f
            ? direction.normalized
            : Vector2.zero;
    }

    protected void DrawEnemyGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, enemySpacing);
    }

    private void OnDrawGizmosSelected()
    {
        DrawEnemyGizmos();
    }

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        previousPosition = rb.position;
    }

    protected virtual void FixedUpdate()
    {
        UpdateStuckTimer();
    }

    protected Vector2 GetMoveDirection(Vector2 targetPosition)
    {
        Vector2 toTarget = targetPosition - (Vector2)transform.position;

        Collider2D[] nearby = Physics2D.OverlapCircleAll(
            transform.position,
            enemySpacing,
            enemyLayer
        );

        Vector2 separation = Vector2.zero;

        foreach (Collider2D other in nearby)
        {
            if (other == null || other.transform == transform)
                continue;

            Vector2 away = (Vector2)transform.position - (Vector2)other.bounds.center;
            float distance = away.magnitude;

            if (distance > 0.001f)
                separation += away.normalized * (enemySpacing - distance) / enemySpacing;
        }

        // Увеличивай separationWeight, если враги всё ещё слишком близко.
        const float separationWeight = 2f;
        Vector2 desired = toTarget.normalized + separation * separationWeight;

        return desired.sqrMagnitude > 0.001f ? desired.normalized : Vector2.zero;
    }

    /// <summary>Направление к цели с учётом препятствий.</summary>
    protected Vector2 GetDirectionTo(Vector2 target)
    {
        Vector2 offset = target - rb.position;

        if (offset.sqrMagnitude < 0.001f)
            return Vector2.zero;

        return FindDirection(offset.normalized);
    }

    /// <summary>Движение в направлении с учётом скорости.</summary>
    protected void MoveInDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.001f)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        lastDirection = direction.normalized;
        rb.linearVelocity = lastDirection * moveSpeed;
    }

    protected void StopMoving()
    {
        rb.linearVelocity = Vector2.zero;
    }

    protected bool HasPlayer => player != null;
    protected Vector2 PlayerPosition => player != null
        ? (Vector2)player.position
        : rb.position;

    /// <summary>Вызывай из урона, чтобы уничтожать врага при нулевом HP.</summary>
    public virtual void TakeDamage(int damage)
    {
        maxHealth -= damage;

        if (maxHealth <= 0)
            Destroy(gameObject);
    }

    private Vector2 FindDirection(Vector2 desired)
    {
        if (!IsBlocked(desired))
            return desired;

        Vector2 bestDirection = Vector2.zero;
        float bestScore = float.NegativeInfinity;

        for (int i = 0; i < scanRays; i++)
        {
            float t = scanRays == 1 ? 0.5f : (float)i / (scanRays - 1);
            float angle = Mathf.Lerp(-scanAngle, scanAngle, t);

            // При застревании немного предпочитаем другую сторону.
            float side = angle < 0f ? -1f : 1f;
            float scoreSideBias = side == (preferLeft ? -1f : 1f) ? sideBias : 0f;

            Vector2 candidate = Rotate(desired, angle);

            if (IsBlocked(candidate))
                continue;

            float score = Vector2.Dot(candidate, desired) + scoreSideBias;

            if (score > bestScore)
            {
                bestScore = score;
                bestDirection = candidate;
            }
        }

        if (bestDirection.sqrMagnitude > 0.001f)
            return bestDirection;

        // Если веер не нашёл проход, пробуем двигаться вдоль стены.
        Vector2 tangentLeft = new Vector2(-desired.y, desired.x);
        Vector2 tangentRight = -tangentLeft;

        if (!IsBlocked(tangentLeft)) return tangentLeft;
        if (!IsBlocked(tangentRight)) return tangentRight;

        return Vector2.zero;
    }

    private bool IsBlocked(Vector2 direction)
    {
        return Physics2D.CircleCast(
            rb.position,
            obstacleRadius,
            direction,
            obstacleCheckDistance,
            obstacleLayer
        ).collider != null;
    }

    private void UpdateStuckTimer()
    {
        if (rb == null) return;

        float moved = Vector2.Distance(rb.position, previousPosition);

        if (rb.linearVelocity.sqrMagnitude > 0.05f && moved < 0.005f)
        {
            stuckTimer += Time.fixedDeltaTime;

            if (stuckTimer >= stuckTimeToReverse)
            {
                preferLeft = !preferLeft;
                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = 0f;
        }

        previousPosition = rb.position;
    }

    protected static Vector2 Rotate(Vector2 vector, float degrees)
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