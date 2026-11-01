using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillDetailsPanel : MonoBehaviour
{
    [Header("Инфо")]
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text description;
    [SerializeField] private TMP_Text cost;
    [SerializeField] private TMP_Text parentText;   // "Наследуется от: ..."

    [Header("Цветовой акцент (опционально)")]
    [SerializeField] private Image accentStripe;

    [Header("Прокачка")]
    [SerializeField] private Button unlockButton;
    [SerializeField] private TMP_Text buttonLabel;

    private SkillDefinition selected;

    private void Awake() => gameObject.SetActive(false);

    private void OnEnable()
    {
        if (SkillTreeManager.Instance != null)
            SkillTreeManager.Instance.Changed += Refresh;
    }

    private void OnDisable()
    {
        if (SkillTreeManager.Instance != null)
            SkillTreeManager.Instance.Changed -= Refresh;
    }

    public void Show(SkillDefinition skill)
    {
        selected = skill;
        gameObject.SetActive(true);
        Refresh();
    }

    public void Hide()
    {
        selected = null;
        gameObject.SetActive(false);
    }

    public void Refresh()
    {
        var manager = SkillTreeManager.Instance;
        if (selected == null || manager == null) return;

        if (icon != null)
        {
            icon.sprite = selected.Icon;
            icon.enabled = selected.Icon != null;
        }

        if (accentStripe != null)
            accentStripe.color = selected.AccentColor;

        if (title != null) title.text = selected.Title;
        if (description != null) description.text = selected.Description;
        if (cost != null) cost.text = $"Цена: {selected.KillCost} убийств";

        // ← НОВОЕ: от кого наследуется
        if (parentText != null)
        {
            parentText.text = selected.Parent != null
                ? $"Наследуется от: {selected.Parent.Title}"
                : "Начальный навык (без родителя)";
        }

        bool unlocked = manager.IsUnlocked(selected);
        bool canUnlock = manager.CanUnlock(selected);

        if (unlockButton != null)
            unlockButton.interactable = !unlocked && canUnlock;

        if (buttonLabel != null)
        {
            if (unlocked) buttonLabel.text = "Получен";
            else if (canUnlock) buttonLabel.text = "Получить";
            else if (selected.StartingSkill) buttonLabel.text = "Стартовый";
            else buttonLabel.text = "Недоступен";
        }
    }

    public void OnUnlockPressed()
    {
        if (selected == null) return;
        if (SkillTreeManager.Instance != null &&
            SkillTreeManager.Instance.TryUnlock(selected))
            Refresh();
    }
}