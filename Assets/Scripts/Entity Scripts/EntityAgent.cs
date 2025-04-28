using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class EntityAgent : MonoBehaviour
{
	NavMeshAgent navMeshAgent;
	Rigidbody rb;

	[Header("Sensors")]
	[SerializeField] EntitySensor fleeSensor;
	[SerializeField] EntitySensor chaseSensor;
	[SerializeField] EntitySensor meleeAttackSensor;
	[SerializeField] EntitySensor rangedAttackSensor;

	[Header("Known Locations")]
	[SerializeField] Transform restingPosition;
	[SerializeField] Transform foodShack;

	[Header("Stats")]
	public EntityTypes entityType;
	public float currentHealth;
	public float currentStamina;

	CountdownTimer statsTimer;
	CountdownTimer goalPriorityTimer;
	[SerializeField] public CountdownTimer basicAttackTimer;
	[SerializeField] public CountdownTimer heavyAttackTimer;
	[SerializeField] public CountdownTimer RangedAttackTimer;

	[Header("Attacks")]
	public bool allAttacksOnCooldown;
	public bool basicAttackReady;
	public bool heavyAttackReady;
	public bool rangedAttackReady;

	public GameObject target;

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
		if (entityType == null)
			Debug.LogError("Entity Type not set for gameobject: " + gameObject.name);
		else if (entityType.team == EntityTypes.EntityTeam.redTeam)
			GetComponent<MeshRenderer>().sharedMaterial = redTeamMaterial;
		else if (entityType.team == EntityTypes.EntityTeam.greenTeam)
			GetComponent<MeshRenderer>().sharedMaterial = greenTeamMaterial;

		navMeshAgent = GetComponent<NavMeshAgent>();
		rb = GetComponent<Rigidbody>();
		rb.freezeRotation = true;

		gPlanner = new GoapPlanner();
	}

	void Start()
	{
		currentHealth = entityType.maxHealth;
		currentStamina = entityType.maxStamina;

		basicAttackReady = true;
		heavyAttackReady = true;
		rangedAttackReady = true;

		SetupBeliefs();
		SetupActions();
		SetupGoals();
		SetupTimers();
	}

	void Update()
	{
		TickAllTimers();

		if (!basicAttackReady && !heavyAttackReady && !rangedAttackReady)
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

		factory.AddLocationBelief("AgentAtRestingPosition", 3f, restingPosition);
		factory.AddLocationBelief("AgentAtFoodShack", 3f, foodShack);

		factory.AddSensorBelief("PlayerInChaseRange", chaseSensor);
		factory.AddSensorBelief("PlayerInFleeRange", fleeSensor);
		factory.AddBelief("FleeingFromPlayer", () => false);

		factory.AddSensorBelief("PlayerInMeleeRange", meleeAttackSensor);
		factory.AddSensorBelief("PlayerInRangedRange", rangedAttackSensor);
		factory.AddBelief("PlayerNotInsideRangedRange", () => !fleeSensor.IsTargetInRange);

		factory.AddBelief("AllAttacksOnCooldown", () => allAttacksOnCooldown);
		factory.AddBelief("BasicAttackReady", () => basicAttackReady);
		factory.AddBelief("HeavyAttackReady", () => heavyAttackReady);
		factory.AddBelief("RangedAttackReady", () => rangedAttackReady);

		factory.AddBelief("AttackingPlayer", () => false); // Player can always be attacked, this will never become true
		factory.AddBelief("MeleeAttackingPlayer", () => false);
		factory.AddBelief("RangeAttackingPlayer", () => false);
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
			.AddEffect(beliefs["AttackingPlayer"])
			.Build(),

			new EntityActions.Builder("WaitForAttacks")
			.WithStrategy(new IdleStrategy(0.5f))
			.AddPrecondition(beliefs["AllAttacksOnCooldown"])
			.AddEffect(beliefs["AttackingPlayer"])
			.Build(),

			new EntityActions.Builder("BasicAttackPlayer")
			.WithStrategy(new BasicAttackStrategy(this))
			.AddPrecondition(beliefs["PlayerInMeleeRange"])
			.AddPrecondition(beliefs["BasicAttackReady"])
			.AddEffect(beliefs["MeleeAttackingPlayer"])
			.Build(),

			new EntityActions.Builder("HeavyAttackPlayer")
			.WithStrategy(new HeavyAttackStrategy(this))
			.AddPrecondition(beliefs["PlayerInMeleeRange"])
			.AddPrecondition(beliefs["HeavyAttackReady"])
			.AddEffect(beliefs["MeleeAttackingPlayer"])
			.Build(),

			new EntityActions.Builder("RangeAttackPlayer")
			.WithStrategy(new RangedAttackStrategy(this))
			.AddPrecondition(beliefs["PlayerInRangedRange"])
			.AddPrecondition(beliefs["PlayerNotInsideRangedRange"])
			.AddPrecondition(beliefs["RangedAttackReady"])
			.AddEffect(beliefs["RangeAttackingPlayer"])
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
			new EntityGoals.Builder("RangeAttackPlayer")
			.WithPriority(80)
			.WithDesiredEffect(beliefs["RangeAttackingPlayer"])
			.Build(),

			new EntityGoals.Builder("MeleeAttackPlayer")
			.WithPriority(70)
			.WithDesiredEffect(beliefs["MeleeAttackingPlayer"])
			.Build(),

			new EntityGoals.Builder("FleeFromPlayer")
			.WithPriority(50)
			.WithDesiredEffect(beliefs["FleeingFromPlayer"])
			.Build(),

			new EntityGoals.Builder("ChasePlayer")
			.WithPriority(40)
			.WithDesiredEffect(beliefs["AttackingPlayer"])
			.Build(),

			new EntityGoals.Builder("KeepHealthUp")
			.WithPriority(20)
			.WithDesiredEffect(beliefs["AgentIsHealthy"])
			.Build(),

			new EntityGoals.Builder("KeepStaminaUp")
			.WithPriority(10)
			.WithDesiredEffect(beliefs["AgentIsRested"])
			.Build(),

			new EntityGoals.Builder("Wander")
			.WithPriority(5)
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

		RangedAttackTimer = new CountdownTimer(5f);
		RangedAttackTimer.OnTimerStart += () =>
		{
			rangedAttackReady = false;
		};
		RangedAttackTimer.OnTimerStop += () =>
		{
			rangedAttackReady = true;
		};
	}
	void UpdateStats()
	{
		currentHealth += InRangeOf(foodShack.position, 3f) ? 30 : -5;
		currentStamina += InRangeOf(restingPosition.position, 3f) ? 30 : -10;

		currentHealth = Mathf.Clamp(currentHealth, 0, 100);
		currentStamina = Mathf.Clamp(currentStamina, 0, 100);
	}

	void TickAllTimers()
	{
		statsTimer.Tick(Time.deltaTime, false);
		basicAttackTimer.Tick(Time.deltaTime, false);
		heavyAttackTimer.Tick(Time.deltaTime, false);
		RangedAttackTimer.Tick(Time.deltaTime, false);
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
