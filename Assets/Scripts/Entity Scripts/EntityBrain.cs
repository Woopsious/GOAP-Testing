using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using static EntitySensor;

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
	public GameObject attackOneTarget;
	public GameObject attackTwoTarget;

	public EntityGoals lastGoal;
	public EntityGoals currentGoal;
	public ActionPlan actionPlan;
	public EntityActions currentAction;

	public Dictionary<string, EntityBeliefs> beliefs;
	public HashSet<EntityActions> actions;
	public HashSet<EntityGoals> goals;

	IGoapPlanner gPlanner;

	public Material redTeamMaterial;
	public Material greenTeamMaterial;

	void Awake()
	{
		entityStats = GetComponent<EntityStats>();

		if (entityStats.type == null)
			Debug.LogError("Entity Type not set for gameobject: " + gameObject.name);
		else if (entityStats.type.team == EntityTypes.EntityTeam.redTeam)
			GetComponent<MeshRenderer>().sharedMaterial = redTeamMaterial;
		else if (entityStats.type.team == EntityTypes.EntityTeam.greenTeam)
			GetComponent<MeshRenderer>().sharedMaterial = greenTeamMaterial;

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

		chaseSensor.UpdateSensorSettings(0, entityStats.type.chaseRange, SensorType.chase);

		if (entityStats.type.attackTypeOne == EntityTypes.AttackType.melee)
			attackSensorOne.UpdateSensorSettings(entityStats.type.attackOneMinRange, entityStats.type.attackOneMaxRange, SensorType.meleeAttack);
		else
			attackSensorOne.UpdateSensorSettings(entityStats.type.attackOneMinRange, entityStats.type.attackOneMaxRange, SensorType.rangedAttack);

		if (entityStats.type.attackTypeOne == EntityTypes.AttackType.melee)
			attackSensorTwo.UpdateSensorSettings(entityStats.type.attackTwoMinRange, entityStats.type.attackTwoMaxRange, SensorType.meleeAttack);
		else
			attackSensorTwo.UpdateSensorSettings(entityStats.type.attackTwoMinRange, entityStats.type.attackTwoMaxRange, SensorType.rangedAttack);

		if (entityStats.type.attackOneMinRange != 0) //flee range = min weapon range, unless min weapon range = 0 (melee entities)
			fleeSensor.UpdateSensorSettings(0, entityStats.type.attackOneMinRange, SensorType.flee);
		else if (entityStats.type.attackTwoMinRange != 0)
			fleeSensor.UpdateSensorSettings(0, entityStats.type.attackTwoMinRange, SensorType.flee);
		else
			fleeSensor.UpdateSensorSettings(0, entityStats.type.fleeRange, SensorType.flee);

		SetupBeliefs();
		SetupActions();
		SetupGoals();
		SetupTimers();
	}

	void SetupBeliefs()
	{
		beliefs = new Dictionary<string, EntityBeliefs>();
		BeliefFactory factory = new(this, beliefs);

		factory.AddBelief("Nothing", () => false);

		factory.AddBelief("AgentIdle", () => !navMeshAgent.hasPath);
		factory.AddBelief("AgentMoving", () => navMeshAgent.hasPath);
		factory.AddBelief("AgentHealthLow", () => entityStats.currentHealth < 40);
		factory.AddBelief("AgentIsHealthy", () => entityStats.currentHealth >= 60);

		factory.AddLocationBelief("AgentAtFoodShack", 3f, foodShack);

		factory.AddSensorBelief("EntityInChaseRange", chaseSensor);
		factory.AddBelief("ChasingEntity", () => false);

		factory.AddSensorBelief("EntityInFleeRange", fleeSensor);
		factory.AddBelief("FleeingFromEntity", () => false);

		factory.AddSensorBelief("EntityInAttackOneRange", attackSensorOne);
		factory.AddSensorBelief("EntityInAttackTwoRange", attackSensorTwo);
		factory.AddBelief("EntityNotInsideRangedRange", () => !fleeSensor.IsTargetInRange);

		factory.AddBelief("AllAttacksOnCooldown", () => allAttacksOnCooldown);
		factory.AddBelief("AttackOneReady", () => attackOneReady && attackOneTarget != null);
		factory.AddBelief("AttackTwoReady", () => attackTwoReady && attackTwoTarget != null);

		factory.AddBelief("WaitingForAttackCooldowns", () => false);
		factory.AddBelief("AttackOne", () => false);
		factory.AddBelief("AttackTwo", () => false);
	}
	void SetupActions()
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

			new EntityActions.Builder("FleeFromEntity")
			.WithStrategy(new FleeStrategy(navMeshAgent, () => beliefs["EntityInFleeRange"].Location))
			.AddPrecondition(beliefs["EntityInFleeRange"])
			.AddEffect(beliefs["FleeingFromEntity"])
			.Build(),

			new EntityActions.Builder("ChaseEntity")
			.WithStrategy(new MoveStrategy(navMeshAgent, entityStats.type.attackOneMaxRange, () => beliefs["EntityInChaseRange"].Location))
			.AddPrecondition(beliefs["EntityInChaseRange"])
			.AddEffect(beliefs["ChasingEntity"])
			.Build(),

			new EntityActions.Builder("WaitForAttacks")
			.WithStrategy(new IdleStrategy(0.5f))
			.AddPrecondition(beliefs["EntityInAttackOneRange"])
			.AddPrecondition(beliefs["EntityInAttackTwoRange"])
			.AddPrecondition(beliefs["AllAttacksOnCooldown"])
			.AddEffect(beliefs["WaitingForAttackCooldowns"])
			.Build(),

			new EntityActions.Builder("AttackOne")
			.WithStrategy(new BasicAttackStrategy(this, 1))
			.AddPrecondition(beliefs["EntityInAttackOneRange"])
			.AddPrecondition(beliefs["AttackOneReady"])
			.AddEffect(beliefs["AttackOne"])
			.Build(),

			new EntityActions.Builder("AttackTwo")
			.WithStrategy(new BasicAttackStrategy(this, 2))
			.AddPrecondition(beliefs["EntityInAttackTwoRange"])
			.AddPrecondition(beliefs["AttackTwoReady"])
			.AddEffect(beliefs["AttackTwo"])
			.Build()
		};
	}
	void SetupGoals()
	{
		goals = new HashSet<EntityGoals>
		{
			new EntityGoals.Builder("FleeFromEntity")
			.WithPriority(50)
			.WithDesiredEffect(beliefs["FleeingFromEntity"])
			.Build(),

			new EntityGoals.Builder("AttackTwo")
			.WithPriority(60)
			.WithDesiredEffect(beliefs["AttackTwo"])
			.Build(),

			new EntityGoals.Builder("AttackOne")
			.WithPriority(60)
			.WithDesiredEffect(beliefs["AttackOne"])
			.Build(),

			new EntityGoals.Builder("WaitForAttackCooldowns")
			.WithPriority(50)
			.WithDesiredEffect(beliefs["WaitingForAttackCooldowns"])
			.Build(),

			new EntityGoals.Builder("ChaseEntity")
			.WithPriority(40)
			.WithDesiredEffect(beliefs["ChasingEntity"])
			.Build(),

			new EntityGoals.Builder("KeepHealthUp")
			.WithPriority(20)
			.WithDesiredEffect(beliefs["AgentIsHealthy"])
			.Build(),

			new EntityGoals.Builder("Wander")
			.WithPriority(5)
			.WithDesiredEffect(beliefs["AgentMoving"])
			.Build(),
		};
	}
	void SetupTimers()
	{
		attackOneTimer = new CountdownTimer(entityStats.type.attackOneCooldown);
		attackOneTimer.OnTimerStart += () =>
		{
			attackOneReady = false;
			UseAttackOne();
		};
		attackOneTimer.OnTimerStop += () =>
		{
			attackOneReady = true;
		};

		attackTwoTimer = new CountdownTimer(entityStats.type.attackTwoCooldown);
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
		if (entityStats.type.team == EntityTypes.EntityTeam.greenTeam)
			Debug.LogError("used attack one");

		attackOneTarget.GetComponent<EntityStats>().RecieveDamage(entityStats.type.attackOneDamage);
	}
	void UseAttackTwo()
	{
		if (entityStats.type.team == EntityTypes.EntityTeam.greenTeam)
			Debug.LogError("used attack two");

		attackTwoTarget.GetComponent<EntityStats>().RecieveDamage(entityStats.type.attackTwoDamage);
	}

	void TickAllTimers()
	{
		attackOneTimer.Tick(Time.deltaTime, false);
		attackTwoTimer.Tick(Time.deltaTime, false);
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

			case SensorType.flee:
			//noop
			break;

			case SensorType.rangedAttack:
			attackOneTarget = target;
			break;

			case SensorType.meleeAttack:
			attackTwoTarget = target;
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
}
