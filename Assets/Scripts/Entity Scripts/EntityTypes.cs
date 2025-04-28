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

	public AttackType attackType;
	public enum AttackType
	{
		melee, ranged
	}

	public float basicAttackDamage;
	public float basicAttackCooldown;

	public float heavyAttackDamage;
	public float heavyAttackCooldown;
}
