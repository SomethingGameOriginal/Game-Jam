using UnityEngine;

public enum ItemType
{
    Weapon,
    Antidote
}

public class InventoryItem : MonoBehaviour
{
    public string itemName;
    [TextArea(3, 8)] public string description;
    public ItemType type;
    public Sprite icon;

    [Tooltip("Префаб пикапа, который появляется при выбрасывании")]
    public GameObject pickupPrefab;

    [Header("Для оружия")]
    public GameObject weaponPrefab;

    [Header("Для антидота")]
    public int healthRestored = 1;
}