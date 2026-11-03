
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
    [SerializeField] private SpriteRenderer bodySpriteRenderer;
    [SerializeField] private bool bodyFacesLeftByDefault = false;

    [Header("Оружие")]
    [SerializeField] private Transform weaponPivot;
    [SerializeField] private SpriteRenderer weaponSpriteRenderer;
    [SerializeField] private float weaponAngleOffset = 0f;

    [Header("Звуки")]
    [SerializeField] private AudioSource audioSource;

    [Header("Ходьба")]
    [SerializeField] private AudioClip[] walkSounds;
    [SerializeField] private float walkSoundInterval = 0.4f;

    [Header("Выстрел")]
    [SerializeField] private AudioClip[] attackSounds;

    [Header("Получение урона")]
    [SerializeField] private AudioClip[] damageSounds;

    [Header("Смерть")]
    [SerializeField] private AudioClip[] deathSounds;

    private float fireTimer;
    private float walkSoundTimer;

    private int moveBoolHash;
    private int attackTriggerHash;

    protected override void Awake()
    {
        base.Awake();

        moveBoolHash = Animator.StringToHash(moveBoolName);
        attackTriggerHash = Animator.StringToHash(attackTriggerName);

        if (animator == null)
            animator = GetComponent<Animator>();

        if (bodySpriteRenderer == null)
            bodySpriteRenderer = GetComponent<SpriteRenderer>();

        if (weaponPivot == null)
        {
            Transform found = transform.Find("WeaponPivot");

            if (found == null)
                found = transform.Find("Weapon");

            if (found != null)
                weaponPivot = found;
        }

        if (weaponSpriteRenderer == null && weaponPivot != null)
            weaponSpriteRenderer =
                weaponPivot.GetComponentInChildren<SpriteRenderer>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    protected override void FixedUpdate()
    {
        if (IsDead)
            return;

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
        if (player == null || IsDead)
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

        // Поворот тела и оружия.
        FacePlayer(playerPos.x);
        AimWeapon(playerPos);

        // Движение.
        if (distance < minDistance)
        {
            Vector2 retreatTarget =
                rb.position - toPlayer.normalized * 3f;

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
            Vector2 retreatTarget =
                rb.position - toPlayer.normalized * 2f;

            MoveInDirection(GetDirectionTo(retreatTarget));
            isMoving = true;
        }
        else
        {
            StopMoving();
        }

        UpdateAnimations(isMoving);

        // Стрельба.
        if (hasShot)
        {
            if (fireTimer > 0f)
                fireTimer -= Time.fixedDeltaTime;

            if (fireTimer <= 0f)
            {
                Shoot(shotDirection);

                fireTimer = 1f /
                    Mathf.Max(0.01f, fireRate);
            }
        }
        else
        {
            fireTimer = 0f;
        }
    }

    private void FacePlayer(float playerX)
    {
        if (bodySpriteRenderer == null)
            return;

        bool playerIsRight = playerX > transform.position.x;

        bodySpriteRenderer.flipX =
            !bodyFacesLeftByDefault
                ? playerIsRight
                : !playerIsRight;
    }

    private void AimWeapon(Vector2 playerPos)
    {
        if (weaponPivot == null)
            return;

        Vector2 origin = weaponPivot.position;
        Vector2 direction = playerPos - origin;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        float angle =
            Mathf.Atan2(direction.y, direction.x) *
            Mathf.Rad2Deg + weaponAngleOffset;

        bool facingLeft = playerPos.x < transform.position.x;

        if (facingLeft)
        {
            angle += 180f;

            if (weaponSpriteRenderer != null)
            {
                weaponSpriteRenderer.flipX = true;
                weaponSpriteRenderer.flipY = false;
            }
        }
        else
        {
            if (weaponSpriteRenderer != null)
            {
                weaponSpriteRenderer.flipX = false;
                weaponSpriteRenderer.flipY = false;
            }
        }

        weaponPivot.rotation =
            Quaternion.Euler(0f, 0f, angle);
    }

    private void UpdateAnimations(bool isMoving)
    {
        if (animator != null)
            animator.SetBool(moveBoolHash, isMoving);

        UpdateWalkSound(isMoving);
    }

    private void UpdateWalkSound(bool isMoving)
    {
        if (!isMoving)
        {
            walkSoundTimer = 0f;
            return;
        }

        walkSoundTimer -= Time.fixedDeltaTime;

        if (walkSoundTimer <= 0f)
        {
            PlayRandomSound(walkSounds);

            walkSoundTimer =
                Mathf.Max(0.05f, walkSoundInterval);
        }
    }

    private void PlayRandomSound(AudioClip[] sounds)
    {
        if (audioSource == null ||
            sounds == null ||
            sounds.Length == 0)
            return;

        AudioClip clip =
            sounds[Random.Range(0, sounds.Length)];

        if (clip != null)
            audioSource.PlayOneShot(clip);
    }

    protected override void OnDamaged()
    {
        PlayRandomSound(damageSounds);
    }

    protected override float OnDeath()
    {
        if (audioSource == null ||
            deathSounds == null ||
            deathSounds.Length == 0)
            return 0f;

        AudioClip clip =
            deathSounds[Random.Range(0, deathSounds.Length)];

        if (clip == null)
            return 0f;

        audioSource.PlayOneShot(clip);

        return clip.length;
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

            if (hitTransform == transform ||
                hitTransform.IsChildOf(transform))
                continue;

            if (hitTransform == player ||
                hitTransform.IsChildOf(player))
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

        PlayRandomSound(attackSounds);

        GameObject projectile = Instantiate(
            projectilePrefab,
            firePoint.position,
            Quaternion.identity
        );

        EnemyProjectile bullet =
            projectile.GetComponent<EnemyProjectile>();

        if (bullet != null)
        {
            bullet.Launch(
                direction,
                projectileSpeed,
                projectileDamage
            );
        }
        else
        {
            Debug.LogError(
                "На Projectile Prefab отсутствует EnemyProjectile",
                projectile
            );
        }
    }
}
