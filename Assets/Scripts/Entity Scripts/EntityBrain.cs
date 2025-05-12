using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using static EntitySensor;
using static EntityData;
using System;

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
		foodShack = GameManager.instance.foodShack.transform;

		if (foodShack == null)
			Debug.LogError("No Global Food Shack Location Set");

		attackOneReady = true;
		attackTwoReady = true;

		SetupSensors();

		SetupBrainType();

		//SetupBeliefs();
		//SetupActions();
		//SetupGoals();

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

	void SetupBrainType()
	{
		EntitySensor[] sensors = new EntitySensor[4];
		sensors[0] = chaseSensor;
		sensors[1] = fleeSensor;
		sensors[2] = attackSensorOne;
		sensors[3] = attackSensorTwo;

		Transform[] knownLocations = new Transform[1];
		knownLocations[0] = foodShack;

		if (entityStats._Data.type == EntityType.combat)
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
		else if (entityStats._Data.type == EntityType.worker)
		{
			entityBrainStrategy = new WorkerBrainStrategy(this, entityStats, navMeshAgent, sensors, knownLocations);

			beliefs = entityBrainStrategy.SetupBeliefs();
			actions = entityBrainStrategy.SetupActions();
			goals = entityBrainStrategy.SetupGoals();
		}
	}

	void SetupTimers()
	{
		if (entityStats._Data.type != EntityType.combat) return; //workers have no attacks atm

		attackOneTimer = new CountdownTimer(entityStats._Data.attackData[0].attackCooldown);
		attackOneTimer.OnTimerStart += () =>
		{
			attackOneReady = false;
			UseAttackOne();

			Debug.LogError("attack timer one start");
		};
		attackOneTimer.OnTimerStop += () =>
		{
			attackOneReady = true;

			Debug.LogError("attack timer one stop");
		};

		attackTwoTimer = new CountdownTimer(entityStats._Data.attackData[1].attackCooldown);
		attackTwoTimer.OnTimerStart += () =>
		{
			attackTwoReady = false;
			UseAttackTwo();

			Debug.LogError("attack timer two start");
		};
		attackTwoTimer.OnTimerStop += () =>
		{
			attackTwoReady = true;

			Debug.LogError("attack timer two stop");
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
		if (entityStats._Data.team == EntityTeam.redTeam)
		{
			attackOneTimer?.Tick(Time.deltaTime, true);
			attackTwoTimer?.Tick(Time.deltaTime, true);
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

	public bool InRangeOf(Vector3 pos, float range) => Vector3.Distance(transform.position, pos) < range;
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
