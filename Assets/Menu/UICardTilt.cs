using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

public class UICardTilt : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    [Header("Настройки эффекта")]
    public float maxTiltAngle = 15f;
    public float zOffset = 50f;
    public float scaleMultiplier = 1.1f;
    public float tweenDuration = 0.3f;
    public float smoothSpeed = 10f;

    [Header("Звук наведения")]
    public AudioSource hoverSource;
    public AudioClip[] hoverClips;
    [Range(0f, 0.3f)] public float hoverPitchRange = 0.06f;

    [Header("Звук нажатия")]
    public AudioSource clickSource;
    public AudioClip[] clickClips;
    [Range(0f, 0.3f)] public float clickPitchRange = 0.06f;

    private RectTransform rectTransform;
    private Canvas parentCanvas;
    private Camera uiCamera;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector3 initialScale;
    private bool isHovered = false;

    private int lastHoverIndex = -1;
    private int lastClickIndex = -1;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();

        if (parentCanvas != null)
            uiCamera = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera;

        initialPosition = rectTransform.anchoredPosition3D;
        initialRotation = rectTransform.localRotation;
        initialScale = rectTransform.localScale;

        if (hoverSource == null) hoverSource = GetComponent<AudioSource>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;

        rectTransform.DOKill();

        rectTransform.DOAnchorPos3D(new Vector3(initialPosition.x, initialPosition.y, initialPosition.z - zOffset), tweenDuration).SetEase(Ease.OutBack);
        rectTransform.DOScale(initialScale * scaleMultiplier, tweenDuration).SetEase(Ease.OutBack);

        PlayRandomClip(hoverSource, hoverClips, hoverPitchRange, ref lastHoverIndex);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;

        rectTransform.DOKill();

        rectTransform.DOAnchorPos3D(initialPosition, tweenDuration).SetEase(Ease.OutCubic);
        rectTransform.DOScale(initialScale, tweenDuration).SetEase(Ease.OutCubic);
        rectTransform.DOLocalRotateQuaternion(initialRotation, tweenDuration).SetEase(Ease.OutCubic);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        PlayRandomClip(clickSource, clickClips, clickPitchRange, ref lastClickIndex);
    }

    void Update()
    {
        if (isHovered)
        {
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, Input.mousePosition, uiCamera, out localPoint);

            float normalizedX = localPoint.x / (rectTransform.rect.width * 0.5f);
            float normalizedY = localPoint.y / (rectTransform.rect.height * 0.5f);

            float targetRotationX = -normalizedY * maxTiltAngle;
            float targetRotationY = normalizedX * maxTiltAngle;

            Quaternion targetRotation = initialRotation * Quaternion.Euler(targetRotationX, targetRotationY, 0);

            rectTransform.localRotation = Quaternion.Slerp(rectTransform.localRotation, targetRotation, Time.deltaTime * smoothSpeed);
        }
    }

    void PlayRandomClip(AudioSource source, AudioClip[] clips, float pitchRange, ref int lastIndex)
    {
        if (source == null || clips == null || clips.Length == 0) return;

        int index;
        if (clips.Length == 1)
        {
            index = 0;
        }
        else
        {
            do { index = Random.Range(0, clips.Length); }
            while (index == lastIndex);
            lastIndex = index;
        }

        source.pitch = 1f + Random.Range(-pitchRange, pitchRange);
        source.PlayOneShot(clips[index]);
        StartCoroutine(ResetPitchAfterClip(source, clips[index].length));
    }

    System.Collections.IEnumerator ResetPitchAfterClip(AudioSource source, float duration)
    {
        yield return new WaitForSeconds(duration);
        if (source != null) source.pitch = 1f;
    }
}