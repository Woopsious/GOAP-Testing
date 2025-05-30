using System;
using System.Collections.Generic;
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
	readonly NavMeshAgent agent;
	readonly float minMoveDistance;
	readonly Func<Vector3> destination;

	public bool CanPerform => !Complete;
	public bool Complete => agent.remainingDistance <= minMoveDistance && !agent.pathPending;

	public MoveStrategy(NavMeshAgent agent, float minMoveDistance, Func<Vector3> destination)
	{
		this.agent = agent;
		this.destination = destination;
		this.minMoveDistance = minMoveDistance;
	}

	public void Start() => agent.SetDestination(destination());
	public void Update(float deltaTime)
	{
		//Debug.LogError("move distance: " + agent.remainingDistance + " distance to meet: " + minMoveDistance);
	}
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
	readonly int entitiesToTryAndFind;
	readonly float maxSearchDistance;
	public bool CanPerform => true;
	public bool Complete => finishedCallForHelp;

	bool finishedCallForHelp;

	public CallForHelpStrategy(EntityStats entity, int entitiesToTryAndFind, float maxSearchDistance)
	{
		this.entity = entity;
		this.entitiesToTryAndFind = entitiesToTryAndFind;
		this.maxSearchDistance = maxSearchDistance;
	}

	public void Start() => FindEntitiesToCallForHelp();

	void FindEntitiesToCallForHelp()
	{
		finishedCallForHelp = false;

		Dictionary<EntityStats, float> foundEntities = GetClosestEntitiesWithinLimit();

		List<EntityStats> entitiesToCall = new List<EntityStats>();

		for (int i = 0; i < entitiesToTryAndFind; i++)
		{
			EntityStats closestEntity = GetClosestEntity(foundEntities);

			if (closestEntity == null) return; //no more entities to find
			else
			{
				entitiesToCall.Add(closestEntity); //add to call list + remove from dict so its not selected again
				foundEntities.Remove(closestEntity);
			}
		}

		foreach (EntityStats entity in entitiesToCall)
		{
			//update some sort of bool in entity brain to get them to answer call for help
		}

		finishedCallForHelp = true;
	}
	Dictionary<EntityStats, float> GetClosestEntitiesWithinLimit()
	{
		Dictionary<EntityStats, float> foundEntities = new Dictionary<EntityStats, float>();

		List<EntityStats> teamToSearch = new List<EntityStats>();

		if (entity._Data.team == EntityData.EntityTeam.redTeam)
			teamToSearch = AiDirector.instance.RedTeamEntities;
		else if (entity._Data.team == EntityData.EntityTeam.greenTeam)
			teamToSearch = AiDirector.instance.GreenTeamEntities;
		else
			Debug.LogError("no team matching entity found");

		for (int i = 0; i < teamToSearch.Count; i++)
		{
			float distance = Vector3.Distance(entity.transform.position, teamToSearch[i].transform.position);
			if (distance > maxSearchDistance) continue;

			foundEntities.Add(teamToSearch[i], distance);
		}

		return foundEntities;
	}
	EntityStats GetClosestEntity(Dictionary<EntityStats, float> foundEntities)
	{
		EntityStats closestEntity = null;
		float distance = maxSearchDistance;

        foreach (var foundEntity in foundEntities)
        {
			if (foundEntity.Key == null) continue;
			if (foundEntity.Value < distance)
			{
				closestEntity = foundEntity.Key;
				distance = foundEntity.Value;
			}
        }

		return closestEntity;
    }
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

public class BasicAttackStrategy : IActionStrategy
{
	readonly EntityBrain entityBrain;
	readonly int attackToUse;
	readonly TargetData target;

	public bool CanPerform => target.obj != null; // Agent can always attack
	public bool Complete => target.obj == null;

	public BasicAttackStrategy(EntityBrain entityBrain, int attackToUse, TargetData target)
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
	readonly EntityBrain entityBrain;
	readonly NavMeshAgent agent;
	readonly float wanderRadius;


	public bool CanPerform => !Complete;
	public bool Complete => agent.remainingDistance <= 2f && !agent.pathPending;

	//public bool CanPerform => !Complete;
	//public bool Complete => agent.remainingDistance <= 2f && !agent.pathPending || 
		//entityBrain.beliefs["FoundEnemyPoi"].Evaluate() && entityBrain.beliefs["FoundFriendlyPoi"].Evaluate();

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