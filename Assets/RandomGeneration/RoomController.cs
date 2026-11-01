using System.Collections.Generic;
using UnityEngine;

public enum RoomState { Inactive, Active, Cleared }

[RequireComponent(typeof(TypeRoom))]
public class RoomController : MonoBehaviour
{
    [Header("Границы комнаты")]
    [SerializeField] private Collider2D roomBounds;

    [Header("Двери")]
    [SerializeField] private List<Door> doors = new();

    [Header("Туман войны")]
    [SerializeField] private GameObject fogOverlay;

    [Header("Отладка")]
    [SerializeField] private bool logState = true;

    public static RoomController ActiveRoom { get; private set; }

    public RoomState State { get; private set; } = RoomState.Inactive;
    public TypeRoom Type { get; private set; }

    private readonly List<EnemyChase2D> enemies = new();

    private void Awake()
    {
        Type = GetComponent<TypeRoom>();
        enemies.AddRange(GetComponentsInChildren<EnemyChase2D>(true));

        foreach (var e in enemies)
            if (e != null) e.gameObject.SetActive(false);

        if (fogOverlay != null)
            fogOverlay.SetActive(true);

        // На старте все двери открыты — игрок может войти.
        SetDoorsLocked(false);

        if (logState)
            Debug.Log($"[Room] Awake {name}: enemies={enemies.Count}, doors={doors.Count}");
    }

    public void Activate()
    {
        if (State != RoomState.Inactive) return;

        // Затемняем предыдущую комнату.
        if (ActiveRoom != null && ActiveRoom != this)
            ActiveRoom.OnPlayerLeft();

        ActiveRoom = this;
        State = RoomState.Active;

        foreach (var e in enemies)
            if (e != null) e.gameObject.SetActive(true);

        if (fogOverlay != null)
            fogOverlay.SetActive(false);

        // Блокируем вход и выход.
        LockEntranceDoors();
        LockExitDoors();

        if (logState)
            Debug.Log($"[Room] {name}: Activate, enemies={enemies.Count}");
    }

    /// <summary>Вызывается EnemyChase2D.TakeDamage при смерти врага.</summary>
    public void OnEnemyDefeated(EnemyChase2D enemy)
    {
        enemies.Remove(enemy);

        if (State == RoomState.Active && enemies.Count == 0)
            Clear();
    }

    private void Clear()
    {
        State = RoomState.Cleared;

        // Открываем выход после зачистки.
        UnlockExitDoors();

        if (logState)
            Debug.Log($"[Room] {name}: Cleared, exit unlocked");
    }

    /// <summary>Вызывается, когда игрок ушёл в следующую комнату.</summary>
    public void OnPlayerLeft()
    {
        if (fogOverlay != null)
            fogOverlay.SetActive(true);

        if (logState)
            Debug.Log($"[Room] {name}: Player left, fog restored");
    }

    private void LockEntranceDoors()
    {
        foreach (var d in doors)
            if (d != null && d.Type == DoorType.Entrance)
                d.Lock();
    }

    private void LockExitDoors()
    {
        foreach (var d in doors)
            if (d != null && d.Type == DoorType.Exit)
                d.Lock();
    }

    private void UnlockExitDoors()
    {
        foreach (var d in doors)
            if (d != null && d.Type == DoorType.Exit)
                d.Unlock();
    }

    private void SetDoorsLocked(bool locked)
    {
        foreach (var d in doors)
        {
            if (d == null) continue;
            if (locked) d.Lock();
            else d.Unlock();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (roomBounds == null) return;
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.25f);
        Gizmos.DrawCube(roomBounds.bounds.center, roomBounds.bounds.size);
    }
}