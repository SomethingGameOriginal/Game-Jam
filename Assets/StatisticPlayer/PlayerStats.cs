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

    [Header("Смерть")]
    [SerializeField] private PlayerDeath playerDeath;

    private float currentMaxHealth;
    private float health;
    private float infection;
    private float infectionTimer;

    // Добавить в PlayerStats
    public float Infection => infection;
    public bool IsAlive => health > 0f;

    private void Start()
    {
        if (playerDeath == null)
            playerDeath = GetComponent<PlayerDeath>();

        RecalculateMaxHealth();
        health = currentMaxHealth;

        // подписка на изменения навыков
        if (SkillTreeManager.Instance != null)
            SkillTreeManager.Instance.Changed += OnSkillsChanged;

        UpdateUI();
    }

    private void OnDestroy()
    {
        if (SkillTreeManager.Instance != null)
            SkillTreeManager.Instance.Changed -= OnSkillsChanged;
    }

    private void OnSkillsChanged()
    {
        RecalculateMaxHealth();
        UpdateUI();
    }

    private void RecalculateMaxHealth()
    {
        float bonus = 0f;
        if (SkillTreeManager.Instance != null)
            bonus = SkillTreeManager.Instance.GetBonus(SkillBonusType.MaxHealth);

        currentMaxHealth = Mathf.Max(1f, maxHealth + bonus);
        health = Mathf.Min(health, currentMaxHealth);
    }

    private void Update()
    {
        // Проверка заражения на 100
        if (infection >= 100f)
        {
            if (playerDeath != null) playerDeath.Die();
            return;
        }

        if (secondsPerIncrease <= 0f)
            return;

        infectionTimer += Time.deltaTime;

        while (infectionTimer >= secondsPerIncrease && infection < 100f)
        {
            infectionTimer -= secondsPerIncrease;
            infection = Mathf.Min(100f, infection + infectionIncrease);
            UpdateUI();
        }

        if (infection >= 100f && playerDeath != null)
            playerDeath.Die();
    }

    public void TakeDamage(float damage)
    {
       // var blocker = GetComponent<DamageBlocker>();
        

        health = Mathf.Max(0f, health - damage);
        UpdateUI();
        if (health <= 0f && playerDeath != null)
            playerDeath.Die();
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