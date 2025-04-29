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
	[SerializeField] EntitySensor chaseSensor;
	[SerializeField] EntitySensor fleeSensor;
	[SerializeField] EntitySensor attackSensor;

	[Header("Known Locations")]
	[SerializeField] Transform foodShack;

	[Header("Stats")]
	public EntityTypes entityType;
	public float currentHealth;

	CountdownTimer statsTimer;
	[SerializeField] public CountdownTimer basicAttackTimer;
	[SerializeField] public CountdownTimer heavyAttackTimer;

	[Header("Attacks")]
	public bool allAttacksOnCooldown;
	public bool basicAttackReady;
	public bool heavyAttackReady;

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
		Initilize();
	}

	void Update()
	{
		TickAllTimers();

		if (!basicAttackReady && !heavyAttackReady)
			allAttacksOnCooldown = true;
		else allAttacksOnCooldown = false;

		CreateNewPlan();
	}

	void Initilize()
	{
		currentHealth = entityType.maxHealth;

		basicAttackReady = true;
		heavyAttackReady = true;

		chaseSensor.UpdateSensorSettings(entityType.chaseRange);
		fleeSensor.UpdateSensorSettings(entityType.fleeRange);
		attackSensor.UpdateSensorSettings(entityType.basicAttackRange); //atm ignore different ranges for basic/heavy attacks

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
		factory.AddBelief("AgentHealthLow", () => currentHealth < 40);
		factory.AddBelief("AgentIsHealthy", () => currentHealth >= 60);

		factory.AddLocationBelief("AgentAtFoodShack", 3f, foodShack);

		factory.AddSensorBelief("EntityInChaseRange", chaseSensor);
		factory.AddSensorBelief("EntityInFleeRange", fleeSensor);
		factory.AddBelief("FleeingFromEntity", () => false);

		factory.AddSensorBelief("EntityInAttackRange", attackSensor);
		factory.AddBelief("EntityNotInsideRangedRange", () => !fleeSensor.IsTargetInRange);

		factory.AddBelief("AllAttacksOnCooldown", () => allAttacksOnCooldown);
		factory.AddBelief("BasicAttackReady", () => basicAttackReady);
		factory.AddBelief("HeavyAttackReady", () => heavyAttackReady);

		factory.AddBelief("AttackingEntity", () => false); // Player can always be attacked, this will never become true
		factory.AddBelief("BasicAttack", () => false);
		factory.AddBelief("HeavyAttack", () => false);
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
			.WithStrategy(new MoveStrategy(navMeshAgent, entityType.basicAttackRange, () => beliefs["EntityInChaseRange"].Location))
			.AddPrecondition(beliefs["EntityInChaseRange"])
			.AddEffect(beliefs["AttackingEntity"])
			.Build(),

			new EntityActions.Builder("WaitForAttacks")
			.WithStrategy(new IdleStrategy(0.5f))
			.AddPrecondition(beliefs["AllAttacksOnCooldown"])
			.AddEffect(beliefs["AttackingEntity"])
			.Build(),

			new EntityActions.Builder("BasicAttack")
			.WithStrategy(new BasicAttackStrategy(this))
			.AddPrecondition(beliefs["EntityInAttackRange"])
			.AddPrecondition(beliefs["BasicAttackReady"])
			.AddEffect(beliefs["BasicAttack"])
			.Build(),

			new EntityActions.Builder("HeavyAttack")
			.WithStrategy(new HeavyAttackStrategy(this))
			.AddPrecondition(beliefs["EntityInAttackRange"])
			.AddPrecondition(beliefs["HeavyAttackReady"])
			.AddEffect(beliefs["HeavyAttack"])
			.Build()
		};
	}
	void SetupGoals()
	{
		goals = new HashSet<EntityGoals>
		{   
			new EntityGoals.Builder("HeavyAttack")
			.WithPriority(70)
			.WithDesiredEffect(beliefs["HeavyAttack"])
			.Build(),

			new EntityGoals.Builder("BasicAttack")
			.WithPriority(70)
			.WithDesiredEffect(beliefs["BasicAttack"])
			.Build(),

			new EntityGoals.Builder("WaitForAttacks")
			.WithPriority(60)
			.WithDesiredEffect(beliefs["AttackingEntity"])
			.Build(),

			new EntityGoals.Builder("FleeFromEntity")
			.WithPriority(50)
			.WithDesiredEffect(beliefs["FleeingFromEntity"])
			.Build(),

			new EntityGoals.Builder("ChaseEntity")
			.WithPriority(40)
			.WithDesiredEffect(beliefs["AttackingEntity"])
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
		statsTimer = new CountdownTimer(5f);
		statsTimer.OnTimerStop += () =>
		{
			UpdateStats();
			statsTimer.Start();
		};
		statsTimer.Start();

		basicAttackTimer = new CountdownTimer(entityType.basicAttackCooldown);
		basicAttackTimer.OnTimerStart += () =>
		{
			basicAttackReady = false;
		};
		basicAttackTimer.OnTimerStop += () =>
		{
			basicAttackReady = true;
		};

		heavyAttackTimer = new CountdownTimer(entityType.heavyAttackCooldown);
		heavyAttackTimer.OnTimerStart += () =>
		{
			heavyAttackReady = false;
		};
		heavyAttackTimer.OnTimerStop += () =>
		{
			heavyAttackReady = true;
		};
	}

	void UpdateStats()
	{
		currentHealth += InRangeOf(foodShack.position, 3f) ? 30 : -5;
		currentHealth = Mathf.Clamp(currentHealth, 0, 100);
	}

	void TickAllTimers()
	{
		statsTimer.Tick(Time.deltaTime, false);
		basicAttackTimer.Tick(Time.deltaTime, false);
		heavyAttackTimer.Tick(Time.deltaTime, false);
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
