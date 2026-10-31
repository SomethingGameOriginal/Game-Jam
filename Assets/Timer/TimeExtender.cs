using UnityEngine;

public class TimeExtender : MonoBehaviour
{
    [SerializeField] private float secondsToAdd = 15f;
    [SerializeField] private bool oneUse = true;
    [SerializeField] private Sprite loss;
    [SerializeField] private Animator anim;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Animator")]
    [SerializeField] private string animationTrigger = "Use";

    private bool playerNearby;
    private bool used;
    private GameTimer gameTimer;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (anim == null)
            anim = GetComponent<Animator>();
    }

    private void Update()
    {
        if (!playerNearby || used || !Input.GetKeyDown(KeyCode.F))
            return;

        if (gameTimer == null)
        {
            Debug.LogWarning("Не найден GameTimer.");
            return;
        }

        gameTimer.AddTime(secondsToAdd);

        if (oneUse)
            used = true;

        // Запускает один раз анимацию с Trigger-параметром "Use".
        if (anim != null)
            anim.SetTrigger(animationTrigger);

        // Меняет спрайт на loss.
        if (spriteRenderer != null && loss != null)
            spriteRenderer.sprite = loss;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerInventory>() == null)
            return;

        playerNearby = true;
        gameTimer = FindFirstObjectByType<GameTimer>();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerInventory>() == null)
            return;

        playerNearby = false;
        gameTimer = null;
    }
}