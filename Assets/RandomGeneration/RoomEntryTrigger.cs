using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class RoomEntryTrigger : MonoBehaviour
{
    private RoomController room;

    private void Awake()
    {
        room = GetComponentInParent<RoomController>();

        if (room == null)
            Debug.LogError($"[RoomEntryTrigger] Не найден RoomController в родителях {name}", this);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (room == null) return;

        if (other.GetComponentInParent<PlayerStats>() != null)
            room.Activate();
    }
}