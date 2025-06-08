using System;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

// TODO Migrate Strategies, Beliefs, Actions and Goals to Scriptable Objects and create Node Editor for them
public interface IActionStrategy
{
	bool CanPerform { get; }
	bool Complete { get; }

	void Start()
	{
		// noop
	}

	void Update(float deltaTime)
	{
		// noop
	}

	void Stop()
	{
		// noop
	}
}

public class IdleStrategy : IActionStrategy
{
	public bool CanPerform => true; // Agent can always Idle
	public bool Complete { get; private set; }

	readonly CountdownTimer timer;

	public IdleStrategy(float duration)
	{
		timer = new CountdownTimer(duration);
		timer.OnTimerStart += () => Complete = false;
		timer.OnTimerStop += () => Complete = true;
	}

	public void Start() => timer.Start();
	public void Update(float deltaTime) => timer.Tick(deltaTime);
}

public class WanderStrategy : IActionStrategy
{
	readonly NavMeshAgent agent;
	readonly float wanderRadius;

	public bool CanPerform => !Complete;
	public bool Complete => agent.remainingDistance <= 2f && !agent.pathPending;

	public WanderStrategy(NavMeshAgent agent, float wanderRadius)
	{
		this.agent = agent;
		this.wanderRadius = wanderRadius;
	}

	public void Start()
	{
		for (int i = 0; i < 5; i++)
		{
			Vector3 randomPositon = Random.insideUnitSphere * wanderRadius;
			Vector3 randomDirection = new(randomPositon.x, 0, randomPositon.z);

			if (NavMesh.SamplePosition(agent.transform.position + randomDirection, out NavMeshHit hit, wanderRadius, 1))
			{
				agent.SetDestination(hit.position);
				return;
			}
		}
	}
}

public class MoveStrategy : IActionStrategy
{
	readonly EntityStats entity;
	readonly NavMeshAgent agent;
	readonly float minMoveDistance;
	readonly Func<Vector3> destination;

	public bool CanPerform => !Complete;
	public bool Complete => agent.remainingDistance <= minMoveDistance && !agent.pathPending;

	public MoveStrategy(NavMeshAgent agent, float minMoveDistance, Func<Vector3> destination)
	{
		entity = agent.GetComponent<EntityStats>();
		this.agent = agent;
		this.destination = destination;
		this.minMoveDistance = minMoveDistance;
	}

	public void Start() => agent.SetDestination(destination());
	public void Update(float deltaTime)
	{
		//Debug.LogError("move distance: " + agent.remainingDistance + " distance to meet: " + minMoveDistance);
	}
	public void Stop()
	{
		entity.entityBrain.UpdateRecievedHelpRequest(Vector3.zero);
		agent.SetDestination(agent.transform.position);
	}
}

public class FleeStrategy : IActionStrategy
{
	readonly NavMeshAgent agent;
	readonly Func<Vector3> destination;
	readonly float maxFleeDistance = 20f;

	public bool CanPerform => !Complete;
	public bool Complete => agent.remainingDistance <= 2f && !agent.pathPending;

	public FleeStrategy(NavMeshAgent agent, Func<Vector3> destination)
	{
		this.agent = agent;
		this.destination = destination;
	}

	public void Start()
	{
		Vector3 normDir = (agent.transform.position - destination()).normalized;
		normDir = Quaternion.AngleAxis(Random.Range(0, 59) - 30, Vector3.up) * normDir; //add slight zigzag
		Vector3 fleeDestination = agent.transform.position + (normDir * maxFleeDistance);
		agent.SetDestination(fleeDestination);
	}
	public void Stop() => agent.SetDestination(agent.transform.position);
}

public class InteractStrategy : IActionStrategy
{
	readonly EntityBrain entityBrain;
	readonly IEntityInteractStrategies interaction;

	readonly CountdownTimer timer;

	public bool CanPerform => true;
	public bool Complete => timer.IsFinished;

	public InteractStrategy(EntityBrain entityBrain, IEntityInteractStrategies interaction)
	{
		this.entityBrain = entityBrain;
		this.interaction = interaction;

		timer = new CountdownTimer(0);
	}

	public void Start()
	{
		if (!timer.IsFinished || timer.IsRunning) return;

		timer.Reset(interaction.GetInteractTimer(), entityBrain);
		timer.OnTimerStart += () => interaction.StartInteract(entityBrain.entityStats);
		timer.OnTimerStop += () => interaction.CompleteInteract(entityBrain.entityStats);
		timer.OnTimerCancel += () => interaction.CancelInteract(entityBrain.entityStats);
		timer.Start();
	}

