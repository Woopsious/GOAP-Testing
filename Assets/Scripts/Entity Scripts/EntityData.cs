using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EntityData", menuName = "ScriptableObjects/EntityData")]
public class EntityData: ScriptableObject
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

	[Header("Attack Behaviours")]
	public List<EntityAttackData> attackData = new List<EntityAttackData>();
}

[Serializable]
public class EntityAttackData
{
	public AttackType attackType;
	public enum AttackType
	{
		melee, ranged
	}

	public float attackDamage;
	public float attackCooldown;
	public float attackMinRange;
	public float attackMaxRange;
}
