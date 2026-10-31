using UnityEngine;

public class PistolController : MonoBehaviour
{
    public GameObject leftGun;
    public GameObject rightGun;

    public Transform PistolLeft;
    public Transform PistolRight;

    Camera mainCamera;

    public GameObject bulletPrefab;
    public Transform leftFirePoint;
    public Transform rightFirePoint;

    public PlayerController playerController;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (playerController.isDashing)
        {
            leftGun.gameObject.SetActive(false);
            rightGun.gameObject.SetActive(false);
            return;
        }

        Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0;

        Vector2 direction = mousePosition - transform.position;

        // Определяем сторону курсора
        bool mouseOnRight = direction.x >= 0;

        leftGun.gameObject.SetActive(!mouseOnRight);
        rightGun.gameObject.SetActive(mouseOnRight);

        // Выбираем активный пистолет
        Transform activePivot = mouseOnRight ? PistolRight : PistolLeft;

        // Поворачиваем его к курсору
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        activePivot.rotation = Quaternion.Euler(0, 0, angle);

        if (Input.GetMouseButtonDown(0))
        {
            Animator animatorGun = mouseOnRight ? rightGun.GetComponent<Animator>() : leftGun.GetComponent<Animator>();
            animatorGun.Play("Fire", 0, 0);

            Transform firePoint = mouseOnRight ? rightFirePoint : leftFirePoint;
            firePoint.GetComponent<Animator>().Play("Fire", 0, 0);

            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

            Vector2 shootDirection = (mainCamera.ScreenToWorldPoint(Input.mousePosition) - firePoint.position).normalized; // direction ошибку выдаёт
            
            bullet.GetComponent<Pistol_Bullet>().Shoot(shootDirection);
        }
    }

}
