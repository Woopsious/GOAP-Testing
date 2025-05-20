using System;
using Unity.Multiplayer.Center.Common.Analytics;
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
	public void Update(float deltaTime) => timer.Tick(deltaTime, false);
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
	readonly NavMeshAgent agent;
	readonly float minMoveDistanceToSatisfy;
	readonly Func<Vector3> destination;

	public bool CanPerform => !Complete;
	public bool Complete => agent.remainingDistance <= minMoveDistanceToSatisfy && !agent.pathPending;

	public MoveStrategy(NavMeshAgent agent, Func<Vector3> destination)
	{
		this.agent = agent;
		this.destination = destination;
	}

	public MoveStrategy(NavMeshAgent agent, float minMoveDistance, Func<Vector3> destination)
	{
		this.agent = agent;
		this.destination = destination;
		minMoveDistanceToSatisfy = GetMinMoveDistanceToSatisfy(minMoveDistance);
	}

	float GetMinMoveDistanceToSatisfy(float minMoveDistance)
	{
		minMoveDistance *= 0.75f;
		return minMoveDistance;
	}

	public void Start() => agent.SetDestination(destination());
	public void Stop() => agent.SetDestination(agent.transform.position);
}

public class InteractStrategy : IActionStrategy
{
	readonly EntityBrain entityBrain;
	readonly IEntityInteractStrategies interaction;

	public InteractType interactType;
	public enum InteractType
	{
		poiController
	}

	CountdownTimer timer;

	public bool CanPerform => !Complete;
	public bool Complete => false;

	public InteractStrategy(EntityBrain entityBrain, IEntityInteractStrategies interaction, InteractType interactType)
	{
		this.entityBrain = entityBrain;
		this.interaction = interaction;
		this.interactType = interactType;
	}

	public void Start()
	{
		if (interactType == InteractType.poiController)
		{
			SetupPoiInteraction();
		}
	}

	//interact type setups
	void SetupPoiInteraction()
	{
		interaction.SetInteractType(entityBrain.chaseTarget.poiController);

		timer = new CountdownTimer(interaction.GetInteractTimer());
		timer.OnTimerStart += () => interaction.StartInteract(entityBrain.entityStats);
		timer.OnTimerStop += () => interaction.CompleteInteract(entityBrain.entityStats);
		timer.OnTimerCancel += () => interaction.CancelInteract(entityBrain.entityStats);
		timer.Start();
	}

	public void Update(float deltaTime) => timer.Tick(deltaTime, false);
	public void Stop() => timer.Cancel();
}

public class MoveIntoAttackRange : IActionStrategy
{
	readonly NavMeshAgent agent;
	readonly EntityAttackData attackData;
	readonly Func<Vector3> destination;

	public bool CanPerform => !Complete;
	public bool Complete => agent.remainingDistance <= 2f && !agent.pathPending;

	public MoveIntoAttackRange(NavMeshAgent agent, Func<Vector3> destination, EntityAttackData attackData)
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
		normDir = Quaternion.AngleAxis(Random.Range(0, 59) -30, Vector3.up) * normDir; //add slight zigzag
		Vector3 fleeDestination = agent.transform.position + (normDir * maxFleeDistance);
		agent.SetDestination(fleeDestination);
	}
	public void Stop() => agent.SetDestination(agent.transform.position);
}

public class BasicAttackStrategy : IActionStrategy
{
	readonly EntityBrain entityBrain;
	readonly int attackToUse;

	public bool CanPerform => true; // Agent can always attack
	public bool Complete { get; private set; }

	readonly CountdownTimer timer;

	public BasicAttackStrategy(EntityBrain entityBrain, int attackToUse)
	{
		this.entityBrain = entityBrain;
		this.attackToUse = attackToUse;
		timer = new CountdownTimer(0.1f);
		timer.OnTimerStart += () => Complete = false;
		timer.OnTimerStop += () => Complete = true;
	}

	public void Start()
	{
		switch (attackToUse)
		{
			case 1:
				entityBrain.attackOneTimer.Start();
				break;
			case 2:
				entityBrain.attackTwoTimer.Start();
				break;
		}

		timer.Start();
	}

	public void Update(float deltaTime) => timer.Tick(deltaTime, false);
}

public class FindPoiStrategy : IActionStrategy
{
	readonly EntityBrain entityBrain;
	readonly NavMeshAgent agent;
	readonly float wanderRadius;

	public bool CanPerform => !Complete;
	public bool Complete => agent.remainingDistance <= 2f && !agent.pathPending || entityBrain.beliefs["FoundCapturablePoi"].Evaluate();

	public FindPoiStrategy(EntityBrain entity, NavMeshAgent agent, float wanderRadius)
	{
		this.entityBrain = entity;
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