using UnityEngine;
using DG.Tweening;

public class CardTilt : MonoBehaviour
{
    [Header("Настройки эффекта")]
    [Tooltip("Максимальный угол наклона карточки")]
    public float maxTiltAngle = 15f;

    [Tooltip("Насколько карточка выдвигается вперед по оси Z при наведении")]
    public float zOffset = 0.5f;

    [Tooltip("Множитель увеличения размера при наведении (1.1 = +10%)")]
    public float scaleMultiplier = 1.1f;

    [Tooltip("Длительность анимации приближения/удаления (в секундах)")]
    public float tweenDuration = 0.3f;

    [Tooltip("Скорость возврата и следования за мышкой")]
    public float smoothSpeed = 10f;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector3 initialScale; // Сохраняем твой изначальный масштаб
    private bool isHovered = false;
    private Camera cam;

    void Start()
    {
        cam = Camera.main;
        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;
        initialScale = transform.localScale; // Запоминаем то, что ты настроил в инспекторе
    }

    void OnMouseEnter()
    {
        isHovered = true;

        // Выдвигаем вперёд по Z и увеличиваем относительно ТВОЕГО исходного масштаба
        transform.DOLocalMoveZ(initialPosition.z - zOffset, tweenDuration).SetEase(Ease.OutBack);
        transform.DOScale(initialScale * scaleMultiplier, tweenDuration).SetEase(Ease.OutBack);
    }

    void OnMouseExit()
    {
        isHovered = false;

        // Возвращаем позицию, начальный поворот и твой исходный масштаб
        transform.DOLocalMoveZ(initialPosition.z, tweenDuration).SetEase(Ease.OutCubic);
        transform.DOScale(initialScale, tweenDuration).SetEase(Ease.OutCubic);
        transform.DOLocalRotateQuaternion(initialRotation, tweenDuration).SetEase(Ease.OutCubic);
    }

    void Update()
    {
        if (isHovered)
        {
            Vector3 mousePos = cam.WorldToScreenPoint(transform.position);

            float xOffset = (Input.mousePosition.x - mousePos.x) / Screen.width;
            float yOffset = (Input.mousePosition.y - mousePos.y) / Screen.height;

            float targetRotationX = -yOffset * maxTiltAngle * 2f;
            float targetRotationY = xOffset * maxTiltAngle * 2f;

            Quaternion targetRotation = initialRotation * Quaternion.Euler(targetRotationX, targetRotationY, 0);

            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * smoothSpeed);
        }
    }
}
