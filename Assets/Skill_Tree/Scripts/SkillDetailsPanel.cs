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
    [SerializeField] private TMP_Text parentText;

    [Header("Цветовой акцент (опционально)")]
    [SerializeField] private Image accentStripe;

    [Header("Прокачка")]
    [SerializeField] private Button unlockButton;
    [SerializeField] private TMP_Text buttonLabel;

    [Header("Дебаг")]
    [SerializeField] private bool debug = true;

    private SkillDefinition selected;
    private bool subscribed;

    // =========================================================
    //  LIFECYCLE
    // =========================================================
    private void Awake()
    {
        Dbg("Awake");
        gameObject.SetActive(false);
    }

    private void Start()
    {
        // подписка после Awake — на случай, если Instance ещё не готов в OnEnable
        TrySubscribe();
    }

    private void OnEnable()
    {
        Dbg($"OnEnable (selected={selected?.Title})");
        TrySubscribe();
    }

    private void OnDisable()
    {
        Dbg("OnDisable");
        Unsubscribe();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private void TrySubscribe()
    {
        if (subscribed) return;

        if (SkillTreeManager.Instance != null)
        {
            SkillTreeManager.Instance.Changed += OnManagerChanged;
            subscribed = true;
            Dbg("Подписан на Changed");
        }
        else
        {
            Dbg("TrySubscribe: SkillTreeManager.Instance = null");
        }
    }

    private void Unsubscribe()
    {
        if (!subscribed) return;

        if (SkillTreeManager.Instance != null)
            SkillTreeManager.Instance.Changed -= OnManagerChanged;

        subscribed = false;
        Dbg("Отписан от Changed");
    }

    private void OnManagerChanged()
    {
        Dbg("OnManagerChanged");
        Refresh();
    }

    // =========================================================
    //  PUBLIC
    // =========================================================
    public void Show(SkillDefinition skill)
    {
        Dbg($"Show '{skill?.Title}'");

        selected = skill;
        gameObject.SetActive(true);
        TrySubscribe();
        Refresh();
    }

    public void Hide()
    {
        Dbg("Hide");
        selected = null;
        gameObject.SetActive(false);
    }

    // =========================================================
    //  REFRESH
    // =========================================================
    public void Refresh()
    {
        var manager = SkillTreeManager.Instance;

        Dbg($"Refresh: selected={selected?.Title}, manager={(manager != null)}");

        if (selected == null)
        {
            Dbg("Refresh: selected = null, выходим");
            return;
        }
        if (manager == null)
        {
            Debug.LogError("[DetailsPanel] Refresh: SkillTreeManager.Instance = null");
            return;
        }

        // --- визуал ---
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

        if (parentText != null)
        {
            parentText.text = selected.Parent != null
                ? $"Наследуется от: {selected.Parent.Title}"
                : "Начальный навык (без родителя)";
        }

        // --- состояние ---
        bool unlocked = manager.IsUnlocked(selected);
        bool canUnlock = manager.CanUnlock(selected);

        Dbg($"'{selected.Title}': unlocked={unlocked}, canUnlock={canUnlock}, " +
            $"kills={manager.TotalKills}, cost={selected.KillCost}, " +
            $"parent={selected.Parent?.Title ?? "NULL"}, " +
            $"starting={selected.StartingSkill}");

        if (unlockButton != null)
        {
            unlockButton.interactable = !unlocked && canUnlock;
            Dbg($"unlockButton.interactable = {unlockButton.interactable}");
        }
        else
        {
            Debug.LogError("[DetailsPanel] unlockButton = null (не назначен в инспекторе)");
        }

        if (buttonLabel != null)
        {
            if (unlocked) buttonLabel.text = "Получен";
            else if (canUnlock) buttonLabel.text = "Получить";
            else if (selected.StartingSkill) buttonLabel.text = "Стартовый";
            else buttonLabel.text = "Недоступен";
        }
        else
        {
            Debug.LogWarning("[DetailsPanel] buttonLabel = null");
        }
    }

    // =========================================================
    //  UNLOCK
    // =========================================================
    public void OnUnlockPressed()
    {
        Dbg($"OnUnlockPressed: selected={selected?.Title}, " +
            $"manager={(SkillTreeManager.Instance != null)}");

        if (selected == null)
        {
            Debug.LogWarning("[DetailsPanel] OnUnlockPressed: selected = null");
            return;
        }

        if (SkillTreeManager.Instance == null)
        {
            Debug.LogError("[DetailsPanel] OnUnlockPressed: SkillTreeManager.Instance = null");
            return;
        }

        bool canUnlock = SkillTreeManager.Instance.CanUnlock(selected);
        Dbg($"CanUnlock('{selected.Title}') = {canUnlock}");

        if (!canUnlock)
        {
            Debug.LogWarning($"[DetailsPanel] Нельзя купить '{selected.Title}': " +
                             $"kills={SkillTreeManager.Instance.TotalKills}, " +
                             $"cost={selected.KillCost}, " +
                             $"starting={selected.StartingSkill}, " +
                             $"unlocked={SkillTreeManager.Instance.IsUnlocked(selected)}");
            return;
        }

        if (SkillTreeManager.Instance.TryUnlock(selected))
        {
            Dbg($"УСПЕХ: '{selected.Title}' куплен");
            Refresh();
        }
        else
        {
            Debug.LogWarning($"[DetailsPanel] TryUnlock вернул false для '{selected.Title}'");
        }
    }

    // =========================================================
    //  DEBUG
    // =========================================================
    private void Dbg(string msg)
    {
        if (debug) Debug.Log($"[DetailsPanel] {msg}");
    }
}