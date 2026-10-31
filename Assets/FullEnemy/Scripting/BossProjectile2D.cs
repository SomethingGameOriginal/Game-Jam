using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BossProjectile2D : MonoBehaviour
{
    [SerializeField] private float lifetime = 5f;

    private Rigidbody2D rb;
    private float damage;
    private bool initialized;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Initialize(Vector2 direction, float speed, float projectileDamage)
    {
        damage = projectileDamage;
        rb.linearVelocity = direction.normalized * speed;
        initialized = true;

        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!initialized)
            return;

        PlayerStats stats = other.GetComponentInParent<PlayerStats>();
        if (stats != null)
            stats.TakeDamage(damage);

        if (other.GetComponent<RangedEnemy>() || other.GetComponent<ChaserEnemy>() || other.GetComponent<SniperEnemy>())
        {

        }
        if (other.GetComponent<PlayerStats>() || other.CompareTag("Wall"))
        {
            Destroy(gameObject);
        }
    }
}