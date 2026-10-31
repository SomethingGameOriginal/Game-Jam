using UnityEngine;

public class PistolController : MonoBehaviour
{
    public Transform leftGun;
    public Transform rightGun;

    Camera mainCamera;

    public GameObject bulletPrefab;
    public Transform leftFirePoint;
    public Transform rightFirePoint;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0;

        Vector2 direction = mousePosition - transform.position;

        // Определяем сторону курсора
        bool mouseOnRight = direction.x >= 0;

        leftGun.gameObject.SetActive(!mouseOnRight);
        rightGun.gameObject.SetActive(mouseOnRight);

        // Выбираем активный пистолет
        Transform activeGun = mouseOnRight ? rightGun : leftGun;

        // Поворачиваем его к курсору
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        activeGun.rotation = Quaternion.Euler(0, 0, angle);

        if (Input.GetMouseButtonDown(0))
        {
            Transform firePoint = mouseOnRight ? rightFirePoint : leftFirePoint;

            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

            Vector2 shootDirection = (mainCamera.ScreenToWorldPoint(Input.mousePosition) - firePoint.position).normalized; // direction ошибку выдаёт

            bullet.GetComponent<Pistol_Bullet>().Shoot(direction);
        }
    }

}
