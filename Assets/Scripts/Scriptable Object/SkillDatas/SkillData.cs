using UnityEngine;

[CreateAssetMenu(fileName = "New Skill Data", menuName = "Game/Skill Data")]
public class SkillData : ScriptableObject
{
    [Header("Basic Info")]
    [SerializeField] public string skillName;
    [SerializeField] public Sprite skillIcon;

    [Header("Attack Type")]
    [SerializeField] public AttackType attackType;
    [SerializeField] public GameObject skillPrefab;
    [SerializeField] public float projectileSpeed;
    [SerializeField] public float AoeRadius;
    [SerializeField] public GameObject hitVFX;
    
    [Header("Attack")]
    [SerializeField] public float attackDamage;
    [SerializeField] public float attackCooldown;
    [SerializeField] public float attackRange;
    [SerializeField] public Vector2 attackKnockback;
}
