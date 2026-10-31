using UnityEngine;

public class Pistol_Bullet : MonoBehaviour
{
    public float speed;

    Rigidbody2D rb;
    Vector2 direction;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Shoot(Vector2 shootDirection)
    {
        direction = shootDirection.normalized;
    }

    void FixedUpdate()
    {
        rb.linearVelocity = direction * speed;
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Wall"))
        {
            Destroy(gameObject);
        }
    }
}