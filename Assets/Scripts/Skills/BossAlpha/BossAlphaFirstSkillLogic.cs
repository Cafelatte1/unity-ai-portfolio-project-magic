using UnityEngine;
using System.Collections.Generic;

public class BossAlphaFirstSkillLogic : SkillLogic
{
    [SerializeField] LayerMask layerGround;
    [SerializeField] float curveOffset;
    Rigidbody2D rb;
    Vector3 spawnPos;
    Vector3 targetPos;
    float indicator;
    Vector3 controlPos;

    public override void Init(
        AttackContainer attack, Transform origin, float spawnYAdjust,
        LayerMask targetLayerMask, ObjectPool<SkillLogic> pool, HealthSystem targetHealthSystem, int instanceIndex = 0
        )
    {
        base.Init(attack, origin, spawnYAdjust, targetLayerMask, pool, targetHealthSystem, instanceIndex);
        indicator = 0f;
        spawnPos = origin.transform.position;
        targetPos = spawnPos + origin.up * attack.range;
        var mid = (spawnPos + targetPos) * 0.5f;
        var right = new Vector3(origin.right.x, origin.right.y, 0f);
        controlPos = mid - right * curveOffset;
        transform.position = spawnPos;
        Logger.Write($"start skill logic / spawnPos={spawnPos}");
    }

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        if (!gameObject.activeSelf) return;
        if (indicator >= 1f) Deactivate();
        MovePosition(indicator);
    }

    // Bezier formula (feat. ChatGPT)
    void MovePosition(float t)
    {
        t = Mathf.Clamp01(t);

        Vector3 p0 = spawnPos;
        Vector3 p1 = controlPos;
        Vector3 p2 = targetPos;

        float oneMinusT = 1f - t;

        // 2차 베지어
        Vector3 pos =
            oneMinusT * oneMinusT * p0 +
            2f * oneMinusT * t * p1 +
            t * t * p2;

        rb.MovePosition(pos);
    }

    protected override void Update()
    {
        if (!gameObject.activeSelf) return;
        indicator += Time.deltaTime * attack.projectileSpeed;
        base.Update();
    }

    public override void Deactivate()
    {
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
            Deactivate();
        }
    }

}
