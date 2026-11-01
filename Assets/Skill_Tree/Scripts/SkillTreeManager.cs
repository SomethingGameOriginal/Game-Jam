using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class SkillSaveData
{
    public int totalKills;
    public List<string> unlockedIds = new();
}

public class SkillTreeManager : MonoBehaviour
{
    public static SkillTreeManager Instance { get; private set; }

    [SerializeField] private List<SkillDefinition> skills = new();

    public event Action Changed;
    public IReadOnlyList<SkillDefinition> Skills => skills;
    public int TotalKills => saveData.totalKills;

    private SkillSaveData saveData = new();
    private string SavePath => Path.Combine(
        Application.persistentDataPath, "skills.json");

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();

        bool dirty = false;
        foreach (var skill in skills)
        {
            if (skill != null && skill.StartingSkill &&
                !saveData.unlockedIds.Contains(skill.Id))
            {
                saveData.unlockedIds.Add(skill.Id);
                dirty = true;
            }
        }

        if (dirty)
            Save();

        // Оповестим подписчиков на следующем кадре — к этому моменту
        // все UI уже подписались в OnEnable.
        StartCoroutine(NotifyNextFrame());
    }

    private System.Collections.IEnumerator NotifyNextFrame()
    {
        yield return null;
        Changed?.Invoke();
    }

    public bool IsUnlocked(SkillDefinition skill) =>
        skill != null && saveData.unlockedIds.Contains(skill.Id);

    public bool CanUnlock(SkillDefinition skill)
    {
        if (skill == null || IsUnlocked(skill) || skill.StartingSkill)
            return false;

        bool parentUnlocked =
            skill.Parent == null || IsUnlocked(skill.Parent);

        return parentUnlocked && saveData.totalKills >= skill.KillCost;
    }

    public bool TryUnlock(SkillDefinition skill)
    {
        if (!CanUnlock(skill))
            return false;

        saveData.totalKills -= skill.KillCost;
        saveData.unlockedIds.Add(skill.Id);

        Save();
        Changed?.Invoke();
        return true;
    }

    public void RegisterKill(int amount = 1)
    {
        if (amount <= 0)
            return;

        saveData.totalKills += amount;
        Save();
        Changed?.Invoke();
    }

    public void Save()
    {
        File.WriteAllText(SavePath, JsonUtility.ToJson(saveData));
    }

    private void Load()
    {
        if (!File.Exists(SavePath))
        {
            saveData = new SkillSaveData();
            return;
        }

        try
        {
            saveData = JsonUtility.FromJson<SkillSaveData>(
                File.ReadAllText(SavePath)) ?? new SkillSaveData();
            saveData.unlockedIds ??= new List<string>();
        }
        catch (Exception e)
        {
            Debug.LogError($"Не удалось загрузить навыки: {e.Message}");
            saveData = new SkillSaveData();
        }
    }

    public void ResetAll()
    {
        saveData.totalKills = 0;
        saveData.unlockedIds.Clear();

        foreach (var skill in skills)
        {
            if (skill != null && skill.StartingSkill)
                saveData.unlockedIds.Add(skill.Id);
        }

        Save();
        Changed?.Invoke();
        Debug.Log("[SkillTree] Навыки сброшены");
    }

    public float GetBonus(SkillBonusType type)
    {
        float total = 0f;
        foreach (var skill in skills)
        {
            if (skill != null && IsUnlocked(skill) && skill.BonusType == type)
                total += skill.BonusValue;
        }
        return total;
    }
}