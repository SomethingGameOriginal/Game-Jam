using UnityEngine;

public class ExitControllRoom : MonoBehaviour
{
    [SerializeField] private GameObject exit;
    [SerializeField] private LayerMask targetLayers;

    private void OnCollisionStay2D(Collision2D collision)
    {
        UpdateExit();
    }

    private void UpdateExit()
    {
        if (exit == null)
            return;

        // Проверяем коллайдеры, которые пересекаются с коллайдером этой комнаты.
        Collider2D roomCollider = GetComponent<Collider2D>();

        if (roomCollider == null)
        {
            Debug.LogWarning("На объекте ExitControllRoom нет Collider2D.", this);
            return;
        }

        Collider2D[] objectsInside = new Collider2D[32];
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(targetLayers);
        filter.useTriggers = true;

        int count = roomCollider.Overlap(filter, objectsInside);

        exit.SetActive(count > 0);
    }
}