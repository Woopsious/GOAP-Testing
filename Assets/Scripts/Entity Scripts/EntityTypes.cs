using UnityEngine;

[CreateAssetMenu(fileName = "EntityTypes", menuName = "ScriptableObjects/EntityTypes")]
public class EntityTypes : ScriptableObject
{
	[Header("Info")]
	public string Name;
	public EntityTeam team;
	public enum EntityTeam
	{
		redTeam, greenTeam, playerTeam, debugAttackPlayer
	}

	[Header("Stats")]
	public float maxHealth;

	[Header("Sensor Ranges")]
	public float chaseRange;
	public float fleeRange;

	[Header("Attack One")]
	public AttackType attackTypeOne;
	public float attackOneDamage;
	public float attackOneCooldown;
	public float attackOneMinRange;
	public float attackOneMaxRange;

	[Header("Attack Two")]
	public AttackType attackTypeTwo;
	public float attackTwoDamage;
	public float attackTwoCooldown;
	public float attackTwoMinRange;
	public float attackTwoMaxRange;

	public enum AttackType
	{
		melee, ranged
	}
}
