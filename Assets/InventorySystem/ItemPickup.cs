using UnityEngine;
using UnityEngine.Audio;

public class ItemPickup : MonoBehaviour
{
    [Tooltip("Предмет по умолчанию. Используется, если предмет не был задан извне.")]
    [SerializeField] private InventoryItem itemData;

    [Tooltip("Спрайт предмета в мире. Если пусто — берётся SpriteRenderer на этом объекте.")]
    [SerializeField] private SpriteRenderer worldSprite;
    AudioSource audioSource;
    private void Awake()
    {
        if (worldSprite == null)
            worldSprite = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
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
        {
            // 1. Проигрываем звук
            audioSource.Play();

            // 2. Отключаем спрайт и коллайдер, чтобы предмет визуально исчез прямо сейчас
            if (worldSprite != null)
                worldSprite.enabled = false;

            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
                col.enabled = false;

            // Отключаем этот скрипт, чтобы не сработал повторный подбор
            enabled = false;

            // 3. Уничтожаем объект с задержкой, равной длительности звука (или 1 секунда по умолчанию)
            float delay = (audioSource != null && audioSource.clip != null) ? audioSource.clip.length : .500f;
            Destroy(gameObject, delay);
        }
        else
        {
            Debug.Log("Инвентарь заполнен.");
        }
    }
}