using UnityEngine;

public class DamageBlocker : MonoBehaviour
{
    [Range(0f, 1f)] public float blockChance = 0f;

    // 由 PlayerStats.TakeDamage 调用
    public bool TryBlock()
    {
        return Random.value < blockChance;
    }
}