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
	readonly Func<Vector3> destination;

	public bool CanPerform => !Complete;
	public bool Complete => agent.remainingDistance <= 2f && !agent.pathPending;

	public MoveStrategy(NavMeshAgent agent, Func<Vector3> destination)
	{
		this.agent = agent;
		this.destination = destination;
	}

	public void Start() => agent.SetDestination(destination());
	public void Stop() => agent.ResetPath();
}

public class LightAttackStrategy : IActionStrategy
{
	EntityAgent agent;

	public bool CanPerform => !onCooldown; // Agent can always attack
	public bool Complete { get; private set; }

	readonly CountdownTimer timer;

	readonly CountdownTimer cooldownTimer;
	bool onCooldown;
	//readonly AnimationController animations;

	public LightAttackStrategy(EntityAgent agent)
	{
		this.agent = agent;

		//this.animations = animations;
		timer = new CountdownTimer(2);
		timer.OnTimerStart += () => Complete = false;
		timer.OnTimerStop += () => Complete = true;

		cooldownTimer = new CountdownTimer(3);
		cooldownTimer.OnTimerStart += () => onCooldown = false;
		cooldownTimer.OnTimerStop += () => onCooldown = true;
	}

	public void Start()
	{
		timer.Start();
		cooldownTimer.Start();
		//animations.Attack();
	}

	public void Update(float deltaTime)
	{
		timer.Tick(deltaTime);
		cooldownTimer.Tick(deltaTime);
	}
}

public class HeavyAttackStrategy : IActionStrategy
{
	EntityAgent agent;

	public bool CanPerform => !onCooldown; // Agent can always attack
	public bool Complete { get; private set; }

	readonly CountdownTimer timer;

	readonly CountdownTimer cooldownTimer;
	bool onCooldown;
	//readonly AnimationController animations;

	public HeavyAttackStrategy(EntityAgent agent)
	{
		this.agent = agent;

		//this.animations = animations;
		timer = new CountdownTimer(2);
		timer.OnTimerStart += () => Complete = false;
		timer.OnTimerStop += () => Complete = true;

		cooldownTimer = new CountdownTimer(10);
		cooldownTimer.OnTimerStart += () => onCooldown = false;
		cooldownTimer.OnTimerStop += () => onCooldown = true;
	}

	public void Start()
	{
		timer.Start();
		cooldownTimer.Start();
		//animations.Attack();
	}

	public void Update(float deltaTime)
	{
		timer.Tick(deltaTime);
		cooldownTimer.Tick(deltaTime);
	}
}
