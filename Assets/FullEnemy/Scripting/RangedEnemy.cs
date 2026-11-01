using UnityEngine;

public class RangedEnemy : EnemyChase2D
{
    [Header("Дистанция")]
    [SerializeField] private float desiredDistance = 5f;
    [SerializeField] private float distanceTolerance = 0.5f;
    [SerializeField, Min(0f)] private float minApproachDistance = 1.5f;

    [Header("Стрельба")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRate = 1f;
    [SerializeField] private float projectileSpeed = 8f;
    [SerializeField] private int projectileDamage = 1;

    [Header("Препятствия для выстрела")]
    [Tooltip("Слои стен/препятствий. Должно быть НЕ пусто, иначе HasClearShot всегда false.")]
    [SerializeField] private LayerMask shootObstacleLayers;

    [Header("Анимация")]
    [SerializeField] private Animator animator;
    [SerializeField] private string moveBoolName = "isMoving";
    [SerializeField] private string attackTriggerName = "Attack";

    [Header("Поворот спрайта")]
    [Tooltip("Компонент SpriteRenderer, который нужно переворачивать.")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [Tooltip("Поставь галочку, если спрайт по умолчанию смотрит ВЛЕВО.")]
    [SerializeField] private bool spriteFacesLeftByDefault = false;

    private float fireTimer;

    // Хэши для оптимизации Animator
    private int moveBoolHash;
    private int attackTriggerHash;

    protected override void Awake()
    {
        base.Awake();

        // Инициализация хэшей аниматора
        moveBoolHash = Animator.StringToHash(moveBoolName);
        attackTriggerHash = Animator.StringToHash(attackTriggerName);

        // Автопоиск компонентов, если не назначены
        if (animator == null)
            animator = GetComponent<Animator>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    protected override void FixedUpdate()
    {
        bool seeing = FindPlayer();

        if (!seeing)
        {
            StopMoving();
            UpdateAnimations(false); // Стоим
            return;
        }

        UpdateStuckTimer();
        OnPlayerVisible();
    }

    protected override void OnPlayerVisible()
    {
        if (player == null)
        {
            StopMoving();
            UpdateAnimations(false);
            return;
        }

        Vector2 playerPos = player.position;
        Vector2 toPlayer = playerPos - rb.position;
        float distance = toPlayer.magnitude;

        bool hasShot = HasClearShot(out Vector2 shotDir);
        bool isMoving = false;

        // --- Поворот в сторону игрока ---
        FacePlayer(playerPos.x);

        // --- Движение ---
        if (distance > desiredDistance + distanceTolerance)
        {
            MoveInDirection(GetMoveDirection(playerPos));
            isMoving = true;
        }
        else if (hasShot && distance < desiredDistance - distanceTolerance)
        {
            MoveInDirection(GetRetreatDirection(-toPlayer));
            isMoving = true;
        }
        else if (!hasShot && distance > minApproachDistance)
        {
            MoveInDirection(GetMoveDirection(playerPos));
            isMoving = true;
        }
        else
        {
            StopMoving();
        }

        UpdateAnimations(isMoving);

        // --- Стрельба ---
        if (hasShot)
        {
            if (fireTimer > 0f)
                fireTimer -= Time.fixedDeltaTime;

            if (fireTimer <= 0f)
            {
                Shoot(shotDir);
                fireTimer = 1f / Mathf.Max(0.01f, fireRate);
            }
        }
        else
        {
            fireTimer = 0f;
        }
    }

    // Поворот спрайта в сторону игрока (только по X)
    private void FacePlayer(float playerX)
    {
        if (spriteRenderer == null) return;

        bool playerIsRight = playerX > transform.position.x;

        // Если спрайт по умолчанию смотрит вправо:
        // playerIsRight = true  -> flipX = false
        // playerIsRight = false -> flipX = true
        // Если по умолчанию влево — инвертируем.
        spriteRenderer.flipX = !spriteFacesLeftByDefault ? playerIsRight : !playerIsRight;
    }

    private void UpdateAnimations(bool isMoving)
    {
        if (animator != null)
            animator.SetBool(moveBoolHash, isMoving);
    }

    private bool HasClearShot(out Vector2 direction)
    {
        direction = Vector2.zero;

        if (player == null)
            return false;

        Vector2 origin = firePoint != null
            ? (Vector2)firePoint.position
            : rb.position;

        Vector2 offset = (Vector2)player.position - origin;

        if (offset.sqrMagnitude < 0.001f)
            return false;

        direction = offset.normalized;

        LayerMask mask = shootObstacleLayers.value != 0
            ? shootObstacleLayers
            : obstacleLayer;

        if (mask.value == 0)
            return true;

        RaycastHit2D[] hits = Physics2D.RaycastAll(
            origin,
            direction,
            offset.magnitude,
            mask
        );

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null)
                continue;

            Transform hitTransform = hit.collider.transform;

            if (hitTransform == transform || hitTransform.IsChildOf(transform))
                continue;

            if (hitTransform == player || hitTransform.IsChildOf(player))
                continue;

            return false;
        }

        return true;
    }

    private void Shoot(Vector2 direction)
    {
        if (projectilePrefab == null || firePoint == null)
        {
            Debug.LogWarning($"[RangedEnemy] Не назначен {(projectilePrefab == null ? "Projectile Prefab" : "Fire Point")} на {name}", this);
            return;
        }

        if (animator != null)
            animator.SetTrigger(attackTriggerHash);

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