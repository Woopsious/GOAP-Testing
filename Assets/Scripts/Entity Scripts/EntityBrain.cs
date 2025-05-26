using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using static EntitySensor;
using static EntityData;
using System;
using System.Collections;

public class EntityBrain : MonoBehaviour
{
	[HideInInspector] public EntityStats entityStats;
	NavMeshAgent navMeshAgent;
	Rigidbody rb;

	[Header("Sensors")]
	[SerializeField] EntitySensor fleeSensor;
	[SerializeField] EntitySensor chaseSensor;
	[SerializeField] EntitySensor targetSensorOne;
	[SerializeField] EntitySensor targetSensorTwo;

	[Header("Known Locations")]
	public Transform HomeBase { get; private set; }
	public PoIController[] Pois;

	[Header("Attacks")]
	public bool allAttacksOnCooldown;
	public bool attackOneReady;
	public bool attackTwoReady;

	public CountdownTimer attackOneTimer;
	public CountdownTimer attackTwoTimer;

	[Header("Entity Targets")]
	public TargetData chaseTarget;
	public TargetData targetOne;
	public TargetData targetTwo;

	public EntityGoals lastGoal;
	public EntityGoals currentGoal;
	public ActionPlan actionPlan;
	public EntityActions currentAction;

	IEntityBrainStrategies entityBrainStrategy;

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
		if (entityStats._Data.team == EntityTeam.redTeam)
			HomeBase = GameManager.instance.redTeamHomeBase.transform;
		else if (entityStats._Data.team == EntityTeam.greenTeam)
			HomeBase = GameManager.instance.greenTeamHomeBase.transform;
		else
			Debug.LogError("No Home Base Location Set");

		attackOneReady = true;
		attackTwoReady = true;

		navMeshAgent.speed = entityStats._Data.speed;
		navMeshAgent.angularSpeed = entityStats._Data.angularSpeed;
		navMeshAgent.acceleration = entityStats._Data.acceleration;
		navMeshAgent.stoppingDistance = entityStats._Data.stoppingDistance;

		SetupSensors();
		SetupBrainType();
		SetupTimers();
	}

	void SetupSensors()
	{
		if (entityStats._Data.brainType == EntityBrainType.combat)
		{
			float fleeRange = 1;
			foreach (EntityAttackData attackData in entityStats._Data.attackData)
			{
				if (attackData.attackMinRange > fleeRange)
					fleeRange = attackData.attackMinRange;
			}

			fleeSensor.UpdateSensorSettings(SensorType.flee, fleeRange);
			chaseSensor.UpdateSensorSettings(SensorType.chase, entityStats._Data.chaseRange);

			targetSensorOne.UpdateSensorSettings(SensorType.attackSensorOne, entityStats._Data.attackData[0]);
			targetSensorTwo.UpdateSensorSettings(SensorType.attackSensorTwo, entityStats._Data.attackData[1]);
		}
        else if (entityStats._Data.brainType == EntityBrainType.worker)
        {
			fleeSensor.UpdateSensorSettings(SensorType.flee, entityStats._Data.fleeRange);
			chaseSensor.UpdateSensorSettings(SensorType.chase, entityStats._Data.chaseRange);

			targetSensorOne.UpdateSensorSettings(SensorType.closestFriendlyPoi, entityStats._Data.chaseRange);
			targetSensorTwo.UpdateSensorSettings(SensorType.closestEnemyPoi, entityStats._Data.chaseRange);
		}
    }
	void SetupBrainType()
	{
		EntitySensor[] sensors = new EntitySensor[4];
		sensors[0] = fleeSensor;
		sensors[1] = chaseSensor;
		sensors[2] = targetSensorOne;
		sensors[3] = targetSensorTwo;

		Transform[] knownLocations = new Transform[1];
		knownLocations[0] = HomeBase;

		if (entityStats._Data.brainType == EntityBrainType.combat)
		{
			Func<bool>[] attackBools = new Func<bool>[3];
			attackBools[0] = () => allAttacksOnCooldown;
			attackBools[1] = () => attackOneReady;
			attackBools[2] = () => attackTwoReady;

			entityBrainStrategy = new CombatBrainStrategy(this, entityStats, navMeshAgent, sensors, knownLocations, attackBools);

			beliefs = entityBrainStrategy.SetupBeliefs();
			actions = entityBrainStrategy.SetupActions();
			goals = entityBrainStrategy.SetupGoals();
		}
		else if (entityStats._Data.brainType == EntityBrainType.worker)
		{
			entityBrainStrategy = new WorkerBrainStrategy(this, entityStats, navMeshAgent, sensors, knownLocations);

			beliefs = entityBrainStrategy.SetupBeliefs();
			actions = entityBrainStrategy.SetupActions();
			goals = entityBrainStrategy.SetupGoals();
		}
	}
	void SetupTimers()
	{
		if (entityStats._Data.brainType != EntityBrainType.combat) return; //workers have no attacks atm

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
		targetOne.entity.RecieveDamage(entityStats._Data.attackData[0].attackDamage);
	}
	void UseAttackTwo()
	{
		targetTwo.entity.RecieveDamage(entityStats._Data.attackData[1].attackDamage);
	}

	void TickAllTimers()
	{
		if (entityStats._Data.team == EntityTeam.redTeam)
		{
			attackOneTimer?.Tick(Time.deltaTime, false);
			attackTwoTimer?.Tick(Time.deltaTime, false);
		}
        else
        {
			attackOneTimer?.Tick(Time.deltaTime, false);
			attackTwoTimer?.Tick(Time.deltaTime, false);
		}
	}

	void OnEnable()
	{
		chaseSensor.OnTargetChanged += OnTargetChanges;
		targetSensorOne.OnTargetChanged += OnTargetChanges;
		targetSensorTwo.OnTargetChanged += OnTargetChanges;
	}
	void OnDisable()
	{
		chaseSensor.OnTargetChanged -= OnTargetChanges;
		targetSensorOne.OnTargetChanged -= OnTargetChanges;
		targetSensorTwo.OnTargetChanged -= OnTargetChanges;
	}

	void OnTargetChanges(TargetData target, SensorType sensorType)
	{
		switch (sensorType)
		{
			case SensorType.chase:
			// Force the planner to re-evaluate the plan
			chaseTarget = target;
			currentAction = null;
			currentGoal = null;
			break;

			case SensorType.attackSensorOne:
			targetOne = target;
			break;

			case SensorType.attackSensorTwo:
			targetTwo = target;
			break;

			case SensorType.closestFriendlyPoi:
			if (targetOne.obj != target.obj)//recalc goal when poi changes
			{
				targetOne = target;
				currentAction = null;
				currentGoal = null;
			}
			break;
			case SensorType.closestEnemyPoi:
			if (targetTwo.obj != target.obj)//recalc goal when poi changes
			{
				targetTwo = target;
				currentAction = null;
				currentGoal = null;
			}
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

	public bool InRangeOf(Vector3 pos, float range) => Vector3.Distance(transform.position, pos) < range;
}
