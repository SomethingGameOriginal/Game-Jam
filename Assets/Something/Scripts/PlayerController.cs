using UnityEngine;
using System.Collections; // Обязательно для использования корутин

public class PlayerController : MonoBehaviour
{
    Rigidbody2D rb;
    Animator animator;
    SpriteRenderer spriteRenderer;

    public float speed;
    Vector2 move;
    Vector2 lastMove = Vector2.down;

    public float dashSpeed = 15;
    public float dashDistance = 0.2f;
    public float dashDuration;
    public float dashCooldown = 1;

    public bool isDashing = false;
    float dashTimeLeft;
    float dashCooldownTimer;
    Vector2 dashDir;

    // [+] --- Звук шагов ---
    [Header("Звук шагов")]
    public AudioSource footstepSource;
    public AudioClip[] footstepClips;
    public float stepInterval = 0.35f;
    private float stepTimer;
    private int lastStepIndex = -1;
    // [-]

    // [+] --- Звуки рывка ---
    [Header("Звуки рывка")]
    public AudioSource dashSource;          // Источник звука для рывка (можно оставить тем же или выдать отдельный)
    public AudioClip[] dashStartClips;      // Звуки, играющие СРАЗУ при рывке
    public AudioClip[] dashDelayClips;      // Звуки, играющие С ЗАТЯЖКОЙ (позже) во время рывка
    public float dashSoundDelay = 0.08f;    // Задержка перед вторым звуком (в секундах)

    private int lastDashStartIndex = -1;
    private int lastDashDelayIndex = -1;

    // [-]

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (footstepSource == null)
            footstepSource = GetComponent<AudioSource>();

        // Автопоиск AudioSource для рывка, если не назначен
        if (dashSource == null)
            dashSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (dashCooldownTimer > 0)
            dashCooldownTimer -= Time.deltaTime;

        if (isDashing)
        {
            dashTimeLeft -= Time.deltaTime;
            if (dashTimeLeft <= 0)
                isDashing = false;
            return;
        }

        if (move.sqrMagnitude > 0.01f)
        {
            if (Mathf.Abs(move.x) > Mathf.Abs(move.y))
                lastMove = new Vector2(Mathf.Sign(move.x), 0);
            else
                lastMove = new Vector2(0, Mathf.Sign(move.y));
        }

        move.x = Input.GetAxisRaw("Horizontal");
        move.y = Input.GetAxisRaw("Vertical");

        if (Input.GetMouseButtonDown(1) && dashCooldownTimer <= 0)
        {
            StartDash();
            return;
        }

        PlayerAnimation();
        UpdateFootsteps();
    }

    void FixedUpdate()
    {
        if (isDashing)
            rb.MovePosition(rb.position + dashDir * dashSpeed * Time.fixedDeltaTime);
        else
            rb.MovePosition(rb.position + move.normalized * speed * Time.fixedDeltaTime);
    }

    void StartDash()
    {
        isDashing = true;
        dashCooldownTimer = dashCooldown;
        dashTimeLeft = dashDuration;

        if (move.sqrMagnitude > 0.01f)
            dashDir = move.normalized;
        else
            dashDir = lastMove;

        animator.Play("Dash");

        // [+] Воспроизведение звуков рывка
        PlayRandomDashStartSound();

        if (dashDelayClips != null && dashDelayClips.Length > 0)
        {
            // Запускаем корутину для воспроизведения звука с задержкой
            StartCoroutine(PlayDelayedDashSoundRoutine());
        }
    }

    void PlayerAnimation()
    {
        if (move.sqrMagnitude > 0.01f)
        {
            if (Mathf.Abs(move.x) > Mathf.Abs(move.y))
                lastMove = new Vector2(Mathf.Sign(move.x), 0);
            else
                lastMove = new Vector2(0, Mathf.Sign(move.y));

            if (move.y > 0)
                animator.Play("WalkUp");
            else if (move.y < 0)
                animator.Play("WalkDown");
            else if (move.x < 0)
            {
                spriteRenderer.flipX = true;
                animator.Play(lastMove == Vector2.up ? "WalkUp" : "WalkDown");
            }
            else if (move.x > 0)
            {
                spriteRenderer.flipX = false;
                animator.Play(lastMove == Vector2.up ? "WalkUp" : "WalkDown");
            }
        }
        else
        {
            animator.Play("Idle");
        }
    }

    // [+] --- Логика звука шагов ---
    void UpdateFootsteps()
    {
        bool isWalking = move.sqrMagnitude > 0.01f && !isDashing;

        if (!isWalking)
        {
            stepTimer = 0f;
            return;
        }

        stepTimer -= Time.deltaTime;
        if (stepTimer <= 0f)
        {
            PlayRandomFootstep();
            stepTimer = stepInterval;
        }
    }

    void PlayRandomFootstep()
    {
        if (footstepSource == null || footstepClips == null || footstepClips.Length == 0)
            return;

        int index = GetRandomClipIndex(footstepClips.Length, ref lastStepIndex);
        footstepSource.pitch = Random.Range(0.92f, 1.08f);
        footstepSource.PlayOneShot(footstepClips[index]);
    }
    // [-]

    // [+] --- Логика звуков рывка ---
    IEnumerator PlayDelayedDashSoundRoutine()
    {
        // Ждем указанное время перед включением второго звука
        yield return new WaitForSeconds(dashSoundDelay);

        // Проверяем, что персонаж всё еще совершает рывок (на всякий случай)
        if (isDashing)
        {
            PlayRandomDashDelaySound();
        }
    }

    void PlayRandomDashStartSound()
    {
        if (dashSource == null || dashStartClips == null || dashStartClips.Length == 0)
            return;

        int index = GetRandomClipIndex(dashStartClips.Length, ref lastDashStartIndex);
        dashSource.pitch = Random.Range(0.92f, 1.08f);
        dashSource.PlayOneShot(dashStartClips[index]);
    }

    void PlayRandomDashDelaySound()
    {
        if (dashSource == null || dashDelayClips == null || dashDelayClips.Length == 0)
            return;

        int index = GetRandomClipIndex(dashDelayClips.Length, ref lastDashDelayIndex);
        dashSource.pitch = Random.Range(0.92f, 1.08f);
        dashSource.PlayOneShot(dashDelayClips[index]);
    }
    // [-]

    // Универсальный метод для выбора случайного элемента без повторения подряд
    int GetRandomClipIndex(int length, ref int lastIndex)
    {
        if (length <= 1) return 0;

        int index;
        do
        {
            index = Random.Range(0, length);
        } while (index == lastIndex);

        lastIndex = index;
        return index;
    }
}