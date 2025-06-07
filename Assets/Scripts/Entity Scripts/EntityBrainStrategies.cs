using System;
using System.Collections.Generic;
using Unity.VisualScripting;
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
	readonly EntityAttackData[] attackData;
	readonly Func<bool>[] attackBools;

	public Dictionary<string, EntityBeliefs> beliefs;
	public HashSet<EntityActions> actions;
	public HashSet<EntityGoals> goals;

	public CombatBrainStrategy(EntityBrain entityBrain, EntityStats entityStats, NavMeshAgent navMeshAgent,
		EntitySensor[] entitySensors, Transform[] knownLocations, EntityAttackData[] attackData, Func<bool>[] attackBools)
	{
		this.entityBrain = entityBrain;
		this.entityStats = entityStats;
		this.navMeshAgent = navMeshAgent;

		this.entitySensors = entitySensors;
		this.knownLocations = knownLocations;
		this.attackData = attackData;
		this.attackBools = attackBools;

		entityBrain.answerRequestHelpGoalPriority = 50;
	}

	public Dictionary<string, EntityBeliefs> SetupBeliefs()
	{
		beliefs = new Dictionary<string, EntityBeliefs>();
		BeliefFactory factory = new(entityBrain, beliefs);

		//basic beliefs
		factory.AddBelief("Nothing", () => false);
		factory.AddBelief("AgentIdle", () => !navMeshAgent.hasPath);
		factory.AddBelief("AgentMoving", () => navMeshAgent.hasPath);
		factory.AddBelief("AgentHealthLow", () => entityStats.currentHealth < 40);
		factory.AddBelief("AgentIsHealthy", () => entityStats.currentHealth >= 60);

		//at home base beliefs
		if (entityStats._Data.team == EntityData.EntityTeam.redTeam)
			factory.AddLocationBelief("AtHomeBase", 10f, AiDirector.instance.redTeamHomeBase.transform.position);
		else if (entityStats._Data.team == EntityData.EntityTeam.greenTeam)
			factory.AddLocationBelief("AtHomeBase", 10f, AiDirector.instance.greenTeamHomeBase.transform.position);

		//flee beliefs
		factory.AddTargetBelief("TargetInFleeRange", () => entitySensors[0].sensorBehaviours[0].FoundTarget());
		factory.AddBelief("FleeingFromTarget", () => false);

		//target beliefs
		factory.AddTargetBelief("TargetInChaseRange", () => entitySensors[0].sensorBehaviours[1].FoundTarget());
		factory.AddBelief("ChasingTarget", () => false);
		factory.AddTargetBelief("TargetInAttackOneRange", () => entitySensors[0].sensorBehaviours[2].FoundTarget());
		factory.AddTargetBelief("TargetInAttackTwoRange", () => entitySensors[0].sensorBehaviours[3].FoundTarget());

		//attack beliefs
		factory.AddBelief("AllAttacksOnCooldown", () => attackBools[0]());
		factory.AddBelief("AttackOneReady", () => attackBools[1]());
		factory.AddBelief("AttackTwoReady", () => attackBools[2]());

		//enemy poi beliefs
		factory.AddTargetBelief("FoundEnemyPoi", () => entitySensors[2].sensorBehaviours[0].FoundTarget());

		//friendly poi beliefs
		factory.AddTargetBelief("FoundFriendlyPoi", () => entitySensors[2].sensorBehaviours[1].FoundTarget());

		//help beliefs
		factory.AddBelief("NeedsHelp", () => entityBrain.EntityNeedsHelp()); //compare friendlies/enemies in chase range sensor
		factory.AddBelief("CanRequestHelp", () => entityBrain.RequestHelpTimer.IsFinished);
		factory.AddBelief("AnswerRequestForHelp", () => entityBrain.RequestHelpOriginPos != Vector3.zero);

		//goal beliefs
		factory.AddBelief("MoveToAttack", () => false);
		factory.AddBelief("Attack", () => false);
		factory.AddBelief("RequestHelp", () => false);
		factory.AddBelief("AnsweringRequestHelp", () => false);

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
			.WithStrategy(new MoveToAttackStrategy(navMeshAgent, () => beliefs["TargetInChaseRange"].TargetLocation, entityStats._Data.attackData[0]))
			.AddPrecondition(beliefs["TargetInChaseRange"])
			.AddEffect(beliefs["MoveToAttack"])
			.Build(),

			new EntityActions.Builder("MoveToUseAttackTwo")
			.WithStrategy(new MoveToAttackStrategy(navMeshAgent, () => beliefs["TargetInChaseRange"].TargetLocation, entityStats._Data.attackData[1]))
			.AddPrecondition(beliefs["TargetInChaseRange"])
			.AddEffect(beliefs["MoveToAttack"])
			.Build(),

			new EntityActions.Builder("WaitForAttacks")
			.WithStrategy(new IdleStrategy(0.5f))
			.AddPrecondition(beliefs["AllAttacksOnCooldown"])
			.AddEffect(beliefs["Attack"])
			.Build(),

			new EntityActions.Builder("AttackOne")
			.WithStrategy(new AttackStrategy(entityBrain, 1, () => beliefs["TargetInAttackOneRange"].TargetData))
			.AddPrecondition(beliefs["TargetInAttackOneRange"])
			.AddPrecondition(beliefs["AttackOneReady"])
			.AddEffect(beliefs["Attack"])
			.Build(),

			new EntityActions.Builder("AttackTwo")
			.WithStrategy(new AttackStrategy(entityBrain, 2, () => beliefs["TargetInAttackTwoRange"].TargetData))
			.AddPrecondition(beliefs["TargetInAttackTwoRange"])
			.AddPrecondition(beliefs["AttackTwoReady"])
			.AddEffect(beliefs["Attack"])
			.Build(),

			new EntityActions.Builder("RequestHelp")
			.WithStrategy(new CallForHelpStrategy(entityBrain.entityStats, entitySensors[1], 3))
			.AddPrecondition(beliefs["NeedsHelp"])
			.AddPrecondition(beliefs["CanRequestHelp"])
			.AddEffect(beliefs["RequestHelp"])
			.Build(),

			new EntityActions.Builder("AnswerRequestHelp")
			.WithStrategy(new MoveStrategy(navMeshAgent, 10, () => entityBrain.RequestHelpOriginPos))
			.AddPrecondition(beliefs["AnswerRequestForHelp"])
			.AddEffect(beliefs["RequestHelp"])
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

			new EntityGoals.Builder("Wander")
			.WithPriority(10)
			.WithDesiredEffect(beliefs["AgentMoving"])
			.Build(),

			new EntityGoals.Builder("KeepHealthUp")
			.WithPriority(20)
			.WithDesiredEffect(beliefs["AgentIsHealthy"])
			.Build(),

			new EntityGoals.Builder("FleeFromTarget")
			.WithPriority(40)
			.WithDesiredEffect(beliefs["FleeingFromTarget"])
			.Build(),

			new EntityGoals.Builder("AnswerRequstForHelp")
			.WithPriority(entityBrain.answerRequestHelpGoalPriority)
			.WithDesiredEffect(beliefs["AnsweringRequestHelp"])
			.Build(),

			new EntityGoals.Builder("ChaseTarget")
			.WithPriority(50)
			.WithDesiredEffect(beliefs["ChasingTarget"])
			.Build(),

			new EntityGoals.Builder("MoveToAttack")
			.WithPriority(80)
			.WithDesiredEffect(beliefs["MoveToAttack"])
			.Build(),

			new EntityGoals.Builder("Attack")
			.WithPriority(90)
			.WithDesiredEffect(beliefs["Attack"])
			.Build(),

			new EntityGoals.Builder("RequstForHelp")
			.WithPriority(100)
			.WithDesiredEffect(beliefs["RequestHelp"])
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

		//basic beliefs
		factory.AddBelief("Nothing", () => false);
		factory.AddBelief("AgentIdle", () => !navMeshAgent.hasPath);
		factory.AddBelief("AgentMoving", () => navMeshAgent.hasPath);
		factory.AddBelief("AgentHealthLow", () => entityStats.currentHealth < 30);
		factory.AddBelief("AgentIsHealthy", () => entityStats.currentHealth >= 40);

		//flee beliefs
		factory.AddTargetBelief("TargetInFleeRange", () => entitySensors[0].sensorBehaviours[0].FoundTarget());
		factory.AddBelief("FleeingFromTarget", () => false);

		//at home base beliefs
		if (entityStats._Data.team == EntityData.EntityTeam.redTeam)
			factory.AddLocationBelief("AtHomeBase", 10f, AiDirector.instance.redTeamHomeBase.transform.position);
        else if (entityStats._Data.team == EntityData.EntityTeam.greenTeam)
			factory.AddLocationBelief("AtHomeBase", 10f, AiDirector.instance.greenTeamHomeBase.transform.position);

		//enemy poi beliefs
		factory.AddTargetBelief("FoundEnemyPoi", () => entitySensors[2].sensorBehaviours[0].FoundTarget());

		factory.AddBelief("AtEnemyPoi", () => 
			beliefs["FoundEnemyPoi"].TargetData.obj != null && 
			entityBrain.InRangeOf(beliefs["FoundEnemyPoi"].TargetLocation, 10f));

		factory.AddBelief("EnemyPoiCapturable", () => 
			beliefs["FoundEnemyPoi"].TargetData.poi != null &&
			beliefs["FoundEnemyPoi"].TargetData.poi.PoiCapturable(entityStats));

		//friendly poi beliefs
		factory.AddTargetBelief("FoundFriendlyPoi", () => entitySensors[2].sensorBehaviours[1].FoundTarget());

		factory.AddBelief("AtFriendlyPoi", () => 
			beliefs["FoundFriendlyPoi"].TargetData.obj != null &&
			entityBrain.InRangeOf(beliefs["FoundFriendlyPoi"].TargetLocation, 10f));

		factory.AddBelief("FriendlyPoiNeedsResTransfer", () => 
			beliefs["FoundFriendlyPoi"].TargetData.poi != null &&
			beliefs["FoundFriendlyPoi"].TargetData.poi.PoiNeedsResourceTransfer());

		factory.AddBelief("TransferResourceToHomeBase", () => entityStats.carriedResources != 0);

		//help beliefs
		factory.AddBelief("NeedsHelp", () => entityBrain.EntityNeedsHelp()); //compare friendlies/enemies in chase range sensor
		factory.AddBelief("CanRequestHelp", () => entityBrain.RequestHelpTimer.IsFinished);
		factory.AddBelief("AnswerRequestForHelp", () => entityBrain.RequestHelpOriginPos != Vector3.zero);

		//goal beliefs
		factory.AddBelief("FindPois", () => false);
		factory.AddBelief("CapturePoi", () => false);
		factory.AddBelief("PickUpResources", () => false);
		factory.AddBelief("DropOffResources", () => false);
		factory.AddBelief("RequestHelp", () => false);

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
			.WithStrategy(new FindPoiStrategy(navMeshAgent, 50))
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

			new EntityActions.Builder("RequestHelp")
			.WithStrategy(new CallForHelpStrategy(entityBrain.entityStats, entitySensors[1], 3))
			.AddPrecondition(beliefs["NeedsHelp"])
			.AddPrecondition(beliefs["CanRequestHelp"])
			.AddEffect(beliefs["RequestHelp"])
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

			new EntityGoals.Builder("RequstForHelp")
			.WithPriority(100)
			.WithDesiredEffect(beliefs["RequestHelp"])
			.Build(),
		};

		return goals;
	}
}
