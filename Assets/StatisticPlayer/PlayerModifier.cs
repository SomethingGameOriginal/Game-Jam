using UnityEngine;

public enum ModifierStat
{
    MaxHealth,
    InfectionIncrease,
    SecondsPerInfection
}


[CreateAssetMenu(menuName = "Game/Player Modifier")]
public class PlayerModifier : ScriptableObject
{
    public string modifierName;
    [TextArea] public string description;
    public Sprite icon;

    public ModifierStat stat;
    public float amount;
    public float duration;
}