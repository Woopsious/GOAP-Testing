using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using static EntitySensor;
using static EntityData;
using System;

public class EntityBrain : MonoBehaviour
{
	[HideInInspector] public EntityStats entityStats;
	NavMeshAgent navMeshAgent;
	Rigidbody rb;

	[Header("Sensors")]
	[SerializeField] EntitySensor chaseSensor;
	[SerializeField] EntitySensor longRangeSensor;
	[SerializeField] EntitySensor poiSensor;

	[Header("Known Locations")]
	public Transform HomeBase { get; private set; }

	[Header("Attacks")]
	bool allAttacksOnCooldown;
	bool attackOneReady;
	bool attackTwoReady;

	//Request/Answer Help
	public Vector3 RequestHelpOriginPos { get; private set; }

	//timers
	public CountdownTimer RequestHelpTimer { get; private set; }

	CountdownTimer attackOneTimer;
	CountdownTimer attackTwoTimer;

	//GOAP
	public EntityGoals lastGoal;
	public EntityGoals currentGoal;
	public ActionPlan actionPlan;
	public EntityActions currentAction;

	IEntityBrainStrategies entityBrainStrategy;

	public int answerRequestHelpGoalPriority;

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

	void OnEnable()
	{
		chaseSensor.OnTargetChanged += OnTargetChanges;
		longRangeSensor.OnTargetChanged += OnTargetChanges;
		poiSensor.OnTargetChanged += OnTargetChanges;
	}
	void OnDisable()
	{
		chaseSensor.OnTargetChanged -= OnTargetChanges;
		longRangeSensor.OnTargetChanged -= OnTargetChanges;
		poiSensor.OnTargetChanged -= OnTargetChanges;
	}

	void OnTargetChanges(SensorType sensorType)
	{
		ResetPlan();
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
			HomeBase = AiDirector.instance.redTeamHomeBase.transform;
		else if (entityStats._Data.team == EntityTeam.greenTeam)
			HomeBase = AiDirector.instance.greenTeamHomeBase.transform;
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
		chaseSensor.UpdateSensorSettings(entityStats._Data.chaseRange, entityStats._Data.attackData);
		longRangeSensor.UpdateSensorSettings(entityStats._Data.chaseRange * 2f, entityStats._Data.attackData);
		poiSensor.UpdateSensorSettings(entityStats._Data.chaseRange, entityStats._Data.attackData);
    }
	void SetupBrainType()
	{
		EntitySensor[] sensors = new EntitySensor[3];
		sensors[0] = chaseSensor;
		sensors[1] = longRangeSensor;
		sensors[2] = poiSensor;

		Transform[] knownLocations = new Transform[1];
		knownLocations[0] = HomeBase;

		if (entityStats._Data.brainType == EntityBrainType.combat)
		{
			Func<bool>[] attackBools = new Func<bool>[3];
			attackBools[0] = () => allAttacksOnCooldown;
			attackBools[1] = () => attackOneReady;
			attackBools[2] = () => attackTwoReady;

			EntityAttackData[] attackData = new EntityAttackData[2];
			attackData[0] = entityStats._Data.attackData[0];
			attackData[1] = entityStats._Data.attackData[1];

			entityBrainStrategy = new CombatBrainStrategy(this, entityStats, navMeshAgent, sensors, knownLocations, attackData, attackBools);

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
		RequestHelpTimer = new CountdownTimer(5f);

		if (entityStats._Data.brainType != EntityBrainType.combat) return; //workers have no attacks atm

		attackOneTimer = new CountdownTimer(entityStats._Data.attackData[0].attackCooldown);
		attackOneTimer.OnTimerStart += () => UseAttackOne();
		attackOneTimer.OnTimerStop += () => attackOneReady = true;

		attackTwoTimer = new CountdownTimer(entityStats._Data.attackData[1].attackCooldown);
		attackTwoTimer.OnTimerStart += () => UseAttackTwo();
		attackTwoTimer.OnTimerStop += () => attackTwoReady = true;
	}

	//attacks
	public void UseAttackOne()
	{
		attackOneReady = false;
		beliefs["TargetInAttackOneRange"].TargetData.entity.RecieveDamage(entityStats._Data.attackData[0].attackDamage);
		attackOneTimer.Start();
	}
	public void UseAttackTwo()
	{
		attackTwoReady = false;
		beliefs["TargetInAttackTwoRange"].TargetData.entity.RecieveDamage(entityStats._Data.attackData[1].attackDamage);
		attackTwoTimer.Start();
	}

	//request/answer help funcs
	public bool EntityNeedsHelp()
	{
		if (chaseSensor.targetsInRange.Count > chaseSensor.friendliesInRange.Count + 1)
			return true;
		else
			return false;
	}
	public bool CanAnswerRequestHelp()
	{
		if (RequestHelpOriginPos != Vector3.zero) return false; //has existing call for help
		if (currentGoal == null || currentGoal.Priority < answerRequestHelpGoalPriority)
			return true;
		else
			return false;
	}
	public void UpdateRecievedHelpRequest(Vector3 RequestHelpOriginPos)
	{
		this.RequestHelpOriginPos = RequestHelpOriginPos;
		ResetPlan();
	}

	void TickAllTimers()
	{
		if (entityStats._Data.team == EntityTeam.redTeam)
		{
			attackOneTimer?.Tick(Time.deltaTime);
			attackTwoTimer?.Tick(Time.deltaTime);
		}
        else
        {
			attackOneTimer?.Tick(Time.deltaTime);
			attackTwoTimer?.Tick(Time.deltaTime);
		}
	}

	//GOAP
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
	void ResetPlan()
	{
		currentAction = null;
		currentGoal = null;
	}

	public bool InRangeOf(Vector3 pos, float range) => Vector3.Distance(transform.position, pos) < range;
}
