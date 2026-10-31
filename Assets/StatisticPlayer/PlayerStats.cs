using UnityEngine;
using UnityEngine.UI;

public class PlayerStats : MonoBehaviour
{
    [Header("Здоровье")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private Text healthText;
    [SerializeField] private Image healthBar;

    [Header("Заражение")]
    [SerializeField] private float infectionIncrease = 1f;
    [SerializeField] private float secondsPerIncrease = 5f;
    [SerializeField] private Text infectionText;
    [SerializeField] private Image infectionBar;

    private float health;
    private float infection;
    private float infectionTimer;

    private void Start()
    {
        health = maxHealth;
        infection = 0f;
        UpdateUI();
    }

    private void Update()
    {
        if (infection >= 100f)
            return;

        infectionTimer += Time.deltaTime;

        while (infectionTimer >= secondsPerIncrease && infection < 100f)
        {
            infectionTimer -= secondsPerIncrease;
            infection = Mathf.Min(100f, infection + infectionIncrease);
            UpdateUI();
        }
    }

    public void TakeDamage(float damage)
    {
        health = Mathf.Max(0f, health - damage);
        UpdateUI();
    }

    public void Heal(float amount)
    {
        health = Mathf.Min(maxHealth, health + amount);
        UpdateUI();
    }

    public void ReduceInfection(float amount)
    {
        infection = Mathf.Max(0f, infection - amount);
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (healthText != null)
            healthText.text = $"Здоровье: {Mathf.CeilToInt(health)} / {Mathf.CeilToInt(maxHealth)}";

        if (infectionText != null)
            infectionText.text = $"Заражение: {Mathf.CeilToInt(infection)}%";

        if (healthBar != null)
            healthBar.fillAmount = maxHealth > 0f ? health / maxHealth : 0f;

        if (infectionBar != null)
            infectionBar.fillAmount = infection / 100f;
    }
}