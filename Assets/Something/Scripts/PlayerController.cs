using UnityEngine;

public class PlayerController : MonoBehaviour
{
    Rigidbody2D rb;
    Animator animator;
    SpriteRenderer spriteRenderer;

    public float speed;
    Vector2 move;
    Vector2 lastMove = Vector2.down;

    public float dashSpeed = 15; // Скорость рывка
    public float dashDistance = 0.2f; // Дальность рывка
    public float dashDuration; // Длительность
    public float dashCooldown = 1; // Время перезарядки рывка

    bool isDashing = false;
    float dashTimeLeft; // оставшееся время
    float dashCooldownTimer; // таймер перезарядки рывка
    Vector2 dashDir; // мчится к тебе (так перевёл гугл переводчик)
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    void Update()
    {
        if (dashCooldownTimer > 0)
            dashCooldownTimer -= Time.deltaTime;

        if (isDashing)
        {
            dashTimeLeft -= Time.deltaTime;
            if (dashTimeLeft <= 0)
            {
                isDashing = false;
            }
            return;
        }

        if (move.sqrMagnitude > 0.01f)
        {
            if (Mathf.Abs(move.x) > Mathf.Abs(move.y))
            {
                lastMove = new Vector2(Mathf.Sign(move.x), 0);
            }
            else
            {
                lastMove = new Vector2(0, Mathf.Sign(move.y));
            }
        }

        move.x = Input.GetAxisRaw("Horizontal");
        move.y = Input.GetAxisRaw("Vertical");

        if (Input.GetKeyDown(KeyCode.Space) && dashCooldownTimer <= 0)
        {
            StartDash();
            return;
        }

        PlayerAnimation();
    }
    void FixedUpdate()
    {
        if (isDashing)
        {
            rb.MovePosition(rb.position + dashDir * dashSpeed * Time.fixedDeltaTime);
        }
        else
        {
            rb.MovePosition(rb.position + move.normalized * speed * Time.fixedDeltaTime);
        }
    }
    void StartDash()
    {
        isDashing = true;
        dashCooldownTimer = dashCooldown;
        dashTimeLeft = dashDuration;

        // Определяем направление рывка
        if (move.sqrMagnitude > 0.01f)
        {
            dashDir = move.normalized;
        }
        else
        {
            dashDir = lastMove;
        }

        animator.Play("Dash");
    }
    void PlayerAnimation()
    {
        if (move.sqrMagnitude > 0.01f)
        {
            // Выбираем приоритетное направление для lastMove, чтобы избежать странных рывков по диагонали
            if (Mathf.Abs(move.x) > Mathf.Abs(move.y))
            {
                lastMove = new Vector2(Mathf.Sign(move.x), 0);
            }
            else
            {
                lastMove = new Vector2(0, Mathf.Sign(move.y));
            }

            if (move.y > 0)
            {
                animator.Play("WalkUp");
            }
            else if (move.y < 0)
            {
                animator.Play("WalkDown");
            }
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
}
