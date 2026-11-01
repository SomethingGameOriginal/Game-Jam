using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class UICardTilt : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Настройки эффекта")]
    [Tooltip("Максимальный угол наклона карточки")]
    public float maxTiltAngle = 15f;

    [Tooltip("Насколько карточка выдвигается вперед по оси Z при наведении (в пикселях/единицах UI)")]
    public float zOffset = 50f;

    [Tooltip("Множитель увеличения размера при наведении (1.1 = +10%)")]
    public float scaleMultiplier = 1.1f;

    [Tooltip("Длительность анимации приближения/удаления (в секундах)")]
    public float tweenDuration = 0.3f;

    [Tooltip("Скорость возврата и следования за мышкой")]
    public float smoothSpeed = 10f;

    private RectTransform rectTransform;
    private Canvas parentCanvas;
    private Camera uiCamera;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector3 initialScale;
    private bool isHovered = false;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();

        // Определяем камеру в зависимости от режима рендеринга Canvas
        if (parentCanvas != null)
        {
            uiCamera = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera;
        }

        initialPosition = rectTransform.anchoredPosition3D;
        initialRotation = rectTransform.localRotation;
        initialScale = rectTransform.localScale;
    }

    // Срабатывает, когда мышь заходит на UI-объект
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;

        // Прерываем старые твины, чтобы не было конфликтов при быстром наведении
        rectTransform.DOKill();

        // Выдвигаем вперёд по Z и увеличиваем относительно твоего исходного масштаба
        rectTransform.DOAnchorPos3D(new Vector3(initialPosition.x, initialPosition.y, initialPosition.z - zOffset), tweenDuration).SetEase(Ease.OutBack);
        rectTransform.DOScale(initialScale * scaleMultiplier, tweenDuration).SetEase(Ease.OutBack);
    }

    // Срабатывает, когда мышь уходит с UI-объекта
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;

        rectTransform.DOKill();

        // Возвращаем всё в исходное состояние
        rectTransform.DOAnchorPos3D(initialPosition, tweenDuration).SetEase(Ease.OutCubic);
        rectTransform.DOScale(initialScale, tweenDuration).SetEase(Ease.OutCubic);
        rectTransform.DOLocalRotateQuaternion(initialRotation, tweenDuration).SetEase(Ease.OutCubic);
    }

    void Update()
    {
        if (isHovered)
        {
            Vector2 localPoint;
            // Вычисляем положение мыши относительно центра нашей карточки
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, Input.mousePosition, uiCamera, out localPoint);

            // Нормализуем координаты в диапазоне примерно от -1 до 1
            float normalizedX = localPoint.x / (rectTransform.rect.width * 0.5f);
            float normalizedY = localPoint.y / (rectTransform.rect.height * 0.5f);

            // Считаем углы наклона за мышкой
            float targetRotationX = -normalizedY * maxTiltAngle;
            float targetRotationY = normalizedX * maxTiltAngle;

            Quaternion targetRotation = initialRotation * Quaternion.Euler(targetRotationX, targetRotationY, 0);

            // Плавно поворачиваем карточку
            rectTransform.localRotation = Quaternion.Slerp(rectTransform.localRotation, targetRotation, Time.deltaTime * smoothSpeed);
        }
    }
}