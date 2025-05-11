using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using static EntitySensor;
using static EntityData;

public class EntityBrain : MonoBehaviour
{
	EntityStats entityStats;
	NavMeshAgent navMeshAgent;
	Rigidbody rb;

	[Header("Sensors")]
	[SerializeField] EntitySensor chaseSensor;
	[SerializeField] EntitySensor fleeSensor;
	[SerializeField] EntitySensor attackSensorOne;
	[SerializeField] EntitySensor attackSensorTwo;

	[Header("Known Locations")]
	public Transform foodShack { get; private set; }

	public CountdownTimer attackOneTimer;
	public CountdownTimer attackTwoTimer;

	[Header("Attacks")]
	public bool allAttacksOnCooldown;
	public bool attackOneReady;
	public bool attackTwoReady;

	[Header("EntityTargets")]
	public GameObject chaseTarget;
	public GameObject attackTargetOne;
	public GameObject attackTargetTwo;

	public EntityGoals lastGoal;
	public EntityGoals currentGoal;
	public ActionPlan actionPlan;
	public EntityActions currentAction;

	public Dictionary<string, EntityBeliefs> beliefs;
	public HashSet<EntityActions> actions;
	public HashSet<EntityGoals> goals;

	IGoapPlanner gPlanner;

	void Awake()
	{
		entityStats = GetComponent<EntityStats>();
		navMeshAgent = GetComponent<NavMeshAgent>();
		rb = GetComponent<Rigidbody>();
		rb.freezeRotation = true;

		gPlanner = new GoapPlanner();
	}

	void Start()
	{
		Initilize();
	}

	void Update()
	{
		TickAllTimers();

		if (!attackOneReady && !attackTwoReady)
			allAttacksOnCooldown = true;
		else allAttacksOnCooldown = false;

		CreateNewPlan();
	}

	void Initilize()
	{
		foodShack = GameManager.instance.foodShack.transform;

		if (foodShack == null)
			Debug.LogError("No Global Food Shack Location Set");

		attackOneReady = true;
		attackTwoReady = true;

		SetupSensors();

		SetupBeliefs();
		SetupActions();
		SetupGoals();
		SetupTimers();
	}

	void SetupSensors()
	{
		if (entityStats._Data.type == EntityType.combat)
		{
			float fleeRange = 1;
			foreach (EntityAttackData attackData in entityStats._Data.attackData)
			{
				if (attackData.attackMinRange > fleeRange)
					fleeRange = attackData.attackMinRange;
			}

			chaseSensor.UpdateSensorSettings(entityStats._Data.chaseRange);
			fleeSensor.UpdateSensorSettings(fleeRange);

			attackSensorOne.UpdateSensorSettings(entityStats._Data.attackData[0]);
			attackSensorTwo.UpdateSensorSettings(entityStats._Data.attackData[1]);
		}
        else if (entityStats._Data.type == EntityType.worker)
        {
			chaseSensor.UpdateSensorSettings(SensorType.poiDetector, entityStats._Data.chaseRange);
			fleeSensor.UpdateSensorSettings(entityStats._Data.fleeRange);
		}
    }

	void SetupBeliefs()
	{
		if (entityStats._Data.type == EntityType.combat)
			SetupCombatAiBeliefs();
		else if (entityStats._Data.type == EntityType.worker)
			SetupWorkerAiBeliefs();
	}
	void SetupCombatAiBeliefs()
	{
		beliefs = new Dictionary<string, EntityBeliefs>();
		BeliefFactory factory = new(this, beliefs);

		factory.AddBelief("Nothing", () => false);

		factory.AddBelief("AgentIdle", () => !navMeshAgent.hasPath);
		factory.AddBelief("AgentMoving", () => navMeshAgent.hasPath);
		factory.AddBelief("AgentHealthLow", () => entityStats.currentHealth < 40);
		factory.AddBelief("AgentIsHealthy", () => entityStats.currentHealth >= 60);

		factory.AddLocationBelief("AgentAtFoodShack", 3f, foodShack);

		factory.AddTargetBelief("TargetInChaseRange", chaseSensor);
		factory.AddBelief("ChasingTarget", () => false);

		factory.AddTargetBelief("TargetInFleeRange", fleeSensor);
		factory.AddBelief("FleeingFromTarget", () => false);

		factory.AddTargetBelief("TargetInAttackOneRange", attackSensorOne);
		factory.AddTargetBelief("TargetInAttackTwoRange", attackSensorTwo);
		factory.AddTargetBackupBelief("TargetHasBackupForAttackOne", attackSensorOne);
		factory.AddTargetBackupBelief("TargetHasBackupForAttackTwo", attackSensorTwo);

		factory.AddBelief("AttackOneReady", () => attackOneReady);
		factory.AddBelief("AttackOneNotReady", () => !attackOneReady);
		factory.AddBelief("AttackTwoReady", () => attackTwoReady);
		factory.AddBelief("AttackTwoNotReady", () => !attackTwoReady);
		factory.AddBelief("AllAttacksOnCooldown", () => allAttacksOnCooldown);

		factory.AddBelief("WaitingForAttackCooldowns", () => false);
		factory.AddBelief("AttackOne", () => false);
		factory.AddBelief("AttackTwo", () => false);
	}

