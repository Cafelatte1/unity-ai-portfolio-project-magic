using UnityEngine;
using System.Collections.Generic;

public class AoeSkillLogic : SkillLogic
{
    Vector3 spawnPos;
    AnimatorStateInfo stateInfo;

    public override void Init(
        AttackContainer attack, Transform origin, float spawnYAdjust,
        LayerMask targetLayerMask, ObjectPool<SkillLogic> pool, HealthSystem targetHealthSystem, int instanceIndex = 0
        )
    {
        base.Init(attack, origin, spawnYAdjust, targetLayerMask, pool, targetHealthSystem);
        spawnPos = DetectTargetInRange();
        transform.position = spawnPos;
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

    protected override void Update()
    {
        if (!gameObject.activeSelf) return;
        stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.normalizedTime >= hitIntervalMax) { Deactivate(); }
        else if (stateInfo.normalizedTime >= hitIntervalMin) { if (!hitBox.enabled && !IsTriggered) hitBox.enabled = true; }
        else { }
        base.Update();
    }

    Vector3 DetectTargetInRange()
    {
        Vector2 direction = new Vector2(-origin.localScale.x, 0f);
        var pos = origin.position;
        pos.y += spawnYAdjust;
        RaycastHit2D hit = Physics2D.CircleCast(
            origin.position, 
            0.2f,
            direction,
            attack.range,
            targetLayerMask
        );
        if (hit)
        {
            pos.x = hit.collider.transform.position.x;
            Logger.Write($"detect in range for Aoe attack / layer={hit.collider.gameObject.layer}");
            return pos;
        }
        else
        {
            pos.x += direction.x * attack.range;
            return pos;
        }
    }

    public override void Deactivate()
    {
        Logger.Write("aoe skill deactivate");
        base.Deactivate();
        pool?.Return(this);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // (1 << other.gameObject.layer)은 비트 연산으로 layer번호가 1~N 까지 되있는걸 layerMask와 도메인을 맞추기 위해 2^N 으로 바꿈
        // 이렇게 변환한 후 하나라도 같은 값이 있으면 (!=0), 즉 둘 중 겹치는 비트가 있는지 확인하여 겹침을 확인
        Logger.Write($"triggered layer info / layer={other.gameObject.layer}, layerBit={1 << other.gameObject.layer}, targetLayerMask={targetLayerMask}");
        if (((1 << other.gameObject.layer) & targetLayerMask) != 0 && other is CapsuleCollider2D)
        {
            if (targetHealthSystem == null)
            {
                if (other.TryGetComponent<HealthSystem>(out HealthSystem targetHealthSystem))
                {
                    if (!targetHealthSystem.IsAlive) return;
                    ApplyAttack(attack, origin.transform.position, other.transform.position, targetHealthSystem);
                    Logger.Write($"attack detected / instance={this.name}, origin={origin.name}, hit={other.name}, hitTag={other.tag}");
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
                Logger.Write($"attack detected / instance={this.name}, origin={origin.name}, hit={other.name}, hitTag={other.tag}");
            }
            IsTriggered = true;
            hitBox.enabled = false;
        }
    }
}
