using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    private const int SlotCount = 3;

    [Header("Ссылки")]
    [SerializeField] private Transform weaponHolder;
    [SerializeField] private InventorySlotUI[] slotsUI = new InventorySlotUI[SlotCount];

    [Header("Окно описания")]
    [SerializeField] private GameObject detailsPanel;
    [SerializeField] private ItemDetailsUI detailsUI;

    [Header("Выбрасывание")]
    [SerializeField] private float dropDistance = 0.8f;

    private InventoryItem[] items = new InventoryItem[SlotCount];
    private int selectedSlot = -1;
    private GameObject equippedWeapon;

    private void Start()
    {
        if (detailsPanel != null)
            detailsPanel.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectSlot(2);

        if (Input.GetKeyDown(KeyCode.E))
            UseSelectedItem();

        if (Input.GetKeyDown(KeyCode.Q))
            DropSelectedItem();

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            ToggleDetails();
    }

    public bool TryAddItem(InventoryItem item)
    {
        if (item == null)
            return false;

        int freeSlot = System.Array.FindIndex(items, slot => slot == null);
        if (freeSlot == -1)
            return false;

        items[freeSlot] = item;
        UpdateSlotUI(freeSlot);

        if (selectedSlot == -1)
            SelectSlot(freeSlot);

        return true;
    }

    private void SelectSlot(int index)
    {
        selectedSlot = index;
        EquipSelectedWeapon();
        RefreshSelection();
    }

    private void EquipSelectedWeapon()
    {
        if (equippedWeapon != null)
        {
            Destroy(equippedWeapon);
            equippedWeapon = null;
        }

        if (!IsValidSelectedSlot() || weaponHolder == null)
            return;

        InventoryItem item = items[selectedSlot];

        if (item != null &&
            item.type == ItemType.Weapon &&
            item.weaponPrefab != null)
        {
            equippedWeapon = Instantiate(item.weaponPrefab, weaponHolder);
        }
    }

    private void UseSelectedItem()
    {
        if (!IsValidSelectedSlot())
            return;

        InventoryItem item = items[selectedSlot];

        if (item == null || item.type != ItemType.Antidote)
            return;

        // Подключите здесь лечение или снятие отравления.
        Debug.Log($"Использован {item.itemName}. Лечение: {item.healthRestored}");

        RemoveItem(selectedSlot);
    }

    private void DropSelectedItem()
    {
        if (!IsValidSelectedSlot())
            return;

        InventoryItem item = items[selectedSlot];

        if (item == null)
            return;

        if (item.pickupPrefab == null)
        {
            Debug.LogWarning($"Для {item.itemName} не назначен Pickup Prefab.");
            return;
        }

        Vector2 position = (Vector2)transform.position +
                           GetDropDirection() * dropDistance;

        Instantiate(item.pickupPrefab, position, Quaternion.identity);
        RemoveItem(selectedSlot);
    }

    private void ToggleDetails()
    {
        if (detailsPanel == null)
            return;

        bool opening = !detailsPanel.activeSelf;

        if (opening)
        {
            if (!IsValidSelectedSlot() || items[selectedSlot] == null)
                return;

            if (detailsUI != null)
                detailsUI.Show(items[selectedSlot]);
        }

        detailsPanel.SetActive(opening);
        Time.timeScale = opening ? 0f : 1f;
    }

    private void RemoveItem(int index)
    {
        items[index] = null;
        UpdateSlotUI(index);

        if (index == selectedSlot)
            EquipSelectedWeapon();
    }

    private void UpdateSlotUI(int index)
    {
        if (index < slotsUI.Length && slotsUI[index] != null)
            slotsUI[index].SetItem(items[index]);
    }

    private void RefreshSelection()
    {
        for (int i = 0; i < slotsUI.Length; i++)
        {
            if (slotsUI[i] != null)
                slotsUI[i].SetSelected(i == selectedSlot);
        }
    }

    private bool IsValidSelectedSlot()
    {
        return selectedSlot >= 0 && selectedSlot < SlotCount;
    }

    private Vector2 GetDropDirection()
    {
        SpriteRenderer sprite = GetComponentInChildren<SpriteRenderer>();

        if (sprite != null && sprite.flipX)
            return Vector2.left;

        return Vector2.right;
    }

    private void OnDestroy()
    {
        // Не оставляем игру на паузе при уничтожении игрока/сцены.
        Time.timeScale = 1f;
    }
}