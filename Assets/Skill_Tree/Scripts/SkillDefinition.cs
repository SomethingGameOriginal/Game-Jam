using UnityEngine;

public enum SkillBonusType
{
    MoveSpeed,
    MaxHealth,
    Damage,
    DamageBlockChance,
    InfectionSlow,      // ← НОВОЕ: замедление заражения (множитель)
    StartingTime        // ← НОВОЕ: +секунды к стартовому времени
}

[CreateAssetMenu(menuName = "Skills/Skill")]
public class SkillDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string id;
    [SerializeField] private string title;
    [TextArea(2, 5)]
    [SerializeField] private string description;
    [SerializeField] private Sprite icon;

    [Header("Appearance")]
    [SerializeField] private Color accentColor = Color.white;
    [SerializeField] private Sprite frame;

    [Header("Tree")]
    [SerializeField] private SkillDefinition parent;
    [SerializeField] private Vector2 treePosition;
    [Min(0)]
    [SerializeField] private int killCost = 1;
    [SerializeField] private bool startingSkill;

    [Header("Bonus")]
    [SerializeField] private SkillBonusType bonusType;
    [SerializeField] private float bonusValue;

    public string Id => id;
    public string Title => title;
    public string Description => description;
    public Sprite Icon => icon;
    public Color AccentColor => accentColor;
    public Sprite Frame => frame;
    public SkillDefinition Parent => parent;
    public Vector2 TreePosition => treePosition;
    public int KillCost => killCost;
    public bool StartingSkill => startingSkill;
    public SkillBonusType BonusType => bonusType;
    public float BonusValue => bonusValue;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(id))
            id = name;
    }
#endif
}