using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private Image background;
    [SerializeField] private Text itemNameText;

    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.yellow;

    public void SetItem(InventoryItem item)
    {
        if (icon != null)
        {
            icon.sprite = item != null ? item.icon : null;
            icon.enabled = item != null && item.icon != null;
        }

        if (itemNameText != null)
            itemNameText.text = item != null ? item.itemName : "";
    }

    public void SetSelected(bool selected)
    {
        if (background != null)
            background.color = selected ? selectedColor : normalColor;
    }
}