	void SetupWorkerAiBeliefs()
	{
		beliefs = new Dictionary<string, EntityBeliefs>();
		BeliefFactory factory = new(this, beliefs);

		factory.AddBelief("Nothing", () => false);

		factory.AddBelief("AgentIdle", () => !navMeshAgent.hasPath);
		factory.AddBelief("AgentMoving", () => navMeshAgent.hasPath);
		factory.AddBelief("AgentHealthLow", () => entityStats.currentHealth < 30);
		factory.AddBelief("AgentIsHealthy", () => entityStats.currentHealth >= 40);

		factory.AddLocationBelief("AgentAtFoodShack", 3f, foodShack);

		factory.AddTargetBelief("FoundPoi", chaseSensor);

		factory.AddBelief("AtPoi", () => InRangeOf(beliefs["FoundPoi"].TargetLocation, 7.5f));
		factory.AddBelief("CapturePoi", () => false);
	}

	void SetupActions()
	{
		if (entityStats._Data.type == EntityData.EntityType.combat)
			SetupCombatAiActions();
		else if (entityStats._Data.type == EntityData.EntityType.worker)
			SetupWorkerAiActions();
	}
	void SetupCombatAiActions()
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
			.WithStrategy(new MoveStrategy(navMeshAgent, () => foodShack.position))
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
			.WithStrategy(new MoveIntoAttackRange(navMeshAgent, () => beliefs["TargetHasBackupForAttackOne"].TargetBackupLocation, entityStats._Data.attackData[0]))
			.AddPrecondition(beliefs["TargetHasBackupForAttackOne"])
			.AddPrecondition(beliefs["AttackTwoNotReady"])
			.AddEffect(beliefs["AttackOne"])
			.Build(),

			new EntityActions.Builder("MoveToUseAttackTwo")
			.WithStrategy(new MoveIntoAttackRange(navMeshAgent, () => beliefs["TargetHasBackupForAttackTwo"].TargetBackupLocation, entityStats._Data.attackData[1]))
			.AddPrecondition(beliefs["TargetHasBackupForAttackTwo"])
			.AddPrecondition(beliefs["AttackOneNotReady"])
			.AddEffect(beliefs["AttackTwo"])
			.Build(),

			new EntityActions.Builder("AttackOne")
			.WithStrategy(new BasicAttackStrategy(this, 1))
			.AddPrecondition(beliefs["TargetInAttackOneRange"])
			.AddPrecondition(beliefs["AttackOneReady"])
			.AddEffect(beliefs["AttackOne"])
			.Build(),

