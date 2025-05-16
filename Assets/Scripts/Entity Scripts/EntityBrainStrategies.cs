using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

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

		factory.AddLocationBelief("AgentAtFoodShack", 3f, knownLocations[0]);

		factory.AddTargetBelief("TargetInChaseRange", entitySensors[0]);
		factory.AddBelief("ChasingTarget", () => false);

		factory.AddTargetBelief("TargetInFleeRange", entitySensors[1]);
		factory.AddBelief("FleeingFromTarget", () => false);

		factory.AddTargetBelief("TargetInAttackOneRange", entitySensors[2]);
		factory.AddTargetBelief("TargetInAttackTwoRange", entitySensors[3]);
		factory.AddBelief("TargetNotInAttackOneRange",() => entitySensors[2].target.target == null);
		factory.AddBelief("TargetNotInAttackTwoRange", () => entitySensors[3].target.target == null);

		factory.AddBelief("AllAttacksOnCooldown", () => attackBools[0]());
		factory.AddBelief("AttackOneReady", () => attackBools[1]());
		factory.AddBelief("AttackOneNotReady", () => !attackBools[1]());
		factory.AddBelief("AttackTwoReady", () => attackBools[2]());
		factory.AddBelief("AttackTwoNotReady", () => !attackBools[2]());

		factory.AddBelief("WaitingForAttackCooldowns", () => false);
		factory.AddBelief("AttackOne", () => false);
		factory.AddBelief("AttackTwo", () => false);

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

			new EntityActions.Builder("MoveToEatingPosition")
			.WithStrategy(new MoveStrategy(navMeshAgent, () => knownLocations[0].transform.position))
			.AddEffect(beliefs["AgentAtFoodShack"])
			.Build(),

			new EntityActions.Builder("Eat")
			.WithStrategy(new IdleStrategy(5))  // Later replace with a Command
			.AddPrecondition(beliefs["AgentAtFoodShack"])
			.AddEffect(beliefs["AgentIsHealthy"])
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

			new EntityActions.Builder("WaitForAttacks")
			.WithStrategy(new IdleStrategy(0.25f))
			.AddPrecondition(beliefs["TargetInAttackOneRange"])
			.AddPrecondition(beliefs["TargetInAttackTwoRange"])
			.AddPrecondition(beliefs["AllAttacksOnCooldown"])
			.AddEffect(beliefs["WaitingForAttackCooldowns"])
			.Build(),

			new EntityActions.Builder("MoveToUseAttackOne")
			.WithStrategy(new MoveIntoAttackRange(navMeshAgent, () => beliefs["TargetInChaseRange"].TargetLocation, entityStats._Data.attackData[0]))
			.AddPrecondition(beliefs["TargetNotInAttackOneRange"])
			.AddPrecondition(beliefs["AttackTwoNotReady"])
			.AddEffect(beliefs["AttackOne"])
			.Build(),

			new EntityActions.Builder("MoveToUseAttackTwo")
			.WithStrategy(new MoveIntoAttackRange(navMeshAgent, () => beliefs["TargetInChaseRange"].TargetLocation, entityStats._Data.attackData[1]))
			.AddPrecondition(beliefs["TargetNotInAttackTwoRange"])
			.AddPrecondition(beliefs["AttackOneNotReady"])
			.AddEffect(beliefs["AttackTwo"])
			.Build(),

			new EntityActions.Builder("AttackOne")
			.WithStrategy(new BasicAttackStrategy(entityBrain, 1))
			.AddPrecondition(beliefs["TargetInAttackOneRange"])
			.AddPrecondition(beliefs["AttackOneReady"])
			.AddEffect(beliefs["AttackOne"])
			.Build(),

			new EntityActions.Builder("AttackTwo")
			.WithStrategy(new BasicAttackStrategy(entityBrain, 2))
			.AddPrecondition(beliefs["TargetInAttackTwoRange"])
			.AddPrecondition(beliefs["AttackTwoReady"])
			.AddEffect(beliefs["AttackTwo"])
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

			new EntityGoals.Builder("WaitForAttackCooldowns")
			.WithPriority(60)
			.WithDesiredEffect(beliefs["WaitingForAttackCooldowns"])
			.Build(),

			new EntityGoals.Builder("MoveToUseAttackOne")
			.WithPriority(70)
			.WithDesiredEffect(beliefs["AttackOne"])
			.Build(),

			new EntityGoals.Builder("MoveToUseAttackTwo")
			.WithPriority(70)
			.WithDesiredEffect(beliefs["AttackTwo"])
			.Build(),

			new EntityGoals.Builder("AttackOne")
			.WithPriority(80)
			.WithDesiredEffect(beliefs["AttackOne"])
			.Build(),

			new EntityGoals.Builder("AttackTwo")
			.WithPriority(80)
			.WithDesiredEffect(beliefs["AttackTwo"])
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

		factory.AddLocationBelief("AgentAtFoodShack", 3f, knownLocations[0]);

		factory.AddTargetBelief("FoundPoi", entitySensors[0]);

		factory.AddBelief("AtPoi", () => entityBrain.InRangeOf(beliefs["FoundPoi"].TargetLocation, 7.5f));

		//factory.AddPoiCaptureableBelief("PoiCaptureable", entityStats._Data.team, () => entityBrain.chaseTarget.poiController);
		factory.AddBelief("PoiRefExists", () => entityBrain.chaseTarget.poiController != null);

		factory.AddBelief("PoiCapturable", () => beliefs["PoiRefExists"].PoIController != null);

		factory.AddBelief("CapturePoi", () => false);

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

			new EntityActions.Builder("MoveToEatingPosition")
			.WithStrategy(new MoveStrategy(navMeshAgent, () => knownLocations[0].position))
			.AddEffect(beliefs["AgentAtFoodShack"])
			.Build(),

			new EntityActions.Builder("Eat")
			.WithStrategy(new IdleStrategy(5))  // Later replace with a Command
			.AddPrecondition(beliefs["AgentAtFoodShack"])
			.AddEffect(beliefs["AgentIsHealthy"])
			.Build(),

			new EntityActions.Builder("LookForPoi")
			.WithStrategy(new FindPoiStrategy(entityBrain, navMeshAgent, 50))
			.AddEffect(beliefs["FoundPoi"])
			.Build(),

			new EntityActions.Builder("MoveToPoi")
			.WithStrategy(new MoveStrategy(navMeshAgent, 9f, () => beliefs["FoundPoi"].TargetLocation))
			.AddPrecondition(beliefs["FoundPoi"])
			.AddEffect(beliefs["AtPoi"])
			.Build(),

			new EntityActions.Builder("CapturePoi")
			.WithStrategy(new CapturePoiStrategy(entityStats, entityBrain))
			.AddPrecondition(beliefs["AtPoi"])
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

			new EntityGoals.Builder("FindPoi")
			.WithPriority(10)
			.WithDesiredEffect(beliefs["FoundPoi"])
			.Build(),

			new EntityGoals.Builder("KeepHealthUp")
			.WithPriority(20)
			.WithDesiredEffect(beliefs["AgentIsHealthy"])
			.Build(),

			new EntityGoals.Builder("CapturePoi")
			.WithPriority(80)
			.WithDesiredEffect(beliefs["CapturePoi"])
			.Build(),
		};

		return goals;
	}
}
