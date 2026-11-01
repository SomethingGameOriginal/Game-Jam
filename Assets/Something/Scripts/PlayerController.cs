using UnityEngine;

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
    public AudioSource footstepSource;      // AudioSource для шагов
    public AudioClip[] footstepClips;       // Массив звуков шагов (2-5 штук)
    public float stepInterval = 0.35f;      // Интервал между шагами (сек)
    private float stepTimer;                // Таймер до следующего шага
    private int lastStepIndex = -1;         // Индекс прошлого звука
    // [-

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // [+] Автопоиск AudioSource, если не назначен вручную
        if (footstepSource == null)
            footstepSource = GetComponent<AudioSource>();
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

        // [+] Обновление звука шагов
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
            // Персонаж остановился — сбрасываем таймер,
            // чтобы следующий шаг прозвучал сразу при начале движения
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

        int index;

        // Если звук один — просто играем его
        if (footstepClips.Length == 1)
        {
            index = 0;
        }
        else
        {
            // Выбираем случайный индекс, отличный от предыдущего
            do
            {
                index = Random.Range(0, footstepClips.Length);
            } while (index == lastStepIndex);

            lastStepIndex = index;
        }

        // Небольшой рандом питча — добавляет "живости"
        footstepSource.pitch = Random.Range(0.92f, 1.08f);
        footstepSource.PlayOneShot(footstepClips[index]);
    }
    // [-
}