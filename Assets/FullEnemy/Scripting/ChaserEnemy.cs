
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

    [Header("Анимация")]
    [SerializeField] private Animator animator;
    [SerializeField] private string moveBoolName = "isMoving";
    [SerializeField] private string attackTriggerName = "Attack";

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

    private Vector2 previousPlayerPosition;
    private Vector2 rushTarget;

    private float rushTimer;
    private float rushCooldownTimer;
    private float attackTimer;
    private float normalSpeed;
    private float walkSoundTimer;

    private int moveBoolHash;
    private int attackTriggerHash;

    protected override void Awake()
    {
        base.Awake();

        normalSpeed = moveSpeed;

        moveBoolHash = Animator.StringToHash(moveBoolName);
        attackTriggerHash = Animator.StringToHash(attackTriggerName);

        if (animator == null)
            animator = GetComponent<Animator>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    protected override void FixedUpdate()
    {
        if (IsDead)
            return;

        base.FixedUpdate();

        if (IsDead)
            return;

        attackTimer -= Time.fixedDeltaTime;
        rushCooldownTimer -= Time.fixedDeltaTime;

        if (!FindPlayer())
        {
            moveSpeed = normalSpeed;
            StopMoving();
            UpdateAnimations(false);
            return;
        }

        if (rushTimer > 0f)
        {
            rushTimer -= Time.fixedDeltaTime;
            moveSpeed = normalSpeed * rushSpeedMultiplier;

            if (Vector2.Distance(rb.position, rushTarget) <= stopDistance)
            {
                rushTimer = 0f;
                StopMoving();
                UpdateAnimations(false);
            }
            else
            {
                MoveInDirection(GetDirectionTo(rushTarget));
                UpdateAnimations(true);
            }

            if (rushTimer <= 0f)
                moveSpeed = normalSpeed;

            return;
        }

        moveSpeed = normalSpeed;

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
        {
            MoveInDirection(GetMoveDirection(PlayerPosition));
            UpdateAnimations(true);
        }
        else
        {
            StopMoving();
            UpdateAnimations(false);
        }
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
        return PlayDeathSound();
    }

    private float PlayDeathSound()
    {
        if (audioSource == null || deathSounds == null || deathSounds.Length == 0)
            return 0f;

        AudioClip clip = deathSounds[Random.Range(0, deathSounds.Length)];

        if (clip == null)
            return 0f;

        audioSource.PlayOneShot(clip);
        return clip.length;
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
        if (IsDead || attackTimer > 0f)
            return;

        PlayerStats stats = other.GetComponentInParent<PlayerStats>();

        if (stats == null)
            return;

        stats.TakeDamage(contactDamage);

        PlayRandomSound(attackSounds);

        if (animator != null)
            animator.SetTrigger(attackTriggerHash);

        attackTimer = attackCooldown;
    }
}
