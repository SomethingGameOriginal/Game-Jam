using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyChase2D : MonoBehaviour
{
    [Header("Общее")]
    [SerializeField] protected Transform player;
    [SerializeField] protected float moveSpeed = 3f;
    [SerializeField] private int maxHealth = 3;

    [Header("Обнаружение игрока")]
    [SerializeField] protected LayerMask playerLayer;
    [SerializeField, Min(0f)] protected float sightRadius = 8f;

    [Header("Поиск потерянного игрока")]
    [SerializeField, Min(0f)] protected float searchDuration = 3f;
    [SerializeField, Min(0f)] protected float searchArrivalDistance = 0.2f;

    [Header("Обход препятствий")]
    [SerializeField] protected LayerMask obstacleLayer;
    [SerializeField, Min(0.05f)] protected float obstacleCheckDistance = 1.2f;
    [SerializeField, Min(0.05f)] protected float obstacleRadius = 0.25f;
    [SerializeField, Min(0.1f)] protected float wallFollowDuration = 1.2f;
    [SerializeField, Min(0.05f)] protected float wallProbeDistance = 0.7f;

    [Header("Дистанция между врагами")]
    [SerializeField, Min(0f)] protected float enemySpacing = 1.2f;
    [SerializeField] protected LayerMask enemyLayer;
    [SerializeField, Min(0f)] protected float separationWeight = 2f;

    [Header("Отладка")]
    [SerializeField] private bool showGizmos = true;

    protected Rigidbody2D rb;
    protected Vector2 lastDirection;
    protected Vector2 LastKnownPlayerPosition { get; private set; }

    protected bool HasPlayer => player != null;
    protected bool IsSearchingForPlayer => searchTimer > 0f;

    protected Vector2 PlayerPosition =>
        player != null ? (Vector2)player.position : LastKnownPlayerPosition;

    private bool followingWall;
    private float wallFollowTimer;
    private float wallSide;
    private bool preferLeft = true;

    private Vector2 previousPosition;
    private float stuckTimer;
    private float searchTimer;

    private readonly List<Vector2> debugBlocked = new();
    private readonly List<Vector2> debugClear = new();
    private Vector2 debugDesired;
    private Vector2 debugChosen;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        previousPosition = rb.position;
        LastKnownPlayerPosition = rb.position;

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
                player = playerObject.transform;
        }

        if (player != null)
            LastKnownPlayerPosition = player.position;
    }

    protected virtual void FixedUpdate()
    {
        UpdateStuckTimer();

        if (FindPlayer())
        {
            MoveInDirection(GetMoveDirection(PlayerPosition));
            return;
        }

        UpdateSearchMovement();
    }

    /// <summary>
    /// Проверяет, обнаружен ли игрок в пределах sightRadius.
    /// При потере игрока запоминает его последнюю позицию.
    /// </summary>
    protected bool FindPlayer()
    {
        if (player != null)
        {
            LastKnownPlayerPosition = player.position;

            if (Vector2.Distance(rb.position, LastKnownPlayerPosition) <= sightRadius)
                return true;

            player = null;
            searchTimer = searchDuration;
        }

        if (playerLayer.value == 0)
            return false;

        Collider2D hit = Physics2D.OverlapCircle(
            rb.position,
            sightRadius,
            playerLayer
        );

        if (hit == null)
            return false;

        PlayerStats stats = hit.GetComponentInParent<PlayerStats>();
        player = stats != null ? stats.transform : hit.transform;

        if (player == null)
            return false;

        LastKnownPlayerPosition = player.position;
        searchTimer = 0f;
        return true;
    }

    /// <summary>
    /// Идёт к последней известной позиции игрока после его потери.
    /// По прибытии некоторое время ищет игрока, затем прекращает движение.
    /// </summary>
    protected void UpdateSearchMovement()
    {
        if (searchTimer <= 0f)
        {
            StopMoving();
            return;
        }

        Vector2 toLastPosition = LastKnownPlayerPosition - rb.position;

        if (toLastPosition.magnitude > searchArrivalDistance)
        {
            MoveInDirection(GetMoveDirection(LastKnownPlayerPosition));
            return;
        }

        StopMoving();
        searchTimer -= Time.fixedDeltaTime;
    }

    /// <summary>
    /// Направление к цели с учётом дистанции до союзников и препятствий.
    /// </summary>
    protected Vector2 GetMoveDirection(Vector2 targetPosition)
    {
        Vector2 toTarget = targetPosition - rb.position;

        if (toTarget.sqrMagnitude < 0.001f)
            return Vector2.zero;

        Vector2 desired = toTarget.normalized;
        Vector2 separation = GetSeparationDirection();

        if (separation.sqrMagnitude > 0.001f)
            desired = (desired + separation * separationWeight).normalized;

        return FindDirection(desired);
    }

    /// <summary>
    /// Направление для отступления от цели с учётом союзников и препятствий.
    /// </summary>
    protected Vector2 GetRetreatDirection(Vector2 awayFromTarget)
    {
        if (awayFromTarget.sqrMagnitude < 0.001f)
            return Vector2.zero;

        Vector2 desired = awayFromTarget.normalized;
        Vector2 separation = GetSeparationDirection();

        if (separation.sqrMagnitude > 0.001f)
            desired = (desired + separation * separationWeight).normalized;

        return FindDirection(desired);
    }

    protected Vector2 GetSeparationDirection()
    {
        if (rb == null || enemySpacing <= 0f || enemyLayer.value == 0)
            return Vector2.zero;

        Collider2D[] nearby = Physics2D.OverlapCircleAll(
            rb.position,
            enemySpacing,
            enemyLayer
        );

        Vector2 separation = Vector2.zero;

        foreach (Collider2D other in nearby)
        {
            if (other == null || other.transform == transform)
                continue;

            Vector2 away = rb.position - (Vector2)other.bounds.center;
            float distance = away.magnitude;

            if (distance > 0.001f && distance < enemySpacing)
            {
                separation += away.normalized *
                              ((enemySpacing - distance) / enemySpacing);
            }
        }

        return separation.sqrMagnitude > 0.001f
            ? separation.normalized
            : Vector2.zero;
    }

    /// <summary>
    /// Обход препятствий без учёта дистанции до других врагов.
    /// Оставлен для совместимости с другими дочерними классами.
    /// </summary>
    protected Vector2 GetDirectionTo(Vector2 target)
    {
        Vector2 offset = target - rb.position;

        return offset.sqrMagnitude > 0.001f
            ? FindDirection(offset.normalized)
            : Vector2.zero;
    }

    protected void MoveInDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.001f)
        {
            StopMoving();
            return;
        }

        lastDirection = direction.normalized;
        rb.linearVelocity = lastDirection * moveSpeed;
    }

    protected void StopMoving()
    {
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    /// <summary>Выбирает направление движения с обходом препятствий.</summary>
    protected Vector2 FindDirection(Vector2 desired)
    {
        desired = desired.normalized;

        if (desired == Vector2.zero)
            return Vector2.zero;

        debugDesired = desired;
        debugChosen = Vector2.zero;
        debugBlocked.Clear();
        debugClear.Clear();

        bool directBlocked = IsBlocked(desired, obstacleCheckDistance);

        if (followingWall)
        {
            wallFollowTimer -= Time.fixedDeltaTime;

            if (!directBlocked && wallFollowTimer <= 0f)
                followingWall = false;
        }

        if (!followingWall && !directBlocked)
        {
            debugChosen = desired;
            return desired;
        }

        if (!followingWall)
        {
            followingWall = true;
            wallFollowTimer = wallFollowDuration;
            wallSide = preferLeft ? 1f : -1f;
            preferLeft = !preferLeft;
        }

        Vector2 tangent = new Vector2(-desired.y, desired.x) * wallSide;

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
            if (IsBlocked(candidate, wallProbeDistance))
            {
                debugBlocked.Add(candidate);
                continue;
            }

            debugClear.Add(candidate);

            float score = Vector2.Dot(candidate, desired) * 0.5f
                        + Vector2.Dot(candidate, tangent) * 0.8f;

            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

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
        if (direction.sqrMagnitude < 0.001f || obstacleLayer.value == 0)
            return false;

        RaycastHit2D hit = Physics2D.CircleCast(
            rb.position,
            obstacleRadius,
            direction.normalized,
            distance,
            obstacleLayer
        );

        return hit.collider != null;
    }

    protected virtual void UpdateStuckTimer()
    {
        if (rb == null)
            return;

        float moved = Vector2.Distance(rb.position, previousPosition);

        if (rb.linearVelocity.sqrMagnitude > 0.05f && moved < 0.005f)
        {
            stuckTimer += Time.fixedDeltaTime;

            if (stuckTimer >= 0.6f)
            {
                preferLeft = !preferLeft;
                followingWall = false;
                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = 0f;
        }

        previousPosition = rb.position;
    }

    public virtual void TakeDamage(int damage)
    {
        maxHealth -= damage;

        if (maxHealth <= 0)
            Destroy(gameObject);
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

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, enemySpacing);

        if (Application.isPlaying)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(LastKnownPlayerPosition, 0.15f);
            Gizmos.DrawLine(transform.position, LastKnownPlayerPosition);
        }

        Vector3 origin = Application.isPlaying && rb != null
            ? (Vector3)rb.position
            : transform.position;

        Gizmos.color = Color.red;
        foreach (Vector2 direction in debugBlocked)
            Gizmos.DrawLine(
                origin,
                origin + (Vector3)(direction * obstacleCheckDistance)
            );

        Gizmos.color = Color.green;
        foreach (Vector2 direction in debugClear)
            Gizmos.DrawLine(
                origin,
                origin + (Vector3)(direction * obstacleCheckDistance)
            );

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(
            origin,
            origin + (Vector3)(debugDesired * obstacleCheckDistance)
        );

        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(
            origin,
            origin + (Vector3)(debugChosen * obstacleCheckDistance)
        );
    }
}