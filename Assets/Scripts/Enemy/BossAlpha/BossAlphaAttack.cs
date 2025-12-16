using System.Collections.Generic;
using UnityEngine;

public class BossAlphaAttack : AttackSystem
{
    [SerializeField] int numInstances;
    BossAlphaFirstContainer container;

    protected override void Awake()
    {
        base.Awake();
        container = GetComponentInChildren<BossAlphaFirstContainer>();
    }

    protected override void ExecuteCustomAttack(
        int attackIndex, AttackContainer attack, Transform origin, float spawnYPosAdjust,
        LayerMask targetLayerMask, ObjectPool<SkillLogic> pool, HealthSystem targetHealthSystem = null
        )
    {
        switch (attackIndex)
        {
            case 1:
                    if (container.IsSpawning)
                    {
                        Logger.Write("[BossAlphaFirstContainer] Spawn coroutine already running, skip.");
                        return;
                    }
                    var instances = pool.Get(numInstances);
                    container.ProcureInstances(instances, attack, origin, spawnYPosAdjust, targetLayerMask, pool, targetHealthSystem);
                break;
            default:
                break;
        }
    }
}
