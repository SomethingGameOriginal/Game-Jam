using System.Collections.Generic;
using UnityEngine;

public enum SkillEffectType
{
    MoveSpeed,
    MaxHealth,
    DamageBlock,
    BulletDamage
}

[CreateAssetMenu(menuName = "Game/Skill Node")]
public class SkillNodeData : ScriptableObject
{
    public string skillId;
    public string displayName;
    [TextArea] public string description;
    public int cost;
    public SkillEffectType effectType;
    public float value;
    public List<SkillNodeData> prerequisites = new List<SkillNodeData>();
    public Vector2 uiPosition; // 在树中的 UI 位置
}