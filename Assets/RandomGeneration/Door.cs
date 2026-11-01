using UnityEngine;

public enum DoorType { Entrance, Exit }

public class Door : MonoBehaviour, IDoor
{
    [Header("Тип двери")]
    [SerializeField] private DoorType doorType = DoorType.Exit;

    [Header("Блокировка")]
    [Tooltip("Коллайдер, который физически перекрывает проход. Может быть пустым, если дверь только визуальная.")]
    [SerializeField] private Collider2D blocker;

    [Tooltip("Спрайт двери для смены цвета (опционально).")]
    [SerializeField] private SpriteRenderer visual;

    [Header("Цвета")]
    [SerializeField] private Color lockedColor = Color.red;
    [SerializeField] private Color unlockedColor = Color.green;

    public DoorType Type => doorType;

    public void Lock()
    {
        if (blocker != null) blocker.enabled = true;
        if (visual != null) visual.color = lockedColor;
    }

    public void Unlock()
    {
        if (blocker != null) blocker.enabled = false;
        if (visual != null) visual.color = unlockedColor;
    }
}

public interface IDoor
{
    void Lock();
    void Unlock();
}