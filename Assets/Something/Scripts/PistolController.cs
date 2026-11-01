using System.Collections;
using UnityEngine;

public class PistolController : MonoBehaviour
{
    public GameObject leftGun;
    public GameObject rightGun;

    public Transform PistolLeft;
    public Transform PistolRight;

    public GameObject leftLight;
    public GameObject rightLight;
    public float lightDuration;

    Camera mainCamera;

    public GameObject bulletPrefab;
    public Transform leftFirePoint;
    public Transform rightFirePoint;

    public GameTimer gameTimer;

    public PlayerController playerController;

    // [+] --- Звук выстрела ---
    [Header("Звук выстрела")]
    public AudioSource shootSource;         // AudioSource для выстрелов
    public AudioClip[] shootClips;          // Массив звуков выстрела (2-5 штук)
    [Range(0f, 0.3f)] public float pitchRange = 0.08f; // Разброс питча
    private int lastShootIndex = -1;        // Индекс прошлого звука

    // [+] --- Звук перезарядки/пустой обоймы (опционально) ---
    [Header("Звук пустой обоймы (опционально)")]
    public AudioClip emptyClip;             // Например, "щёлк" без патронов
    // [-

    void Start()
    {
        mainCamera = Camera.main;

        // [+] Автопоиск AudioSource, если не назначен вручную
        if (shootSource == null)
            shootSource = GetComponent<AudioSource>();
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

        bool mouseOnRight = direction.x >= 0;

        leftGun.gameObject.SetActive(!mouseOnRight);
        rightGun.gameObject.SetActive(mouseOnRight);

        Transform activePivot = mouseOnRight ? PistolRight : PistolLeft;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        activePivot.rotation = Quaternion.Euler(0, 0, angle);

        if (Input.GetMouseButtonDown(0))
        {
            Animator animatorGun = mouseOnRight ? rightGun.GetComponent<Animator>() : leftGun.GetComponent<Animator>();
            animatorGun.Play("Fire", 0, 0);

            Transform firePoint = mouseOnRight ? rightFirePoint : leftFirePoint;
            firePoint.GetComponent<Animator>().Play("Fire", 0, 0);

            GameObject lightPoint = mouseOnRight ? rightLight : leftLight;
            lightPoint.gameObject.SetActive(true);
            StartCoroutine(DisableLight(lightPoint));

            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

            Vector2 shootDirection = (mainCamera.ScreenToWorldPoint(Input.mousePosition) - firePoint.position).normalized;
            bullet.GetComponent<Pistol_Bullet>().Shoot(shootDirection);

            // [+] Проигрываем звук выстрела
            PlayShootSound();

            gameTimer.LessTime(1);
        }
    }

    IEnumerator DisableLight(GameObject lightPoint)
    {
        yield return new WaitForSeconds(lightDuration);
        lightPoint.gameObject.SetActive(false);
    }

    // [+] --- Логика звука выстрела ---
    void PlayShootSound()
    {
        if (shootSource == null || shootClips == null || shootClips.Length == 0)
            return;

        int index;

        if (shootClips.Length == 1)
        {
            index = 0;
        }
        else
        {
            // Не повторяем предыдущий звук
            do
            {
                index = Random.Range(0, shootClips.Length);
            } while (index == lastShootIndex);

            lastShootIndex = index;
        }

        // Небольшой разброс питча — звук не "приедается"
        shootSource.pitch = 1f + Random.Range(-pitchRange, pitchRange);
        shootSource.PlayOneShot(shootClips[index]);

        // Сбрасываем питч после проигрывания, чтобы не влиял на другие звуки
        // (если этот AudioSource используется и для чего-то ещё)
        StartCoroutine(ResetPitchAfterClip(shootClips[index].length));
    }

    IEnumerator ResetPitchAfterClip(float duration)
    {
        yield return new WaitForSeconds(duration);
        shootSource.pitch = 1f;
    }
    // [-
}