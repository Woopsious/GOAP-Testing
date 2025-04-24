using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class EntityAgent : MonoBehaviour
{
	[Header("Sensors")]
	[SerializeField] EntitySensor fleeSensor;
	[SerializeField] EntitySensor chaseSensor;
	[SerializeField] EntitySensor attackSensor;

	[Header("Known Locations")]
	[SerializeField] Transform restingPosition;
	[SerializeField] Transform foodShack;

	NavMeshAgent navMeshAgent;
	Rigidbody rb;

	[Header("Stats")]
	private readonly float maxHealth = 100;
	public float currentHealth;

	private readonly float maxStamina = 100;
	public float currentStamina;

	CountdownTimer statsTimer;
	CountdownTimer goalPriorityTimer;
	[SerializeField] public CountdownTimer basicAttackTimer;
	[SerializeField] public CountdownTimer heavyAttackTimer;

	[Header("Attacks")]
	public bool allAttacksOnCooldown;
	public bool basicAttackReady;
	public bool heavyAttackReady;

	public GameObject target;
	public Vector3 destination;

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
		navMeshAgent = GetComponent<NavMeshAgent>();
		rb = GetComponent<Rigidbody>();
		rb.freezeRotation = true;

		gPlanner = new GoapPlanner();
	}

	void Start()
	{
		currentHealth = maxHealth;
		currentStamina = maxStamina;

		basicAttackReady = true;
		heavyAttackReady = true;

		SetupBeliefs();
		SetupActions();
		SetupGoals();
		SetupTimers();
	}

	void Update()
	{
		statsTimer.Tick(Time.deltaTime, false);
		//goalPriorityTimer.Tick(Time.deltaTime);
		basicAttackTimer.Tick(Time.deltaTime, false);
		heavyAttackTimer.Tick(Time.deltaTime, false);

		if (!basicAttackReady && !heavyAttackReady)
			allAttacksOnCooldown = true;
		else allAttacksOnCooldown = false;

		CreateNewPlan();
	}

	void SetupBeliefs()
	{
		beliefs = new Dictionary<string, EntityBeliefs>();
		BeliefFactory factory = new(this, beliefs);

		factory.AddBelief("Nothing", () => false);

		factory.AddBelief("AgentIdle", () => !navMeshAgent.hasPath);
		factory.AddBelief("AgentMoving", () => navMeshAgent.hasPath);
		factory.AddBelief("AgentHealthLow", () => currentHealth < 20);
		factory.AddBelief("AgentIsHealthy", () => currentHealth >= 40);
		factory.AddBelief("AgentStaminaLow", () => currentStamina < 30);
		factory.AddBelief("AgentIsRested", () => currentStamina >= 50);

		factory.AddBelief("AllAttacksOnCooldown", () => allAttacksOnCooldown);
		factory.AddBelief("BasicAttackReady", () => basicAttackReady);
		factory.AddBelief("HeavyAttackReady", () => heavyAttackReady);

		factory.AddLocationBelief("AgentAtRestingPosition", 3f, restingPosition);
		factory.AddLocationBelief("AgentAtFoodShack", 3f, foodShack);

		factory.AddSensorBelief("PlayerInFleeRange", fleeSensor);
		factory.AddBelief("FleeingFromPlayer", () => false);

		factory.AddSensorBelief("PlayerInChaseRange", chaseSensor);
		factory.AddSensorBelief("PlayerInAttackRange", attackSensor);
		factory.AddBelief("AttackingPlayer", () => false); // Player can always be attacked, this will never become true
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

			new EntityActions.Builder("MoveToRestPosition")
			.WithStrategy(new MoveStrategy(navMeshAgent, () => restingPosition.position))
			.AddEffect(beliefs["AgentAtRestingPosition"])
			.Build(),

			new EntityActions.Builder("Rest")
			.WithStrategy(new IdleStrategy(3))
			.AddPrecondition(beliefs["AgentAtRestingPosition"])
			.AddEffect(beliefs["AgentIsRested"])
			.Build(),

			new EntityActions.Builder("ChasePlayer")
			.WithStrategy(new MoveStrategy(navMeshAgent, () => beliefs["PlayerInChaseRange"].Location))
			.AddPrecondition(beliefs["PlayerInChaseRange"])
			.AddEffect(beliefs["PlayerInAttackRange"])
			.Build(),

			new EntityActions.Builder("WaitForAttacks")
			.WithStrategy(new IdleStrategy(0.5f))
			.AddPrecondition(beliefs["AllAttacksOnCooldown"])
			.AddEffect(beliefs["AttackingPlayer"])
			.Build(),

			new EntityActions.Builder("BasicAttackPlayer")
			.WithStrategy(new BasicAttackStrategy(this))
			.AddPrecondition(beliefs["PlayerInAttackRange"])
			.AddPrecondition(beliefs["BasicAttackReady"])
			.AddEffect(beliefs["AttackingPlayer"])
			.Build(),

			new EntityActions.Builder("HeavyAttackPlayer")
			.WithStrategy(new HeavyAttackStrategy(this))
			.AddPrecondition(beliefs["PlayerInAttackRange"])
			.AddPrecondition(beliefs["HeavyAttackReady"])
			.AddEffect(beliefs["AttackingPlayer"])
			.Build(),

			new EntityActions.Builder("FleeFromPlayer")
			.WithStrategy(new FleeStrategy(navMeshAgent, () => beliefs["PlayerInFleeRange"].Location))
			.AddPrecondition(beliefs["PlayerInFleeRange"])
			.AddEffect(beliefs["FleeingFromPlayer"])
			.Build()
		};
	}
	void SetupGoals()
	{
		goals = new HashSet<EntityGoals>
		{
			new EntityGoals.Builder("FleeFromPlayer")
			.WithPriority(120)
			.WithDesiredEffect(beliefs["FleeingFromPlayer"])
			.Build(),

			new EntityGoals.Builder("ChasePlayer")
			.WithPriority(100)
			.WithDesiredEffect(beliefs["AttackingPlayer"])
			.Build(),

			new EntityGoals.Builder("KeepHealthUp")
			.WithPriority(80)
			.WithDesiredEffect(beliefs["AgentIsHealthy"])
			.Build(),

			new EntityGoals.Builder("KeepStaminaUp")
			.WithPriority(60)
			.WithDesiredEffect(beliefs["AgentIsRested"])
			.Build(),

			new EntityGoals.Builder("Wander")
			.WithPriority(40)
			.WithDesiredEffect(beliefs["AgentMoving"])
			.Build(),
		};
	}
	void SetupTimers()
	{
		statsTimer = new CountdownTimer(2f);
		statsTimer.OnTimerStop += () =>
		{
			UpdateStats();
			statsTimer.Start();
		};
		statsTimer.Start();

		basicAttackTimer = new CountdownTimer(2f);
		basicAttackTimer.OnTimerStart += () =>
		{
			basicAttackReady = false;
		};
		basicAttackTimer.OnTimerStop += () =>
		{
			basicAttackReady = true;
		};

		heavyAttackTimer = new CountdownTimer(10f);
		heavyAttackTimer.OnTimerStart += () =>
		{
			heavyAttackReady = false;
		};
		heavyAttackTimer.OnTimerStop += () =>
		{
			heavyAttackReady = true;
		};

		/*
		goalPriorityTimer = new CountdownTimer(2f);
		goalPriorityTimer.OnTimerStop += () =>
		{
			UpdateGoalPriorities();
			goalPriorityTimer.Start();
		};
		goalPriorityTimer.Start();
		*/
	}

	void UpdateStats()
	{
		currentHealth += InRangeOf(foodShack.position, 3f) ? 30 : -5;
		currentStamina += InRangeOf(restingPosition.position, 3f) ? 30 : -10;

		currentHealth = Mathf.Clamp(currentHealth, 0, 100);
		currentStamina = Mathf.Clamp(currentStamina, 0, 100);
	}
	void UpdateGoalPriorities()
	{
		foreach (EntityGoals goal in goals)
		{
			if (goal.Name == "KeepHealthUp")
				goal.UpdateGoalPriority(Mathf.Abs(maxHealth - currentHealth) * 1.25f);
			else if (goal.Name == "KeepStaminaUp")
				goal.UpdateGoalPriority(Mathf.Abs(maxStamina - currentStamina) * 1f);
		}
	}

	bool InRangeOf(Vector3 pos, float range) => Vector3.Distance(transform.position, pos) < range;

	void OnEnable() => chaseSensor.OnTargetChanged += HandleTargetChanged;
	void OnDisable() => chaseSensor.OnTargetChanged -= HandleTargetChanged;

	void HandleTargetChanged()
	{
		Debug.Log("Target changed, clearing current action and goal");
		// Force the planner to re-evaluate the plan
		currentAction = null;
		currentGoal = null;
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
