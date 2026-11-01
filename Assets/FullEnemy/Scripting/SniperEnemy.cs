using UnityEngine;

public class SniperEnemy : EnemyChase2D
{
    [Header("Дистанция снайпера")]
    [SerializeField] private float preferredDistance = 9f;
    [SerializeField] private float minDistance = 7f;
    [SerializeField] private float maxDistance = 12f;

    [Header("Стрельба")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRate = 0.5f;
    [SerializeField] private float projectileSpeed = 12f;
    [SerializeField] private int projectileDamage = 1;

    [Header("Анимация")]
    [SerializeField] private Animator animator;
    [SerializeField] private string moveBoolName = "isMoving";
    [SerializeField] private string attackTriggerName = "Attack";

    [Header("Тело")]
    [Tooltip("SpriteRenderer ТЕЛА (не оружия!). Перетащи вручную, чтобы не путаться.")]
    [SerializeField] private SpriteRenderer bodySpriteRenderer;
    [SerializeField] private bool bodyFacesLeftByDefault = false;

    [Header("Оружие")]
    [Tooltip("Пустой объект-пивот, вращается через rotation. Спрайт винтовки — его дочерний объект.")]
    [SerializeField] private Transform weaponPivot;
    [Tooltip("SpriteRenderer САМОЙ ВИНТОВКИ (для flipY, чтобы не была вверх ногами).")]
    [SerializeField] private SpriteRenderer weaponSpriteRenderer;
    [Tooltip("Смещение угла, если ствол нарисован не вдоль +X.")]
    [SerializeField] private float weaponAngleOffset = 0f;

    private float fireTimer;
    private int moveBoolHash;
    private int attackTriggerHash;

    protected override void Awake()
    {
        base.Awake();

        moveBoolHash = Animator.StringToHash(moveBoolName);
        attackTriggerHash = Animator.StringToHash(attackTriggerName);

        if (animator == null)
            animator = GetComponent<Animator>();

        // ВАЖНО: не используем GetComponentInChildren для тела,
        // потому что первым может найтись спрайт оружия.
        if (bodySpriteRenderer == null)
            bodySpriteRenderer = GetComponent<SpriteRenderer>();

        if (weaponPivot == null)
        {
            Transform found = transform.Find("WeaponPivot");
            if (found == null) found = transform.Find("Weapon");
            if (found != null) weaponPivot = found;
        }

        if (weaponSpriteRenderer == null && weaponPivot != null)
            weaponSpriteRenderer = weaponPivot.GetComponentInChildren<SpriteRenderer>();
    }

    protected override void FixedUpdate()
    {
        bool seeing = FindPlayer();

        if (!seeing)
        {
            StopMoving();
            UpdateAnimations(false);
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
        bool hasShot = HasClearShot(out Vector2 shotDirection);
        bool isMoving = false;

        // --- Поворот тела и оружия ---
        FacePlayer(playerPos.x);
        AimWeapon(playerPos);

        // --- Движение ---
        if (distance < minDistance)
        {
            Vector2 retreatTarget = rb.position - toPlayer.normalized * 3f;
            MoveInDirection(GetDirectionTo(retreatTarget));
            isMoving = true;
        }
        else if (distance > maxDistance)
        {
            MoveInDirection(GetDirectionTo(playerPos));
            isMoving = true;
        }
        else if (distance < preferredDistance - 0.5f)
        {
            Vector2 retreatTarget = rb.position - toPlayer.normalized * 2f;
            MoveInDirection(GetDirectionTo(retreatTarget));
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
                Shoot(shotDirection);
                fireTimer = 1f / Mathf.Max(0.01f, fireRate);
            }
        }
        else
        {
            fireTimer = 0f;
        }
    }

    // Переворот ТЕЛА
    private void FacePlayer(float playerX)
    {
        if (bodySpriteRenderer == null) return;

        bool playerIsRight = playerX > transform.position.x;
        bodySpriteRenderer.flipX = !bodyFacesLeftByDefault ? playerIsRight : !playerIsRight;
    }

    // Поворот ОРУЖИЯ к игроку
    private void AimWeapon(Vector2 playerPos)
    {
        if (weaponPivot == null) return;

        Vector2 origin = weaponPivot.position;
        Vector2 direction = playerPos - origin;

        if (direction.sqrMagnitude < 0.0001f) return;

        // Считаем угол всегда от +X
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + weaponAngleOffset;

        // Если игрок слева — зеркалим пивот по X и корректируем угол,
        // чтобы ствол смотрел в правильную сторону.
        bool facingLeft = direction.x < 0f;

        Vector3 scale = weaponPivot.localScale;
        scale.x = facingLeft ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
        weaponPivot.localScale = scale;

        // При зеркалировании угол нужно инвертировать, иначе винтовка "отразится" не туда.
        if (facingLeft)
        {
            angle = 180f - angle + weaponAngleOffset * 2f;
            // или проще:
            // angle = Mathf.Atan2(-direction.y, -direction.x) * Mathf.Rad2Deg + weaponAngleOffset;
        }

        weaponPivot.rotation = Quaternion.Euler(0f, 0f, angle);

        // Никакого flipY — он ломает вращение.
        if (weaponSpriteRenderer != null)
            weaponSpriteRenderer.flipY = false;
    }

    private void UpdateAnimations(bool isMoving)
    {
        if (animator != null)
            animator.SetBool(moveBoolHash, isMoving);
    }

    private bool HasClearShot(out Vector2 direction)
    {
        Vector2 origin = firePoint != null
            ? (Vector2)firePoint.position
            : rb.position;

        Vector2 offset = (Vector2)player.position - origin;

        if (offset.sqrMagnitude < 0.001f)
        {
            direction = Vector2.zero;
            return false;
        }

        direction = offset.normalized;

        if (obstacleLayer.value == 0)
            return false;

        RaycastHit2D[] hits = Physics2D.RaycastAll(
            origin,
            direction,
            offset.magnitude,
            obstacleLayer
        );

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null)
                continue;

            Transform hitTransform = hit.collider.transform;

            if (hitTransform == transform || hitTransform.IsChildOf(transform))
                continue;

            return false;
        }

        return true;
    }

    private void Shoot(Vector2 direction)
    {
        if (projectilePrefab == null || firePoint == null)
            return;

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
            Debug.LogError(
                "На Projectile Prefab отсутствует EnemyProjectile",
                projectile
            );
    }
}