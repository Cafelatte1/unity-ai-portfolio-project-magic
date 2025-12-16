using UnityEngine;

public class EnemyAttack : AttackSystem
{
    [SerializeField] HealthSystem playerHealthSystem;

    protected override void Start()
    {
        base.Start();
        targetHealthSystem = playerHealthSystem;
    }
}