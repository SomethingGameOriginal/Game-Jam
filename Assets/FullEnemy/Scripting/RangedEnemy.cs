
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
    [SerializeField] private LayerMask shootObstacleLayers;

    [Header("Анимация")]
    [SerializeField] private Animator animator;
    [SerializeField] private string moveBoolName = "isMoving";
    [SerializeField] private string attackTriggerName = "Attack";

    [Header("Поворот спрайта")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private bool spriteFacesLeftByDefault = false;

    [Header("Звуки")]
    [SerializeField] private AudioSource audioSource;

    [Header("Ходьба")]
    [SerializeField] private AudioClip[] walkSounds;
    [SerializeField] private float walkSoundInterval = 0.4f;

    [Header("Атака")]
    [SerializeField] private AudioClip[] attackSounds;

    [Header("Получение урона")]
    [SerializeField] private AudioClip[] damageSounds;

    [Header("Смерть")]
    [SerializeField] private AudioClip[] deathSounds;

    [Header("Случайные крики")]
    [SerializeField] private AudioClip[] screamSounds;
    [SerializeField] private float minScreamInterval = 20f;
    [SerializeField] private float maxScreamInterval = 60f;

    private float fireTimer;
    private float walkSoundTimer;
    private float screamTimer;

    private int moveBoolHash;
    private int attackTriggerHash;

    protected override void Awake()
    {
        base.Awake();

        moveBoolHash = Animator.StringToHash(moveBoolName);
        attackTriggerHash = Animator.StringToHash(attackTriggerName);

        if (animator == null)
            animator = GetComponent<Animator>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        screamTimer = GetRandomScreamInterval();
    }

    protected override void FixedUpdate()
    {
        if (IsDead)
            return;

        UpdateScream();

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

        bool hasShot = HasClearShot(out Vector2 shotDir);
        bool isMoving = false;

        FacePlayer(playerPos.x);

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

    private void FacePlayer(float playerX)
    {
        if (spriteRenderer == null)
            return;

        bool playerIsRight = playerX > transform.position.x;

        spriteRenderer.flipX = !spriteFacesLeftByDefault
            ? playerIsRight
            : !playerIsRight;
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
            walkSoundTimer = Mathf.Max(0.05f, walkSoundInterval);
        }
    }

    private void UpdateScream()
    {
        if (screamSounds == null || screamSounds.Length == 0)
            return;

        screamTimer -= Time.fixedDeltaTime;

        if (screamTimer <= 0f)
        {
            PlayRandomSound(screamSounds);
            screamTimer = GetRandomScreamInterval();
        }
    }

    private float GetRandomScreamInterval()
    {
        float min = Mathf.Max(0f, minScreamInterval);
        float max = Mathf.Max(min, maxScreamInterval);

        return Random.Range(min, max);
    }

    private void PlayRandomSound(AudioClip[] sounds)
    {
        if (audioSource == null || sounds == null || sounds.Length == 0)
            return;

        AudioClip clip = sounds[Random.Range(0, sounds.Length)];

        if (clip != null)
            audioSource.PlayOneShot(clip);
    }

    protected override void OnDamaged()
    {
        PlayRandomSound(damageSounds);
    }

    protected override float OnDeath()
    {
        if (audioSource == null || deathSounds == null || deathSounds.Length == 0)
            return 0f;

        AudioClip clip = deathSounds[Random.Range(0, deathSounds.Length)];

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
            Debug.LogWarning(
                $"[RangedEnemy] Не назначен {(projectilePrefab == null ? "Projectile Prefab" : "Fire Point")} на {name}",
                this
            );

            return;
        }

        if (animator != null)
            animator.SetTrigger(attackTriggerHash);

        PlayRandomSound(attackSounds);

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
