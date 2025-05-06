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

	public Material redTeamMaterial;
	public Material greenTeamMaterial;

	void Awake()
	{
		entityStats = GetComponent<EntityStats>();

		if (entityStats._Data == null)
			Debug.LogError("Entity Type not set for gameobject: " + gameObject.name);
		else if (entityStats._Data.team == EntityData.EntityTeam.redTeam)
			GetComponent<MeshRenderer>().sharedMaterial = redTeamMaterial;
		else if (entityStats._Data.team == EntityData.EntityTeam.greenTeam)
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

		float fleeRange = 0;
		foreach (EntityAttackData attackData in entityStats._Data.attackData)
		{
			if (attackData.attackMinRange > fleeRange)
				fleeRange = attackData.attackMinRange;
		}

		chaseSensor.UpdateSensorSettings(entityStats._Data.chaseRange);
		fleeSensor.UpdateSensorSettings(entityStats._Data.fleeRange);
		attackSensorOne.UpdateSensorSettings(entityStats._Data.attackData[0]);
		attackSensorTwo.UpdateSensorSettings(entityStats._Data.attackData[1]);

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
			.WithStrategy(new IdleStrategy(0.5f))
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
	void SetupGoals()
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
			.WithPriority(40)
			.WithDesiredEffect(beliefs["ChasingTarget"])
			.Build(),

			new EntityGoals.Builder("FleeFromTarget")
			.WithPriority(45)
			.WithDesiredEffect(beliefs["FleeingFromTarget"])
			.Build(),

			new EntityGoals.Builder("WaitForAttackCooldowns")
			.WithPriority(50)
			.WithDesiredEffect(beliefs["WaitingForAttackCooldowns"])
			.Build(),

			new EntityGoals.Builder("MoveToUseAttackOne")
			.WithPriority(60)
			.WithDesiredEffect(beliefs["AttackOne"])
			.Build(),

			new EntityGoals.Builder("MoveToUseAttackTwo")
			.WithPriority(60)
			.WithDesiredEffect(beliefs["AttackTwo"])
			.Build(),

			new EntityGoals.Builder("AttackOne")
			.WithPriority(70)
			.WithDesiredEffect(beliefs["AttackOne"])
			.Build(),

			new EntityGoals.Builder("AttackTwo")
			.WithPriority(70)
			.WithDesiredEffect(beliefs["AttackTwo"])
			.Build(),
		};
	}
	void SetupTimers()
	{
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
		if (entityStats._Data.team == EntityData.EntityTeam.greenTeam)
			Debug.LogError("used attack one");

		attackTargetOne.GetComponent<EntityStats>().RecieveDamage(entityStats._Data.attackData[0].attackDamage);
	}
	void UseAttackTwo()
	{
		if (entityStats._Data.team == EntityData.EntityTeam.greenTeam)
			Debug.LogError("used attack two");

		attackTargetTwo.GetComponent<EntityStats>().RecieveDamage(entityStats._Data.attackData[1].attackDamage);
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

			case SensorType.attackSensorOne:
			attackTargetOne = target;
			break;

			case SensorType.attackSensorTwo:
			attackTargetTwo = target;
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
