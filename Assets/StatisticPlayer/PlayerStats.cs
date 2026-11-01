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

    private float currentMaxHealth;
    private float health;
    private float infection;
    private float infectionTimer;

    private void Start()
    {
        currentMaxHealth = maxHealth;
        health = currentMaxHealth;
        infection = 0f;
        UpdateUI();
    }

    private void Update()
    {
        if (infection >= 100f || secondsPerIncrease <= 0f)
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
        health = Mathf.Min(currentMaxHealth, health + amount);
        UpdateUI();
    }

    public void HealPoisonous(float amount)
    {
        infection = Mathf.Min(currentMaxHealth, infection - amount);
        UpdateUI();
    }

    public void ReduceInfection(float amount)
    {
        infection = Mathf.Max(0f, infection - amount);
        UpdateUI();
    }

    public void ModifyMaxHealth(float amount)
    {
        currentMaxHealth = Mathf.Max(1f, currentMaxHealth + amount);

        // Если максимум уменьшился ниже текущего здоровья, ограничиваем здоровье.
        health = Mathf.Min(health, currentMaxHealth);
        UpdateUI();
    }

    public void ModifyInfectionRate(float amount)
    {
        infectionIncrease = Mathf.Max(0f, infectionIncrease + amount);
    }

    public void ModifySecondsPerInfection(float amount)
    {
        secondsPerIncrease = Mathf.Max(0.1f, secondsPerIncrease + amount);
    }

    private void UpdateUI()
    {
        if (healthText != null)
            healthText.text =
                $"Здоровье: {Mathf.CeilToInt(health)} / {Mathf.CeilToInt(currentMaxHealth)}";

        if (infectionText != null)
            infectionText.text = $"Заражение: {Mathf.CeilToInt(infection)}%";

        if (healthBar != null)
            healthBar.fillAmount =
                currentMaxHealth > 0f ? health / currentMaxHealth : 0f;

        if (infectionBar != null)
            infectionBar.fillAmount = infection / 100f;
    }
}