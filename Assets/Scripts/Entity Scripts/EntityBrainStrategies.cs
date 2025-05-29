using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using static InteractStrategy;

public interface IEntityBrainStrategies
{
	Dictionary<string, EntityBeliefs> SetupBeliefs();
	HashSet<EntityActions> SetupActions();
	HashSet<EntityGoals> SetupGoals();
}

public class CombatBrainStrategy : IEntityBrainStrategies
{
	readonly EntityBrain entityBrain;
	readonly EntityStats entityStats;
	readonly NavMeshAgent navMeshAgent;

	readonly EntitySensor[] entitySensors;
	readonly Transform[] knownLocations;
	readonly Func<bool>[] attackBools;

	public Dictionary<string, EntityBeliefs> beliefs;
	public HashSet<EntityActions> actions;
	public HashSet<EntityGoals> goals;

	public CombatBrainStrategy(EntityBrain entityBrain, EntityStats entityStats, NavMeshAgent navMeshAgent,
		EntitySensor[] entitySensors, Transform[] knownLocations, Func<bool>[] attackBools)
	{
		this.entityBrain = entityBrain;
		this.entityStats = entityStats;
		this.navMeshAgent = navMeshAgent;

		this.entitySensors = entitySensors;
		this.knownLocations = knownLocations;
		this.attackBools = attackBools;
	}

