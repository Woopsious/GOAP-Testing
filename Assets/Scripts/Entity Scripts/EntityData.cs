using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EntityData", menuName = "ScriptableObjects/EntityData")]
public class EntityData : ScriptableObject
{
	[Header("Info")]
	public string Name;

	public EntityDataType type;
	public enum EntityDataType
	{
		player, greenWorker, greenDualist, greenMelee, greenRanged, redWorker, redDualist, redMelee, redRanged,
	}

	public EntityBrainType brainType;
	public enum EntityBrainType
	{
		combat, worker
	}

	public EntityTeam team;
	public enum EntityTeam
	{
		neutral, redTeam, greenTeam, playerTeam, debugAttackPlayer
	}

	[Header("Stats")]
	public float maxHealth;
	public float armour;
	public int resourceCost;

	[Header("NavMesh Settings")]
	public float speed;
	public float angularSpeed;
	public float acceleration;
	public float stoppingDistance;

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
