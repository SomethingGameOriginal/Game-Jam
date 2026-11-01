using System.Collections.Generic;
using UnityEngine;
using System.IO;

[System.Serializable]
public class SkillSaveData
{
    public int coins;
    public List<string> unlockedSkillIds = new List<string>();
    public int totalKills;
}

public class SkillTreeManager : MonoBehaviour
{
    public static SkillTreeManager Instance { get; private set; }

    [Header("Skill Definitions (assign in Inspector)")]
    public List<SkillNodeData> allSkills = new List<SkillNodeData>();

    public SkillSaveData SaveData { get; private set; } = new SkillSaveData();

    private string SavePath => Path.Combine(Application.persistentDataPath, "skilltree_save.json");

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
    }

    public bool IsUnlocked(string skillId) => SaveData.unlockedSkillIds.Contains(skillId);

    public bool CanUnlock(SkillNodeData skill)
    {
        if (IsUnlocked(skill.skillId)) return false;
        if (SaveData.coins < skill.cost) return false;
        // 前置技能检查
        foreach (var pre in skill.prerequisites)
        {
            if (!IsUnlocked(pre.skillId)) return false;
        }
        return true;
    }

    public bool TryUnlock(SkillNodeData skill)
    {
        if (!CanUnlock(skill)) return false;

        SaveData.coins -= skill.cost;
        SaveData.unlockedSkillIds.Add(skill.skillId);

        ApplySkillEffect(skill);
        Save();
        return true;
    }

    public void AddKill(int amount = 1)
    {
        SaveData.totalKills += amount;
        SaveData.coins += amount;
        Save();
    }

    private void ApplySkillEffect(SkillNodeData skill)
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        switch (skill.effectType)
        {
            case SkillEffectType.MoveSpeed:
                var pc = player.GetComponent<PlayerController>();
                if (pc != null) pc.speed += skill.value;
                break;

            case SkillEffectType.MaxHealth:
                var ps = player.GetComponent<PlayerStats>();
                if (ps != null) ps.ModifyMaxHealth(skill.value);
                break;

            case SkillEffectType.DamageBlock:
                var block = player.GetComponent<DamageBlocker>();
                if (block != null) block.blockChance += skill.value;
                break;

            case SkillEffectType.BulletDamage:
                var bulletPrefabSetter = player.GetComponent<BulletDamageBonus>();
                if (bulletPrefabSetter != null) bulletPrefabSetter.bonusDamage += skill.value;
                break;
        }
    }

    public void Save()
    {
        string json = JsonUtility.ToJson(SaveData, true);
        File.WriteAllText(SavePath, json);
        Debug.Log("SkillTree saved to " + SavePath);
    }

    public void Load()
    {
        if (File.Exists(SavePath))
        {
            string json = File.ReadAllText(SavePath);
            SaveData = JsonUtility.FromJson<SkillSaveData>(json);
        }
        else
        {
            SaveData = new SkillSaveData();
            SaveData.coins = 0;
        }
    }
}