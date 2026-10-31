using UnityEngine;

public class ChaserEnemy : EnemyChase2D
{
    [Header("Атака")]
    [SerializeField] private int contactDamage = 1;
    [SerializeField] private float attackCooldown = 1f;

    [Header("Преследование")]
    [SerializeField] private float stopDistance = 0.8f;

    [Header("Рывок к прошлой позиции игрока")]
    [SerializeField, Range(0f, 1f)] private float rushChance = 0.2f;
    [SerializeField] private float rushDuration = 0.7f;
    [SerializeField] private float rushCooldown = 3f;
    [SerializeField] private float rushSpeedMultiplier = 2f;

    private Vector2 previousPlayerPosition;
    private Vector2 rushTarget;
    private float rushTimer;
    private float rushCooldownTimer;
    private float attackTimer;
    private float normalSpeed;

    protected override void Awake()
    {
        base.Awake();
        normalSpeed = moveSpeed;
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        attackTimer -= Time.fixedDeltaTime;
        rushCooldownTimer -= Time.fixedDeltaTime;

        if (!FindPlayer())
        {
            moveSpeed = normalSpeed;
            StopMoving();
            return;
        }

        if (rushTimer > 0f)
        {
            rushTimer -= Time.fixedDeltaTime;
            moveSpeed = normalSpeed * rushSpeedMultiplier;

            if (Vector2.Distance(rb.position, rushTarget) <= stopDistance)
            {
                rushTimer = 0f;
            }
            else
            {
                MoveInDirection(GetDirectionTo(rushTarget));
            }

            if (rushTimer <= 0f)
                moveSpeed = normalSpeed;

            return;
        }

        moveSpeed = normalSpeed;

        // Рывок к позиции, где игрок был в предыдущем физическом кадре.
        if (rushCooldownTimer <= 0f &&
            Random.value < rushChance * Time.fixedDeltaTime)
        {
            rushTarget = previousPlayerPosition;
            rushTimer = rushDuration;
            rushCooldownTimer = rushCooldown;
            return;
        }

        previousPlayerPosition = PlayerPosition;

        float distance = Vector2.Distance(rb.position, PlayerPosition);

        if (distance > stopDistance)
            MoveInDirection(GetDirectionTo(PlayerPosition));
        else
            StopMoving();
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryDamagePlayer(collision.collider);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDamagePlayer(other);
    }

    private void TryDamagePlayer(Collider2D other)
    {
        if (attackTimer > 0f)
            return;

        PlayerStats stats = other.GetComponentInParent<PlayerStats>();

        if (stats == null)
            return;

        stats.TakeDamage(contactDamage);
        attackTimer = attackCooldown;
    }
}