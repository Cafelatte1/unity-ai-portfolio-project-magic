using UnityEngine;

[CreateAssetMenu(fileName = "New Character Stats", menuName = "Game/Character Stats")]
public class CharacterStats : ScriptableObject
{
    [Header("Basic Info")]
    [SerializeField] public string characterName;
    [SerializeField] public Sprite characterIcon;

    [Header("Health")]
    [SerializeField] public float maxHealth;
    [SerializeField] public float healthRegen;

    [Header("Movement")]
    [SerializeField] public float moveSpeed;
    [SerializeField] public float jumpForce;
    [Header("Attack Type")]
    [SerializeField] public AttackType attackType;
    [SerializeField] public GameObject skillPrefab;
    [SerializeField] public float projectileSpeed;

    [Header("Attack")]
    [SerializeField] public float attackDamage;
    [SerializeField] public float attackCooldown;
    [SerializeField] public float attackRange;
    [SerializeField] public Vector2 attackKnockback;

    [Header("Skill")]
    [SerializeField] public SkillData[] skillDatas;

    [Header("Defense")]
    [SerializeField] public float defense;
    [SerializeField] public float damageReduction;
    [SerializeField] public float invincibilityDuration;
}
