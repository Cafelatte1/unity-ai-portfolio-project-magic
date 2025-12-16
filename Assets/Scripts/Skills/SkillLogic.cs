using UnityEngine;

public class SkillLogic : MonoBehaviour
{
    [SerializeField] protected float MAX_LIFETIME;
    [SerializeField] [Range(0, 1)] protected float hitIntervalMin;
    [SerializeField] [Range(0, 1)] protected float hitIntervalMax;
    protected float lifeTimer;
    protected AttackContainer attack;
    protected Transform origin;
    protected float spawnYAdjust;
    protected LayerMask targetLayerMask;
    protected ObjectPool<SkillLogic> pool;
    protected HealthSystem targetHealthSystem;
    protected int instanceIndex;
    protected Animator animator;
    protected Collider2D hitBox;
    public bool IsTriggered { get; protected set; }
    bool _hitBoxInitState;

    public virtual void Init(
        AttackContainer attack, Transform origin, float spawnYAdjust, LayerMask targetLayerMask,
        ObjectPool<SkillLogic> pool, HealthSystem targetHealthSystem, int instanceIndex = 0
        )
    {
        lifeTimer = MAX_LIFETIME;
        this.attack = attack;
        this.origin = origin;
        this.spawnYAdjust = spawnYAdjust;
        this.targetLayerMask = targetLayerMask;
        this.pool = pool;
        this.targetHealthSystem = targetHealthSystem;
        this.instanceIndex = instanceIndex;
        gameObject.SetActive(true);
        hitBox.enabled = _hitBoxInitState;
        IsTriggered = false;
        Logger.Write("pool instance activated !");
    }

    protected virtual void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        hitBox = GetComponent<Collider2D>();
        _hitBoxInitState = hitBox.enabled;
    }

    protected virtual void Update()
    {
        if (lifeTimer > 0) {
            lifeTimer -= Time.deltaTime;
        }
        else {
            if (gameObject.activeSelf) Deactivate();
        }
    }

    protected virtual void ApplyAttack(AttackContainer attack, Vector3 originPos, Vector3 hitPoint, HealthSystem targetHealthSystem)
    {
        var knockback = CalculateKnockback(originPos, hitPoint, attack.knockback);
        targetHealthSystem.TakeDamage(attack.damage, knockback);
        Logger.Write("pool instance ApplyAttack");
    }

    protected Vector2 CalculateKnockback(Vector3 from, Vector3 to, Vector2 force)
    {
        Vector2 direction = new Vector2(Mathf.Sign(to.x - from.x), 1f);
        return direction * force;
    }

    public virtual void Deactivate()
    {
        IsTriggered = false;
        hitBox.enabled = false;
        gameObject.SetActive(false);
    }
}
