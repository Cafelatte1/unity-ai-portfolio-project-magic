using UnityEngine;
using System.Collections.Generic;

public class ProjectileSkillLogic : SkillLogic
{
    [SerializeField] LayerMask layerGround;
    Rigidbody2D rb;
    Vector3 spawnPos;
    float direction;
    AnimatorStateInfo stateInfo;

    public override void Init(
        AttackContainer attack, Transform origin, float spawnYAdjust,
        LayerMask targetLayerMask, ObjectPool<SkillLogic> pool, HealthSystem targetHealthSystem, int instanceIndex = 0
        )
    {
        base.Init(attack, origin, spawnYAdjust, targetLayerMask, pool, targetHealthSystem);
        // adjsut position
        direction = -origin.localScale.x;
        spawnPos = origin.transform.position;
        spawnPos.x += direction * 0.5f;
        spawnPos.y += spawnYAdjust;
        transform.position = spawnPos;
        // sycn to rigidbody position
        rb.position = new Vector2(spawnPos.x, spawnPos.y);
        rb.linearVelocity = Vector2.zero;
        Logger.Write($"start skill logic / spawnPos={spawnPos}");
        stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        // State 길이 (normalizedTime이 0~1)
        float stateLength = stateInfo.length;
        // 현재 재생 시간 (0~1, Loop면 1 넘음)
        float normalizedTime = stateInfo.normalizedTime;
        // State 이름 해시
        int stateHash = stateInfo.shortNameHash;
        Logger.Write($"State Length: {stateLength}, Progress: {normalizedTime}, stateHash={stateHash}");
    }

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        if (!gameObject.activeSelf) return;
        if (Mathf.Abs(rb.position.x - spawnPos.x) > attack.range) Deactivate();
        MovePosition();
    }

    void MovePosition()
    {
        var delta = new Vector2(direction * attack.projectileSpeed * Time.fixedDeltaTime, 0f);
        rb.MovePosition(rb.position + delta);
    }

    protected override void Update()
    {
        if (!gameObject.activeSelf) return;
        base.Update();
    }

    public override void Deactivate()
    {
        Logger.Write("projectile skill deactivate");
        base.Deactivate();
        pool?.Return(this);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // (1 << other.gameObject.layer)은 비트 연산으로 layer번호가 1~N 까지 되있는걸 layerMask와 도메인을 맞추기 위해 2^N 으로 바꿈
        // 이렇게 변환한 후 하나라도 같은 값이 있으면 (!=0), 즉 둘 중 겹치는 비트가 있는지 확인하여 겹침을 확인
        Logger.Write($"triggered layer info / layer={other.gameObject.layer}, layerBit={1 << other.gameObject.layer}, targetLayerMask={targetLayerMask.value}, layerGround={layerGround.value}");
        if (((1 << other.gameObject.layer) & targetLayerMask) != 0 && other is CapsuleCollider2D)
        {
            if (targetHealthSystem == null)
            {
                if (other.TryGetComponent<HealthSystem>(out HealthSystem targetHealthSystem))
                {
                    if (!targetHealthSystem.IsAlive) return;
                    ApplyAttack(attack, origin.transform.position, other.transform.position, targetHealthSystem);
                    Logger.Write($"attack detected / origin={origin.name}, hit={other.name}, hitTag={other.tag}");
                }
                else
                {
                    Logger.Write("HealthSystem component not found", "WARNING");
                }
            }
            else
            {
                if (!targetHealthSystem.IsAlive) return;
                ApplyAttack(attack, origin.transform.position, other.transform.position, targetHealthSystem);
                Logger.Write($"attack detected / origin={origin.name}, hit={other.name}, hitTag={other.tag}");
            }
            IsTriggered = true;
            hitBox.enabled = false;
            
            Deactivate();
        }
        else if (((1 << other.gameObject.layer) & layerGround) != 0)
        {
            Logger.Write($"projectile hit wall");
            IsTriggered = true;
            hitBox.enabled = false;
            Deactivate();
        }
    }
}
