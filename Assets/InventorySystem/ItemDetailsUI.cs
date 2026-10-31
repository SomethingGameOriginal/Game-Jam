using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ItemDetailsUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private Text itemNameText;
    [SerializeField] private Text descriptionText;

    public void Show(InventoryItem item)
    {
        if (item == null)
            return;

        if (icon != null)
        {
            icon.sprite = item.icon;
            icon.enabled = item.icon != null;
        }

        if (itemNameText != null)
            itemNameText.text = item.itemName;

        if (descriptionText != null)
            descriptionText.text = item.description;
    }
}