			new EntityActions.Builder("AttackTwo")
			.WithStrategy(new BasicAttackStrategy(this, 2))
			.AddPrecondition(beliefs["TargetInAttackTwoRange"])
			.AddPrecondition(beliefs["AttackTwoReady"])
			.AddEffect(beliefs["AttackTwo"])
			.Build()
		};
	}
	void SetupWorkerAiActions()
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
			.WithStrategy(new MoveStrategy(navMeshAgent, () => foodShack.position))
			.AddEffect(beliefs["AgentAtFoodShack"])
			.Build(),

			new EntityActions.Builder("Eat")
			.WithStrategy(new IdleStrategy(5))  // Later replace with a Command
			.AddPrecondition(beliefs["AgentAtFoodShack"])
			.AddEffect(beliefs["AgentIsHealthy"])
			.Build(),

			new EntityActions.Builder("LookForPoi")
			.WithStrategy(new FindPoiStrategy(this, navMeshAgent, 50))
			.AddEffect(beliefs["FoundPoi"])
			.Build(),

			new EntityActions.Builder("MoveToPoi")
			.WithStrategy(new MoveStrategy(navMeshAgent, 9f, () => beliefs["FoundPoi"].TargetLocation))
			.AddPrecondition(beliefs["FoundPoi"])
			.AddEffect(beliefs["AtPoi"])
			.Build(),

			new EntityActions.Builder("CapturePoi")
			.WithStrategy(new CapturePoiStrategy(entityStats, this))
			.AddPrecondition(beliefs["AtPoi"])
			.AddEffect(beliefs["CapturePoi"])
			.Build(),
		};
	}

	void SetupGoals()
	{
		if (entityStats._Data.type == EntityData.EntityType.combat)
			SetupCombatAiGoals();
		else if (entityStats._Data.type == EntityData.EntityType.worker)
			SetupWorkerAiGoals();
	}
	void SetupCombatAiGoals()
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
	}
	void SetupWorkerAiGoals()
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
	}

	void SetupTimers()
	{
		if (entityStats._Data.type != EntityType.combat) return; //workers have no attacks atm

		attackOneTimer = new CountdownTimer(entityStats._Data.attackData[0].attackCooldown);
		attackOneTimer.OnTimerStart += () =>
		{
			attackOneReady = false;
			UseAttackOne();
		};
		attackOneTimer.OnTimerStop += () =>
		{
			attackOneReady = true;
		};

		attackTwoTimer = new CountdownTimer(entityStats._Data.attackData[1].attackCooldown);
		attackTwoTimer.OnTimerStart += () =>
		{
			attackTwoReady = false;
			UseAttackTwo();
		};
		attackTwoTimer.OnTimerStop += () =>
		{
			attackTwoReady = true;
		};
	}

	void UseAttackOne()
	{
		attackTargetOne.GetComponent<EntityStats>().RecieveDamage(entityStats._Data.attackData[0].attackDamage);
	}
	void UseAttackTwo()
	{
		attackTargetTwo.GetComponent<EntityStats>().RecieveDamage(entityStats._Data.attackData[1].attackDamage);
	}

	void TickAllTimers()
	{
		attackOneTimer?.Tick(Time.deltaTime, false);
		attackTwoTimer?.Tick(Time.deltaTime, false);
	}

	void OnEnable()
	{
		chaseSensor.OnTargetChanged += OnTargetChanges;
		attackSensorOne.OnTargetChanged += OnTargetChanges;
		attackSensorTwo.OnTargetChanged += OnTargetChanges;
	}
	void OnDisable()
	{
		chaseSensor.OnTargetChanged -= OnTargetChanges;
		attackSensorOne.OnTargetChanged -= OnTargetChanges;
		attackSensorTwo.OnTargetChanged -= OnTargetChanges;
	}

	void OnTargetChanges(GameObject target, SensorType sensorType)
	{
		switch (sensorType)
		{
			case SensorType.chase:
			Debug.Log("Target changed, clearing current action and goal");
			// Force the planner to re-evaluate the plan
			chaseTarget = target;
			currentAction = null;
			currentGoal = null;
			break;

			case SensorType.attackSensorOne:
			attackTargetOne = target;
			break;

			case SensorType.attackSensorTwo:
			attackTargetTwo = target;
			break;

			case SensorType.poiDetector:
			//pois dont move so no need to recalc plan
			chaseTarget = target;
			break;
		}
	}

	void CreateNewPlan()
	{
		// Update the plan and current action if there is one
		if (currentAction == null)
		{
			Debug.Log("Calculating any potential new plan");
			CalculatePlan();

			if (actionPlan != null && actionPlan.Actions.Count > 0)
			{
				navMeshAgent.ResetPath();

				currentGoal = actionPlan.EntityGoals;
				Debug.Log($"Goal: {currentGoal.Name} with {actionPlan.Actions.Count} actions in plan");
				currentAction = actionPlan.Actions.Pop();
				Debug.Log($"Popped action: {currentAction.Name}");
				// Verify all precondition effects are true
				if (currentAction.Preconditions.All(b => b.Evaluate()))
				{
					currentAction.Start();
				}
				else
				{
					Debug.Log("Preconditions not met, clearing current action and goal");
					currentAction = null;
					currentGoal = null;
				}
			}
		}

		// If we have a current action, execute it
		if (actionPlan != null && currentAction != null)
			ExecuteActionPlan();
	}
	void CalculatePlan()
	{
		var priorityLevel = currentGoal?.Priority ?? 0;

		HashSet<EntityGoals> goalsToCheck = goals;

		// If we have a current goal, we only want to check goals with higher priority
		if (currentGoal != null)
		{
			Debug.Log("Current goal exists, checking goals with higher priority");
			goalsToCheck = new HashSet<EntityGoals>(goals.Where(g => g.Priority > priorityLevel));
		}

		var potentialPlan = gPlanner.Plan(this, goalsToCheck, lastGoal);
		if (potentialPlan != null)
		{
			actionPlan = potentialPlan;
		}
	}
	void ExecuteActionPlan()
	{
		currentAction.Update(Time.deltaTime);

		if (currentAction.Complete)
		{
			Debug.Log($"{currentAction.Name} complete");
			currentAction.Stop();
			currentAction = null;

			if (actionPlan.Actions.Count == 0)
			{
				Debug.Log("Plan complete");
				lastGoal = currentGoal;
				currentGoal = null;
			}
		}
	}

	private bool InRangeOf(Vector3 pos, float range) => Vector3.Distance(transform.position, pos) < range;
}

public interface IEntityBrainStrategy
{

}

public class EntityCombatBrain : IEntityBrainStrategy
{
	public EntityCombatBrain()
	{

	}

	public Dictionary<string, EntityBeliefs> beliefs;
	public HashSet<EntityActions> actions;
	public HashSet<EntityGoals> goals;
}

[CreateAssetMenu(fileName = "EntityData", menuName = "ScriptableObjects/EntityBrainType")]
public class EntityBrainType : ScriptableObject
{
	[SerializeReference] public Dictionary<string, EntityBeliefs> beliefs;
	[SerializeReference] public HashSet<EntityActions> actions;
	[SerializeReference] public HashSet<EntityGoals> goals;
}