	public Dictionary<string, EntityBeliefs> SetupBeliefs()
	{
		beliefs = new Dictionary<string, EntityBeliefs>();
		BeliefFactory factory = new(entityBrain, beliefs);

		factory.AddBelief("Nothing", () => false);

		factory.AddBelief("AgentIdle", () => !navMeshAgent.hasPath);
		factory.AddBelief("AgentMoving", () => navMeshAgent.hasPath);
		factory.AddBelief("AgentHealthLow", () => entityStats.currentHealth < 40);
		factory.AddBelief("AgentIsHealthy", () => entityStats.currentHealth >= 60);

		factory.AddTargetBelief("TargetInFleeRange", entitySensors[0]);
		factory.AddBelief("FleeingFromTarget", () => false);

		factory.AddTargetBelief("TargetInChaseRange", entitySensors[1]);
		factory.AddBelief("ChasingTarget", () => false);

		factory.AddBelief("TargetInAttackOneRange", () => entitySensors[2].target.obj != null);
		factory.AddBelief("TargetInAttackTwoRange", () => entitySensors[3].target.obj != null);

		factory.AddBelief("AllAttacksOnCooldown", () => attackBools[0]());
		factory.AddBelief("AttackOneReady", () => attackBools[1]());
		factory.AddBelief("AttackTwoReady", () => attackBools[2]());

		factory.AddBelief("MoveToAttack", () => false);
		factory.AddBelief("Attack", () => false);

		return beliefs;
	}
	public HashSet<EntityActions> SetupActions()
	{
		actions = new HashSet<EntityActions>
		{
			new EntityActions.Builder("Relax")
			.WithStrategy(new IdleStrategy(5))
			.AddEffect(beliefs["Nothing"])
			.Build(),

			new EntityActions.Builder("Wander Around")
			.WithStrategy(new WanderStrategy(navMeshAgent, 20))
			.AddEffect(beliefs["AgentMoving"])
			.Build(),

			new EntityActions.Builder("FleeFromTarget")
			.WithStrategy(new FleeStrategy(navMeshAgent, () => beliefs["TargetInFleeRange"].TargetLocation))
			.AddPrecondition(beliefs["TargetInFleeRange"])
			.AddEffect(beliefs["FleeingFromTarget"])
			.Build(),

			new EntityActions.Builder("ChaseTarget")
			.WithStrategy(new MoveStrategy(navMeshAgent, 3, () => beliefs["TargetInChaseRange"].TargetLocation))
			.AddPrecondition(beliefs["TargetInChaseRange"])
			.AddEffect(beliefs["ChasingTarget"])
			.Build(),

			new EntityActions.Builder("MoveToUseAttackOne")
			.WithStrategy(new MoveIntoAttackRange(navMeshAgent, () => beliefs["TargetInChaseRange"].TargetLocation, entityStats._Data.attackData[0]))
			.AddPrecondition(beliefs["TargetInChaseRange"])
			.AddPrecondition(beliefs["AttackOneReady"])
			.AddEffect(beliefs["MoveToAttack"])
			.Build(),

			new EntityActions.Builder("MoveToUseAttackTwo")
			.WithStrategy(new MoveIntoAttackRange(navMeshAgent, () => beliefs["TargetInChaseRange"].TargetLocation, entityStats._Data.attackData[1]))
			.AddPrecondition(beliefs["TargetInChaseRange"])
			.AddPrecondition(beliefs["AttackTwoReady"])
			.AddEffect(beliefs["MoveToAttack"])
			.Build(),

			new EntityActions.Builder("WaitForAttacks")
			.WithStrategy(new IdleStrategy(5f))
			.AddPrecondition(beliefs["AllAttacksOnCooldown"])
			.AddEffect(beliefs["Attack"])
			.Build(),

			new EntityActions.Builder("AttackOne")
			.WithStrategy(new BasicAttackStrategy(entityBrain, 1))
			.AddPrecondition(beliefs["TargetInAttackOneRange"])
			.AddPrecondition(beliefs["AttackOneReady"])
			.AddEffect(beliefs["Attack"])
			.Build(),

			new EntityActions.Builder("AttackTwo")
			.WithStrategy(new BasicAttackStrategy(entityBrain, 2))
			.AddPrecondition(beliefs["TargetInAttackTwoRange"])
			.AddPrecondition(beliefs["AttackTwoReady"])
			.AddEffect(beliefs["Attack"])
			.Build()
		};

		return actions;
	}
	public HashSet<EntityGoals> SetupGoals()
	{
		goals = new HashSet<EntityGoals>
		{
			new EntityGoals.Builder("Idle")
			.WithPriority(5)
			.WithDesiredEffect(beliefs["Nothing"])
			.Build(),

			new EntityGoals.Builder("Wander")
			.WithPriority(10)
			.WithDesiredEffect(beliefs["AgentMoving"])
			.Build(),

			new EntityGoals.Builder("KeepHealthUp")
			.WithPriority(20)
			.WithDesiredEffect(beliefs["AgentIsHealthy"])
			.Build(),

			new EntityGoals.Builder("ChaseTarget")
			.WithPriority(50)
			.WithDesiredEffect(beliefs["ChasingTarget"])
			.Build(),

			new EntityGoals.Builder("MoveToUseAttack")
			.WithPriority(60)
			.WithDesiredEffect(beliefs["MoveToAttack"])
			.Build(),

			new EntityGoals.Builder("WaitForAttacks")
			.WithPriority(80)
			.WithDesiredEffect(beliefs["Attack"])
			.Build(),

			new EntityGoals.Builder("Attack")
			.WithPriority(90)
			.WithDesiredEffect(beliefs["Attack"])
			.Build(),

			new EntityGoals.Builder("FleeFromTarget")
			.WithPriority(40)
			.WithDesiredEffect(beliefs["FleeingFromTarget"])
			.Build(),
		};

		return goals;
	}
}

public class WorkerBrainStrategy : IEntityBrainStrategies
{
	readonly EntityBrain entityBrain;
	readonly EntityStats entityStats;
	readonly NavMeshAgent navMeshAgent;

	readonly EntitySensor[] entitySensors;
	readonly Transform[] knownLocations;

	public Dictionary<string, EntityBeliefs> beliefs;
	public HashSet<EntityActions> actions;
	public HashSet<EntityGoals> goals;

	public WorkerBrainStrategy(EntityBrain entityBrain, EntityStats entityStats, NavMeshAgent navMeshAgent,
		EntitySensor[] entitySensors, Transform[] knownLocations)
	{
		this.entityBrain = entityBrain;
		this.entityStats = entityStats;
		this.navMeshAgent = navMeshAgent;

		this.entitySensors = entitySensors;
		this.knownLocations = knownLocations;
	}

