using System;
using UnityEngine;
using UnityEngine.AI;

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
	float minMoveDistanceToSatisfy;
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
	public void Stop() => agent.ResetPath();
}

public class FleeStrategy : IActionStrategy
{
	readonly NavMeshAgent agent;
	readonly Func<Vector3> destination;

	public bool CanPerform => !Complete;
	public bool Complete => agent.remainingDistance <= 2f && !agent.pathPending;

	public FleeStrategy(NavMeshAgent agent, Func<Vector3> destination)
	{
		this.agent = agent;
		this.destination = destination;
	}

	public void Start()
	{
		Vector3 fleeDestination = agent.transform.position - (destination() - agent.transform.position);
		fleeDestination *= 0.8f; //stop it moving too far away
		agent.SetDestination(fleeDestination);
	}
	public void Stop() => agent.ResetPath();
}

public class BasicAttackStrategy : IActionStrategy
{
	EntityAgent agent;

	public bool CanPerform => true; // Agent can always attack
	public bool Complete { get; private set; }

	readonly CountdownTimer timer;

	public BasicAttackStrategy(EntityAgent agent)
	{
		this.agent = agent;
		timer = new CountdownTimer(2f);
		timer.OnTimerStart += () => Complete = false;
		timer.OnTimerStop += () => Complete = true;
	}

	public void Start()
	{
		agent.basicAttackTimer.Start();
		timer.Start();
	}

public void Update(float deltaTime) => timer.Tick(deltaTime, false);
}

public class HeavyAttackStrategy : IActionStrategy
{
	EntityAgent agent;

	public bool CanPerform => true; // Agent can always attack
	public bool Complete { get; private set; }

	readonly CountdownTimer timer;

	public HeavyAttackStrategy(EntityAgent agent)
	{
		this.agent = agent;
		timer = new CountdownTimer(2f);
		timer.OnTimerStart += () => Complete = false;
		timer.OnTimerStop += () => Complete = true;
	}

	public void Start()
	{
		agent.heavyAttackTimer.Start();
		timer.Start();
	}

	public void Update(float deltaTime) => timer.Tick(deltaTime, false);
}