using UnityEngine;

[CreateAssetMenu(fileName = "EntityTypes", menuName = "ScriptableObjects/EntityTypes")]
public class EntityTypes : ScriptableObject
{
	[Header("Stats")]
	public EntityTeam team;
	public enum EntityTeam
	{
		redTeam, greenTeam, playerTeam, debugAttackPlayer
	}

	public float maxHealth;
	public float maxStamina;

	[Header("Sensor Ranges")]
	public float chaseRange;
	public float fleeRange;

	[Header("Stats")]
	public AttackType attackType;
	public enum AttackType
	{
		melee, ranged
	}

	public float basicAttackDamage;
	public float basicAttackCooldown;
	public float basicAttackRange;

	public float heavyAttackDamage;
	public float heavyAttackCooldown;
	public float heavyAttackRange;
}
