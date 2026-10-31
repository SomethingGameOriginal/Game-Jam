using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private float speed = 8f;
    [SerializeField] private float lifeTime = 5f;

    private Rigidbody2D rb;
    private Vector2 direction;
    private bool hasHit;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (rb != null)
            rb.gravityScale = 0f;
    }

    public void Launch(Vector2 shootDirection, float projectileSpeed, int projectileDamage)
    {
        direction = shootDirection.normalized;
        speed = projectileSpeed;
        damage = projectileDamage;

        if (rb != null)
            rb.linearVelocity = direction * speed;

        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Hit(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Hit(collision.collider);
    }

    private void Hit(Collider2D other)
    { 

        PlayerStats stats = other.GetComponentInParent<PlayerStats>();
        if (stats != null)
            stats.TakeDamage(damage);

        if (other.GetComponent<RangedEnemy>() || other.GetComponent<ChaserEnemy>() || other.GetComponent<SniperEnemy>())
        {

        }
        if (other.GetComponent<PlayerStats>() || other.CompareTag("Wall")) { 
            Destroy(gameObject);
        }
            
    }
}