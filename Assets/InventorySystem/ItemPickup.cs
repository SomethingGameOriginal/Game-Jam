using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [SerializeField] private InventoryItem itemData;

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerInventory inventory =
            other.GetComponentInParent<PlayerInventory>();

        if (inventory == null || itemData == null)
            return;

        if (inventory.TryAddItem(itemData))
            Destroy(gameObject);
        else
            Debug.Log("Инвентарь заполнен.");
    }
}