	public Dictionary<string, EntityBeliefs> SetupBeliefs()
	{
		beliefs = new Dictionary<string, EntityBeliefs>();
		BeliefFactory factory = new(entityBrain, beliefs);

		factory.AddBelief("Nothing", () => false);

		factory.AddBelief("AgentIdle", () => !navMeshAgent.hasPath);
		factory.AddBelief("AgentMoving", () => navMeshAgent.hasPath);
		factory.AddBelief("AgentHealthLow", () => entityStats.currentHealth < 30);
		factory.AddBelief("AgentIsHealthy", () => entityStats.currentHealth >= 40);

		factory.AddTargetBelief("TargetInFleeRange", entitySensors[0]);
		factory.AddBelief("FleeingFromTarget", () => false);

		if (entityStats._Data.team == EntityData.EntityTeam.redTeam)
			factory.AddLocationBelief("AtHomeBase", 10f, GameManager.instance.redTeamHomeBase.transform.position);
        else if (entityStats._Data.team == EntityData.EntityTeam.greenTeam)
			factory.AddLocationBelief("AtHomeBase", 10f, GameManager.instance.greenTeamHomeBase.transform.position);

		factory.AddTargetBelief("FoundFriendlyPoi", entitySensors[2]);
		factory.AddBelief("AtFriendlyPoi", () => entityBrain.InRangeOf(beliefs["FoundFriendlyPoi"].TargetLocation, 10f));
		//factory.AddLocationBelief("AtFriendlyPoi", 15f, beliefs["FoundFriendlyPoi"].TargetLocation);

		factory.AddBelief("FriendlyPoiNeedsResTransfer", () =>
			beliefs["FoundFriendlyPoi"].TargetData.poi != null &&
			beliefs["FoundFriendlyPoi"].TargetData.poi.PoiNeedsResourceTransfer());
		factory.AddBelief("TransferResourceToHomeBase", () => entityStats.carriedResources != 0);

		factory.AddTargetBelief("FoundEnemyPoi", entitySensors[3]);
		factory.AddBelief("AtEnemyPoi", () => entityBrain.InRangeOf(beliefs["FoundEnemyPoi"].TargetLocation, 10f));
		factory.AddBelief("EnemyPoiCapturable", () => 
			beliefs["FoundEnemyPoi"].TargetData.poi != null &&
			beliefs["FoundEnemyPoi"].TargetData.poi.PoiCapturable(entityStats));
		//factory.AddLocationBelief("AtCapturablePoi", 15f, beliefs["FoundCapturablePoi"].TargetLocation);

		factory.AddBelief("FindPois", () => false);
		factory.AddBelief("CapturePoi", () => false);
		factory.AddBelief("PickUpResources", () => false);
		factory.AddBelief("DropOffResources", () => false);

		return beliefs;
	}
	public HashSet<EntityActions> SetupActions()
	{
		actions = new HashSet<EntityActions>
		{
			new EntityActions.Builder("Relax")
			.WithStrategy(new IdleStrategy(5))
			.AddEffect(beliefs["Nothing"])
			.Build(),

			new EntityActions.Builder("FleeFromEnemy")
			.WithStrategy(new FleeStrategy(navMeshAgent, () => beliefs["TargetInFleeRange"].TargetLocation))
			.AddPrecondition(beliefs["TargetInFleeRange"])
			.AddEffect(beliefs["FleeingFromTarget"])
			.Build(),

			new EntityActions.Builder("FindPois")
			.WithStrategy(new FindPoiStrategy(entityBrain, navMeshAgent, 50))
			.AddEffect(beliefs["AgentMoving"])
			.AddEffect(beliefs["FindPois"])
			.Build(),

			new EntityActions.Builder("MoveToHealAtFriendlyPoi")
			.WithStrategy(new MoveStrategy(navMeshAgent, 3f, () => beliefs["FoundFriendlyPoi"].TargetLocation))
			.AddPrecondition(beliefs["AgentHealthLow"])
			.AddPrecondition(beliefs["FoundFriendlyPoi"])
			.AddEffect(beliefs["AtFriendlyPoi"])
			.Build(),

			new EntityActions.Builder("HealAtFriendlyPoi")
			.WithStrategy(new InteractStrategy(entityBrain, new HealAtPoiInteract(beliefs["FoundFriendlyPoi"])))
			.AddPrecondition(beliefs["AgentHealthLow"])
			.AddPrecondition(beliefs["AtFriendlyPoi"])
			.AddEffect(beliefs["AgentIsHealthy"])
			.Build(),

			new EntityActions.Builder("MoveToPickUpResources")
			.WithStrategy(new MoveStrategy(navMeshAgent, 3f, () => beliefs["FoundFriendlyPoi"].TargetLocation))
			.AddPrecondition(beliefs["FoundFriendlyPoi"])
			.AddEffect(beliefs["AtFriendlyPoi"])
			.Build(),

			new EntityActions.Builder("PickUpResources")
			.WithStrategy(new InteractStrategy(entityBrain, new PickupResourcesInteract(beliefs["FoundFriendlyPoi"])))
			.AddPrecondition(beliefs["AtFriendlyPoi"])
			.AddPrecondition(beliefs["FriendlyPoiNeedsResTransfer"])
			.AddEffect(beliefs["PickUpResources"])
			.Build(),

			new EntityActions.Builder("MoveToDropOffResources")
			.WithStrategy(new MoveStrategy(navMeshAgent, 3f, () => beliefs["AtHomeBase"].Location))
			.AddPrecondition(beliefs["TransferResourceToHomeBase"])
			.AddEffect(beliefs["AtHomeBase"])
			.Build(),

			new EntityActions.Builder("DropOffResources")
			.WithStrategy(new InteractStrategy(entityBrain, new DropOffResourcesInteract(beliefs["FoundFriendlyPoi"])))
			.AddPrecondition(beliefs["AtHomeBase"])
			.AddPrecondition(beliefs["TransferResourceToHomeBase"])
			.AddEffect(beliefs["DropOffResources"])
			.Build(),

			new EntityActions.Builder("MoveToCapturablePoi")
			.WithStrategy(new MoveStrategy(navMeshAgent, 3f, () => beliefs["FoundEnemyPoi"].TargetLocation))
			.AddPrecondition(beliefs["EnemyPoiCapturable"])
			.AddEffect(beliefs["AtEnemyPoi"])
			.Build(),

			new EntityActions.Builder("CapturePoi")
			.WithStrategy(new InteractStrategy(entityBrain, new CapturePoiInteract(beliefs["FoundEnemyPoi"])))
			.AddPrecondition(beliefs["AtEnemyPoi"])
			.AddPrecondition(beliefs["EnemyPoiCapturable"])
			.AddEffect(beliefs["CapturePoi"])
			.Build(),
		};

		return actions;
	}
	public HashSet<EntityGoals> SetupGoals()
	{
		goals = new HashSet<EntityGoals>
		{
			new EntityGoals.Builder("Idle")
			.WithPriority(5)
			.WithDesiredEffect(beliefs["Nothing"])
			.Build(),

			new EntityGoals.Builder("FindPois")
			.WithPriority(10)
			.WithDesiredEffect(beliefs["FindPois"])
			.Build(),

			new EntityGoals.Builder("CapturePoi")
			.WithPriority(20)
			.WithDesiredEffect(beliefs["CapturePoi"])
			.Build(),

			new EntityGoals.Builder("FleeFromTarget")
			.WithPriority(40)
			.WithDesiredEffect(beliefs["FleeingFromTarget"])
			.Build(),

			new EntityGoals.Builder("PickUpResources")
			.WithPriority(60)
			.WithDesiredEffect(beliefs["PickUpResources"])
			.Build(),

			new EntityGoals.Builder("DropOffResources")
			.WithPriority(80)
			.WithDesiredEffect(beliefs["DropOffResources"])
			.Build(),

			new EntityGoals.Builder("KeepHealthUp")
			.WithPriority(90)
			.WithDesiredEffect(beliefs["AgentIsHealthy"])
			.Build(),
		};

		return goals;
	}
}
