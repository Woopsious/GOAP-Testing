using System;
using System.Xml;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.GraphicsBuffer;
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
			Vector3 randomPositon = UnityEngine.Random.insideUnitSphere * wanderRadius;
			Vector3 randomDirection = new Vector3(randomPositon.x, 0, randomPositon.z);
			NavMeshHit hit;

			if (NavMesh.SamplePosition(agent.transform.position + randomDirection, out hit, wanderRadius, 1))
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

		Debug.LogError("destination: " + destination());

		if (destinationDistance >= attackData.attackMinRange && destinationDistance <= attackData.attackMaxRange)
		{
			Debug.LogError("inside attack range");
		}
		else if (destinationDistance < attackData.attackMinRange)
		{
			Debug.LogError("TOO CLOSE");

			Vector3 normDir = (agent.transform.position - destination()).normalized;
			normDir = Quaternion.AngleAxis(Random.Range(0, 59) - 30, Vector3.up) * normDir; //add slight zigzag
			Vector3 fleeDestination = agent.transform.position + (normDir * 10);
			agent.SetDestination(fleeDestination);
		}
		else if (destinationDistance > attackData.attackMaxRange)
		{
			Debug.LogError("TOO FAR");

			agent.SetDestination(destination());
		}
	}

	public void Start() => GetWithinAttackRangeMinMax();
	public void Update()
	{

	}
	public void Stop() => agent.SetDestination(agent.transform.position);
}

public class FleeStrategy : IActionStrategy
{
	readonly NavMeshAgent agent;
	readonly Func<Vector3> destination;
	readonly float maxFleeDistance = 10f;

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