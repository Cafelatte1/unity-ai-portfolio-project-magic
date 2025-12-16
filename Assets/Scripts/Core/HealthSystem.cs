using UnityEngine;
using UnityEngine.Events;

public abstract class HealthSystem : MonoBehaviour
{
    
    protected CharacterStats stats;
    protected float currentHealth;
    protected float invincibleTimer;
    public float MaxHealth => stats.maxHealth;
    public float CurrentHealth => currentHealth;
    public float HealthPercentage => currentHealth / stats.maxHealth;
    public bool IsAlive => currentHealth > 0;
    // for UI
    public UnityEvent<float, float, float> OnHealthChanged;
    // for State
    public UnityEvent<Vector2> EventHit;
    public UnityEvent EventDeath;

    protected virtual void Awake()
    {
        stats = GetComponentInChildren<StatsContainer>().stats;
    }

    protected virtual void Start()
    {
        currentHealth = stats.maxHealth;
        OnHealthChanged?.Invoke(MaxHealth, 0f, currentHealth);
    }

    protected virtual void Update()
    {
        TickUpdate();
    }

    void TickUpdate()
    {
        if (invincibleTimer > 0) invincibleTimer -= Time.deltaTime;
    }

    public virtual void TakeDamage(float rawDamage, Vector2 knockbackDirection = default)
    {
        if (invincibleTimer > 0 || currentHealth <= 0) return;

        // 데미지 계산
        float finalDamage = CalculateDamage(rawDamage);
        var beforeHealth = currentHealth;
        currentHealth = Mathf.Max(0, currentHealth - finalDamage);
        OnHealthChanged?.Invoke(MaxHealth, beforeHealth, currentHealth);
        EventHit?.Invoke(knockbackDirection);
        Logger.Write($"Take damaged / finalDamage={finalDamage}, currentHealth={currentHealth}, knockbackDirection={knockbackDirection}");
        
        if (currentHealth <= 0)
            EventDeath?.Invoke();
    }

    protected virtual float CalculateDamage(float rawDamage)
    {
        float damageAfterDefense = Mathf.Max(1f, rawDamage - stats.defense);
        float finalDamage = damageAfterDefense * (1f - stats.damageReduction);
        return Mathf.Max(1, finalDamage);
    }


}