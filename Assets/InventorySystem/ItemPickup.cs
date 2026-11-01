using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [Tooltip("Предмет по умолчанию. Используется, если предмет не был задан извне.")]
    [SerializeField] private InventoryItem itemData;

    [Tooltip("Спрайт предмета в мире. Если пусто — берётся SpriteRenderer на этом объекте.")]
    [SerializeField] private SpriteRenderer worldSprite;

    private void Awake()
    {
        if (worldSprite == null)
            worldSprite = GetComponent<SpriteRenderer>();
    }

    /// <summary>Задать предмет извне (при выбросе из инвентаря).</summary>
    public void Setup(InventoryItem item)
    {
        itemData = item;
        RefreshVisual();
    }

    private void RefreshVisual()
    {
        if (worldSprite == null || itemData == null)
            return;

        // Если у InventoryItem есть поле со спрайтом мира — берём его.
        // Например: itemData.worldSprite или itemData.icon.
        worldSprite.sprite = itemData.icon;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerInventory inventory = other.GetComponentInParent<PlayerInventory>();

        if (inventory == null || itemData == null)
            return;

        if (inventory.TryAddItem(itemData))
            Destroy(gameObject);
        else
            Debug.Log("Инвентарь заполнен.");
    }
}