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

    [Header("Дебаг")]
    [SerializeField] private bool debug = false;

    // runtime
    private float currentMaxHealth;
    private float health;
    private float infection;
    private float infectionTimer;

    // бонусы (кэш)
    private float bonusMaxHealth;
    private float bonusBlockChance;
    private float bonusInfectionSlow;

    public float Infection => infection;
    public bool IsAlive => health > 0f;
    public float Health => health;
    public float MaxHealth => currentMaxHealth;

    public bool Use;

    // =========================================================
    //  LIFECYCLE
    // =========================================================
    private void Awake()
    {
        if (playerDeath == null)
            playerDeath = GetComponent<PlayerDeath>();
    }

    private void Start()
    {
        if (SkillTreeManager.Instance != null)
            SkillTreeManager.Instance.Changed += OnSkillsChanged;

        RefreshBonuses();
        RecalculateMaxHealth();
        health = currentMaxHealth;
        infection = 0f;
        infectionTimer = 0f;

        UpdateUI();

        if (debug)
            Debug.Log($"[PlayerStats] Start: maxHealth={currentMaxHealth}, " +
                      $"blockChance={bonusBlockChance:P0}, " +
                      $"infectionSlow={bonusInfectionSlow:P0}");
    }

    private void OnDestroy()
    {
        if (SkillTreeManager.Instance != null)
            SkillTreeManager.Instance.Changed -= OnSkillsChanged;
    }

    private void OnSkillsChanged()
    {
        RefreshBonuses();
        RecalculateMaxHealth();
        UpdateUI();

        if (debug)
            Debug.Log($"[PlayerStats] Skills changed: maxHealth={currentMaxHealth}, " +
                      $"blockChance={bonusBlockChance:P0}, " +
                      $"infectionSlow={bonusInfectionSlow:P0}");
    }

    private void RefreshBonuses()
    {
        var mgr = SkillTreeManager.Instance;
        bonusMaxHealth = mgr != null ? mgr.GetBonus(SkillBonusType.MaxHealth) : 0f;
        bonusBlockChance = mgr != null ? mgr.GetBonus(SkillBonusType.DamageBlockChance) : 0f;
        bonusInfectionSlow = mgr != null ? mgr.GetBonus(SkillBonusType.InfectionSlow) : 0f;
    }

    private void RecalculateMaxHealth()
    {
        currentMaxHealth = Mathf.Max(1f, maxHealth + bonusMaxHealth);
        health = Mathf.Min(health, currentMaxHealth);
    }

    // =========================================================
    //  UPDATE
    // =========================================================
    private void Update()
    {
        if (infection >= 100f)
        {
            if (playerDeath != null && !playerDeath.IsDead)
                playerDeath.Die(DeathCause.Infection, $"{Mathf.CeilToInt(infection)}%");
            return;
        }

        if (secondsPerIncrease <= 0f)
            return;

        infectionTimer += Time.deltaTime;

        // множитель замедления: bonus=0.25 → 0.75 (на 25% медленнее)
        float infectionMultiplier = Mathf.Clamp(1f - bonusInfectionSlow, 0.05f, 5f);

        while (infectionTimer >= secondsPerIncrease && infection < 100f)
        {
            infectionTimer -= secondsPerIncrease;
            infection = Mathf.Min(100f,
                infection + infectionIncrease * infectionMultiplier);
            UpdateUI();
        }

        if (infection >= 50f)
        {
            CanvasTooltip.Show("Теперь вы можете применить способность из-за паразитов, нажав на C, вы ускорите заражение паразитами, но нанесете мощный удар");
        }

        if (infection >= 100f && playerDeath != null && !playerDeath.IsDead)
            playerDeath.Die(DeathCause.Infection, $"{Mathf.CeilToInt(infection)}%");
    }

    // =========================================================
    //  DAMAGE / HEAL
    // =========================================================
    public void TakeDamage(float damage)
    {
        if (damage <= 0f)
            return;

        // блок урона
        if (bonusBlockChance > 0f)
        {
            float chance = Mathf.Clamp01(bonusBlockChance);
            if (Random.value < chance)
            {
                if (debug)
                    Debug.Log($"[PlayerStats] Урон {damage} заблокирован ({chance:P0})");
                return;
            }
        }

        health = Mathf.Max(0f, health - damage);
        UpdateUI();

        if (debug)
            Debug.Log($"[PlayerStats] TakeDamage {damage}, hp={health}/{currentMaxHealth}");

        if (health <= 0f && playerDeath != null)
            playerDeath.Die(DeathCause.Damage, "пуля/удар");
    }

    public void Heal(float amount)
    {
        health = Mathf.Min(currentMaxHealth, health + amount);
        UpdateUI();
    }

    public void HealPoisonous(float amount)
    {
        infection = Mathf.Max(0f, infection - amount);
        UpdateUI();
    }

    public void ReduceInfection(float amount)
    {
        infection = Mathf.Max(0f, infection - amount);
        UpdateUI();
    }

    public void ModifyMaxHealth(float amount)
    {
        // меняем БАЗУ, а не текущий максимум — иначе бонус затрётся
        maxHealth = Mathf.Max(1f, maxHealth + amount);
        RecalculateMaxHealth();
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

    // =========================================================
    //  UI
    // =========================================================
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