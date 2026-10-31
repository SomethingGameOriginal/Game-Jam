using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyChase2D : MonoBehaviour
{
    [Header("Общее")]
    [SerializeField] protected Transform player;
    [SerializeField] protected float moveSpeed = 3f;
    [SerializeField] private int maxHealth = 3;

    [Header("Обход препятствий")]
    [SerializeField] protected LayerMask obstacleLayer;
    [SerializeField, Min(0.05f)] protected float obstacleCheckDistance = 1.2f;
    [SerializeField, Min(0.05f)] protected float obstacleRadius = 0.25f;
    [SerializeField, Range(15f, 120f)] protected float scanAngle = 100f;
    [SerializeField, Range(3, 31)] protected int scanRays = 15;

    [Header("Настройка обхода")]
    [SerializeField, Min(0f)] private float wallFollowTime = 0.5f;
    [SerializeField, Min(0f)] private float progressWeight = 1f;
    [SerializeField] private bool drawObstacleLines = true;

    private float wallFollowTimer;
    private float wallSide;
    private Vector2 debugDesired;
    private Vector2 debugChosen;
    private readonly System.Collections.Generic.List<Vector2> debugBlocked = new();
    private readonly System.Collections.Generic.List<Vector2> debugClear = new();
    [Header("Расширенный поиск")]
    [SerializeField, Range(0f, 1f)] private float sideBias = 0.3f;
    [SerializeField] private float stuckTimeToReverse = 0.6f;

    [Header("Отладка обхода")]
    [SerializeField] private bool showObstacleGizmos = true;
    [SerializeField] private bool showAllScanLines = true;

    private Vector2 debugDesiredDirection;
    private Vector2 debugChosenDirection;
    private readonly System.Collections.Generic.List<Vector2> debugBlockedDirections = new();
    private readonly System.Collections.Generic.List<Vector2> debugClearDirections = new();

    // Добавь рядом с полями обхода
    private bool followingWall;

    [SerializeField, Min(0.1f)] private float wallFollowDuration = 1.2f;
    [SerializeField, Min(0.1f)] private float wallProbeDistance = 0.7f;
    [SerializeField, Range(0f, 1f)] private float wallClearance = 0.35f;

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

        if (!drawObstacleLines)
            return;

        Vector3 origin = Application.isPlaying && rb != null
            ? (Vector3)rb.position
            : transform.position;

        Gizmos.color = Color.red;
        foreach (Vector2 dir in debugBlocked)
            Gizmos.DrawLine(origin, origin + (Vector3)(dir * obstacleCheckDistance));

        Gizmos.color = Color.green;
        foreach (Vector2 dir in debugClear)
            Gizmos.DrawLine(origin, origin + (Vector3)(dir * obstacleCheckDistance));

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin, origin + (Vector3)(debugDesired * obstacleCheckDistance));

        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(origin, origin + (Vector3)(debugChosen * obstacleCheckDistance));
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

        Vector2 result = FindDirection(offset);
        Debug.Log($"Выбранное направление: {result}");
        return result;
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

    public Vector2 FindDirection(Vector2 desired)
    {
        desired = desired.normalized;

        if (desired == Vector2.zero)
            return Vector2.zero;

        debugDesired = desired;
        debugChosen = Vector2.zero;
        debugBlocked.Clear();
        debugClear.Clear();

        bool directBlocked = IsBlocked(desired, obstacleCheckDistance);

        // Если обходили стену и путь к цели снова свободен — заканчиваем обход.
        if (followingWall)
        {
            wallFollowTimer -= Time.fixedDeltaTime;

            if (!directBlocked && wallFollowTimer <= 0f)
                followingWall = false;
        }

        // Прямой путь свободен — идём к цели.
        if (!followingWall && !directBlocked)
        {
            debugChosen = desired;
            return desired;
        }

        // Начинаем обход. Сторона задаётся при первом столкновении.
        if (!followingWall)
        {
            followingWall = true;
            wallFollowTimer = wallFollowDuration;
            wallSide = preferLeft ? 1f : -1f;
            preferLeft = !preferLeft;
        }

        Vector2 tangent = new Vector2(-desired.y, desired.x) * wallSide;

        // Продвигаемся вдоль стены и слегка от неё.
        Vector2[] candidates =
        {
        (tangent + desired * 0.25f).normalized,
        tangent,
        (tangent - desired * 0.25f).normalized,
        (tangent + desired * 0.6f).normalized,
        (tangent - desired * 0.6f).normalized
    };

        Vector2 best = Vector2.zero;
        float bestScore = float.NegativeInfinity;

        foreach (Vector2 candidate in candidates)
        {
            bool blocked = IsBlocked(candidate, wallProbeDistance);

            if (blocked)
            {
                debugBlocked.Add(candidate);
                continue;
            }

            debugClear.Add(candidate);

            // Сильно предпочитаем движение к цели, но сохраняем движение вдоль стены.
            float score = Vector2.Dot(candidate, desired) * 0.5f
                        + Vector2.Dot(candidate, tangent) * 0.8f;

            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        // Если выбранная сторона упёрлась, пробуем противоположную.
        if (best == Vector2.zero)
        {
            wallSide *= -1f;
            tangent = -tangent;

            Vector2[] oppositeCandidates =
            {
            (tangent + desired * 0.25f).normalized,
            tangent,
            (tangent - desired * 0.25f).normalized
        };

            foreach (Vector2 candidate in oppositeCandidates)
            {
                if (!IsBlocked(candidate, wallProbeDistance))
                {
                    best = candidate;
                    break;
                }
            }
        }

        debugChosen = best;
        return best;
    }

    private bool IsBlocked(Vector2 direction, float distance)
    {
        if (direction.sqrMagnitude < 0.001f)
            return true;

        RaycastHit2D hit = Physics2D.CircleCast(
            rb.position,
            obstacleRadius,
            direction.normalized,
            distance,
            obstacleLayer
        );

        return hit.collider != null;
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