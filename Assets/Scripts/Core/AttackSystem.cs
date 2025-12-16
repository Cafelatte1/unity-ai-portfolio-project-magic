using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEditor;

public enum AttackType
{
    Melee,
    Projectile,
    Aoe,
}

public class AttackContainer
{
    public float damage { get; private set; }
    public float cooldown { get; private set; }
    public float cooldownTimer { get; set; }
    public float range { get; private set; }
    public Vector2 knockback { get; private set; }
    public AttackType attackType { get; private set; }
    public GameObject skillPrefab { get; private set; }
    public float projectileSpeed { get; private set; }

    public AttackContainer(
        float damage, 
        float cooldown, 
        float range, 
        Vector2 knockback,
        AttackType attackType,
        GameObject skillPrefab,
        float projectileSpeed
    )
    {
        this.damage = damage;
        this.cooldown = cooldown;
        this.range = range;
        this.knockback = knockback;
        this.attackType = attackType;
        this.skillPrefab = skillPrefab;
        this.projectileSpeed = projectileSpeed;
    }

    public void UpdateTimer(float deltaTime)
    {
        if (cooldownTimer > 0)
            cooldownTimer -= deltaTime;
    }

    public bool CheckTimer()
    {
        return cooldownTimer <= 0;
    }

    public void RefillTimer()
    {
        cooldownTimer = cooldown;
    }
}

public abstract class AttackSystem : MonoBehaviour
{
    [SerializeField] float YPosAdjust;
    [SerializeField] float hitAreaRadius;
    [SerializeField] int ObjectPoolSize;
    protected CharacterStats stats;
    protected HealthSystem targetHealthSystem;
    protected List<AttackContainer> attackContainer;
    public UnityEvent<int> EventAttack;
    Dictionary<int, ObjectPool<SkillLogic>> poolContainer;

    protected virtual void Awake()
    {
        stats = GetComponentInChildren<StatsContainer>().stats;
        attackContainer = new List<AttackContainer>()
        {
            new AttackContainer(
                stats.attackDamage,
                stats.attackCooldown,
                stats.attackRange,
                stats.attackKnockback,
                stats.attackType,
                stats.skillPrefab,
                stats.projectileSpeed
            )
        };
        for(int i = 0; i < stats.skillDatas.Length; i++)
        {
            var skill = stats.skillDatas[i];
            attackContainer.Add(new AttackContainer(
                skill.attackDamage,
                skill.attackCooldown,
                skill.attackRange,
                skill.attackKnockback,
                skill.attackType,
                skill.skillPrefab,
                skill.projectileSpeed   
            ));
        }
    }

    protected virtual void Start()
    {
        poolContainer = new Dictionary<int, ObjectPool<SkillLogic>>();
        var goContainer = ContainerController.Instance.GetContainer(ContainerName.VFX.ToString());
        for (int i=0; i<attackContainer.Count; i++)
        {
            if (attackContainer[i].attackType == AttackType.Melee) continue;

            var skillLogic = attackContainer[i].skillPrefab?.GetComponent<SkillLogic>();
            if (skillLogic == null)
            {
                Logger.Write($"skill logic not found / attackType={attackContainer[i].attackType}, skillLogic={skillLogic}");
                continue;
            }
            poolContainer[i] = new ObjectPool<SkillLogic>(skillLogic, ObjectPoolSize, goContainer);
        }    
    }

    protected virtual void Update()
    {
        // 모든 공격의 쿨다운 업데이트
        foreach (var attack in attackContainer)
        {
            attack.UpdateTimer(Time.deltaTime);
        }
    }

    public virtual bool ExecuteAttack(
        int attackIndex,
        Transform origin,
        LayerMask targetLayerMask)
    {
        // 유효성 검사
        if (attackIndex >= attackContainer.Count)
        {
            Logger.Write($"Invalid attack index: {attackIndex}", "ERROR");
            return false;
        }
        var attack = attackContainer[attackIndex];

        // 쿨다운 체크
        if (!attack.CheckTimer())
        {
            Logger.Write($"Attack on cooldown: {attack.cooldownTimer:F2}s remaining");
            return false;
        }

        // attack type routing
        switch (attack.attackType)
        {
            case AttackType.Melee:
                ExecuteMeleeAttack(attackIndex, attack, origin, YPosAdjust, targetLayerMask);
                break;
            default:
                ExecuteCustomAttack(attackIndex, attack, origin, YPosAdjust, targetLayerMask, poolContainer[attackIndex]);
                break;
        }

        EventAttack?.Invoke(attackIndex);
        attack.RefillTimer();
        return true;
    }

    protected virtual void ExecuteMeleeAttack(int attackIndex, AttackContainer attack, Transform origin, float spawnYPosAdjust, LayerMask targetLayerMask)
    {
        var direction = new Vector2(-origin.localScale.x, 0f);
        var attackBox = origin.position;
        attackBox.y += spawnYPosAdjust;
        attackBox.x += direction.x * attack.range;
        Collider2D hit = Physics2D.OverlapCircle(
            attackBox,
            hitAreaRadius,
            targetLayerMask
        );
        if (hit != null)
        {
            if (targetHealthSystem == null)
            {
                if (hit.TryGetComponent<HealthSystem>(out HealthSystem targetHealthSystem))
                {
                    ApplyAttack(attack, origin.transform.position, hit.transform.position, targetHealthSystem);
                    Logger.Write($"attack detected / origin={origin.name}, hit={hit.name}, hitTag={hit.tag}");
                }
                else
                {
                    Logger.Write("HealthSystem component not found", "WARNING");
                }
            }
            else
            {
                ApplyAttack(attack, origin.transform.position, hit.transform.position, targetHealthSystem);
                Logger.Write($"attack detected / origin={origin.name}, hit={hit.name}, hitTag={hit.tag}");
            }
        }
    }

    protected virtual void ExecuteCustomAttack(
        int attackIndex, AttackContainer attack, Transform origin, float spawnYPosAdjust,
        LayerMask targetLayerMask, ObjectPool<SkillLogic> pool, HealthSystem targetHealthSystem = null
        )
    {
        var instance = pool.Get();
        instance.Init(attack, origin, spawnYPosAdjust, targetLayerMask, pool, targetHealthSystem);
    }

    protected void ApplyAttack(AttackContainer attack, Vector3 originPos, Vector3 hitPoint, HealthSystem targetHealthSystem)
    {
        if (targetHealthSystem == null) return;
        
        // 공격자와 피격자가 서있는 위치 기준으로 넉백 계산
        var knockback = CalculateKnockback(originPos, hitPoint, attack.knockback);
        targetHealthSystem.TakeDamage(attack.damage, knockback);
    }

    protected Vector2 CalculateKnockback(Vector3 from, Vector3 to, Vector2 force)
    {
        Vector2 direction = new Vector2(Mathf.Sign(to.x - from.x), 1f);
        return direction * force;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        float direction = -Mathf.Sign(transform.localScale.x);
        float attackRange = 0.8f;
        Vector3 pos = transform.position;
        pos.y += YPosAdjust;
        pos.x += direction * attackRange;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(pos, hitAreaRadius);
    }
#endif
}