	public void Update(float deltaTime) => timer.Tick(deltaTime);
	public void Stop() => timer.Cancel();
}

public class CallForHelpStrategy : IActionStrategy
{
	readonly EntityStats entity;
	readonly EntitySensor longRangeSensor;
	readonly int entitiesToTryAndFind;

	public bool CanPerform => entity.entityBrain.RequestHelpTimer.IsFinished;
	public bool Complete => entity.entityBrain.RequestHelpTimer.IsRunning;

	public CallForHelpStrategy(EntityStats entity, EntitySensor longRangeSensor, int entitiesToTryAndFind)
	{
		this.entity = entity;
		this.longRangeSensor = longRangeSensor;
		this.entitiesToTryAndFind = entitiesToTryAndFind;
	}

	public void Start() => RequestAvailableFriendliesForHelp();

	void RequestAvailableFriendliesForHelp()
	{
		int entitiesFoundToCall = 0;

		for (int i = 0; i < longRangeSensor.friendliesInRange.Count; i++)
		{
			if (longRangeSensor.friendliesInRange[i].entity.entityBrain.CanAnswerRequestHelp())
			{
				longRangeSensor.friendliesInRange[i].entity.entityBrain.UpdateRecievedHelpRequest(entity.transform.position);
				entitiesFoundToCall++;
				i++;
			}

            if (entitiesFoundToCall >= entitiesToTryAndFind)
				break;
		}

		entity.entityBrain.RequestHelpTimer.Start();
	}
}

public class MoveToAttackStrategy : IActionStrategy
{
	readonly NavMeshAgent agent;
	readonly EntityAttackData attackData;
	readonly Func<Vector3> destination;

	public bool CanPerform => !Complete;
	public bool Complete => agent.remainingDistance <= 2f && !agent.pathPending;

	public MoveToAttackStrategy(NavMeshAgent agent, Func<Vector3> destination, EntityAttackData attackData)
	{
		this.agent = agent;
		this.attackData = attackData;
		this.destination = destination;
	}

	void GetWithinAttackRangeMinMax()
	{
		float destinationDistance = Vector3.Distance(agent.transform.position, destination());
		Vector3 moveDestination = Vector3.zero;

		if (destinationDistance < attackData.attackMinRange)
		{
			Vector3 normDir = (agent.transform.position - destination()).normalized;
			normDir = Quaternion.AngleAxis(Random.Range(0, 59) - 30, Vector3.up) * normDir; //add slight zigzag
			moveDestination = agent.transform.position + (normDir * 20);
		}
		else if (destinationDistance > attackData.attackMaxRange)
			moveDestination = destination();
		else
			moveDestination = agent.transform.position;

		agent.SetDestination(moveDestination);
	}

	public void Start() => GetWithinAttackRangeMinMax();
	public void Stop() => agent.SetDestination(agent.transform.position);
}

public class AttackStrategy : IActionStrategy
{
	readonly EntityBrain entityBrain;
	readonly int attackToUse;
	readonly Func<TargetData> target;

	public bool CanPerform => target != null;
	public bool Complete => true;

	public AttackStrategy(EntityBrain entityBrain, int attackToUse, Func<TargetData> target)
	{
		this.entityBrain = entityBrain;
		this.attackToUse = attackToUse;
		this.target = target;
	}

	public void Start()
	{
		switch (attackToUse)
		{
			case 1:
				entityBrain.UseAttackOne();
				break;
			case 2:
				entityBrain.UseAttackTwo();
				break;
		}
	}
}

public class FindPoiStrategy : IActionStrategy
{
	readonly NavMeshAgent agent;
	readonly float wanderRadius;

	public bool CanPerform => !Complete;
	public bool Complete => agent.remainingDistance <= 2f && !agent.pathPending && !agent.pathPending;

	public FindPoiStrategy(NavMeshAgent agent, float wanderRadius)
	{
		this.agent = agent;
		this.wanderRadius = wanderRadius;
	}

	public void Start()
	{
		for (int i = 0; i < 5; i++)
		{
			Vector3 randomPositon = Random.insideUnitSphere * wanderRadius;
			Vector3 randomDirection = new(randomPositon.x, 0, randomPositon.z);

			if (NavMesh.SamplePosition(agent.transform.position + randomDirection, out NavMeshHit hit, wanderRadius, 1))
			{
				agent.SetDestination(hit.position);
				return;
			}
		}
	}
}