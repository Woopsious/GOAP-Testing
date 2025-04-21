using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class EntityAgent : MonoBehaviour
{
	[Header("Sensors")]
	[SerializeField] EntitySensor chaseSensor;
	[SerializeField] EntitySensor attackSensor;

	[Header("Known Locations")]
	[SerializeField] Transform restingPosition;
	[SerializeField] Transform foodShack;
	[SerializeField] Transform doorOnePosition;
	[SerializeField] Transform doorTwoPosition;

	NavMeshAgent navMeshAgent;
	//AnimationController animations;
	Rigidbody rb;

	[Header("Stats")]
	public float health = 100;
	public float stamina = 100;

	CountdownTimer statsTimer;

	GameObject target;
	Vector3 destination;

	public EntityGoals lastGoal;
	public EntityGoals currentGoal;
	public ActionPlan actionPlan;
	public EntityActions currentAction;

	public Dictionary<string, EntityBeliefs> beliefs;
	public HashSet<EntityActions> actions;
	public HashSet<EntityGoals> goals;

	public GoapFactory gFactory;
	IGoapPlanner gPlanner;

	void Awake()
	{
		navMeshAgent = GetComponent<NavMeshAgent>();
		//animations = GetComponent<AnimationController>();
		rb = GetComponent<Rigidbody>();
		rb.freezeRotation = true;

		gPlanner = new GoapPlanner();
	}

	void Start()
	{
		SetupTimers();
		SetupBeliefs();
		SetupActions();
		SetupGoals();

		Debug.LogError("beliefs count: " + beliefs.Count + " actions count: " + actions.Count + " goals count: " + goals.Count);
	}

	void SetupBeliefs()
	{
		beliefs = new Dictionary<string, EntityBeliefs>();
		BeliefFactory factory = new BeliefFactory(this, beliefs);

		factory.AddBelief("Nothing", () => false);

		factory.AddBelief("AgentIdle", () => !navMeshAgent.hasPath);
		factory.AddBelief("AgentMoving", () => navMeshAgent.hasPath);
		factory.AddBelief("AgentHealthLow", () => health < 30);
		factory.AddBelief("AgentIsHealthy", () => health >= 50);
		factory.AddBelief("AgentStaminaLow", () => stamina < 10);
		factory.AddBelief("AgentIsRested", () => stamina >= 50);

		factory.AddLocationBelief("AgentAtDoorOne", 3f, doorOnePosition);
		factory.AddLocationBelief("AgentAtDoorTwo", 3f, doorTwoPosition);
		factory.AddLocationBelief("AgentAtRestingPosition", 3f, restingPosition);
		factory.AddLocationBelief("AgentAtFoodShack", 3f, foodShack);

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
			.WithStrategy(new WanderStrategy(navMeshAgent, 10))
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

			new EntityActions.Builder("MoveToDoorOne")
			.WithStrategy(new MoveStrategy(navMeshAgent, () => doorOnePosition.position))
			.AddEffect(beliefs["AgentAtDoorOne"])
			.Build(),

			new EntityActions.Builder("MoveToDoorTwo")
			.WithStrategy(new MoveStrategy(navMeshAgent, () => doorTwoPosition.position))
			.AddEffect(beliefs["AgentAtDoorTwo"])
			.Build(),

			new EntityActions.Builder("MoveFromDoorOneToRestArea")
			.WithCost(2)
			.WithStrategy(new MoveStrategy(navMeshAgent, () => restingPosition.position))
			.AddPrecondition(beliefs["AgentAtDoorOne"])
			.AddEffect(beliefs["AgentAtRestingPosition"])
			.Build(),

			new EntityActions.Builder("MoveFromDoorTwoRestArea")
			.WithStrategy(new MoveStrategy(navMeshAgent, () => restingPosition.position))
			.AddPrecondition(beliefs["AgentAtDoorTwo"])
			.AddEffect(beliefs["AgentAtRestingPosition"])
			.Build(),

			new EntityActions.Builder("Rest")
			.WithStrategy(new IdleStrategy(5))
			.AddPrecondition(beliefs["AgentAtRestingPosition"])
			.AddEffect(beliefs["AgentIsRested"])
			.Build(),

			new EntityActions.Builder("ChasePlayer")
			.WithStrategy(new MoveStrategy(navMeshAgent, () => beliefs["PlayerInChaseRange"].Location))
			.AddPrecondition(beliefs["PlayerInChaseRange"])
			.AddEffect(beliefs["PlayerInAttackRange"])
			.Build(),

			new EntityActions.Builder("AttackPlayer")
			.WithStrategy(new AttackStrategy())
			.AddPrecondition(beliefs["PlayerInAttackRange"])
			.AddEffect(beliefs["AttackingPlayer"])
			.Build()
		};
	}

	void SetupGoals()
	{
		goals = new HashSet<EntityGoals>
		{
			new EntityGoals.Builder("KeepStaminaUp")
			.WithPriority(2)
			.WithDesiredEffect(beliefs["AgentIsRested"])
			.Build(),

			/*
			new EntityGoals.Builder("Chill Out")
			.WithPriority(1)
			.WithDesiredEffect(beliefs["Nothing"])
			.Build(),

			new EntityGoals.Builder("Wander")
			.WithPriority(1)
			.WithDesiredEffect(beliefs["AgentMoving"])
			.Build(),

			new EntityGoals.Builder("KeepHealthUp")
			.WithPriority(2)
			.WithDesiredEffect(beliefs["AgentIsHealthy"])
			.Build(),

			new EntityGoals.Builder("KeepStaminaUp")
			.WithPriority(2)
			.WithDesiredEffect(beliefs["AgentIsRested"])
			.Build(),

			new EntityGoals.Builder("SeekAndDestroy")
			.WithPriority(3)
			.WithDesiredEffect(beliefs["AttackingPlayer"])
			.Build()
			*/
		};
	}

	void SetupTimers()
	{
		statsTimer = new CountdownTimer(2f);
		statsTimer.OnTimerStop += () => {
			UpdateStats();
			statsTimer.Start();
		};
		statsTimer.Start();
	}

	// TODO move to stats system
	void UpdateStats()
	{
		stamina += InRangeOf(restingPosition.position, 3f) ? 20 : -10;
		health += InRangeOf(foodShack.position, 3f) ? 20 : -5;
		stamina = Mathf.Clamp(stamina, 0, 100);
		health = Mathf.Clamp(health, 0, 100);
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

	void Update()
	{
		statsTimer.Tick(Time.deltaTime);
		//animations.SetSpeed(navMeshAgent.velocity.magnitude);

		Debug.LogError("beliefs count: " + beliefs.Count + " actions count: " + actions.Count + " goals count: " + goals.Count);



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
}
