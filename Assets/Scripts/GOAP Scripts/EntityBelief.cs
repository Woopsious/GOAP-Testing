using System;
using System.Collections.Generic;
using UnityEngine;

public class BeliefFactory
{
	readonly EntityBrain agent;
	readonly Dictionary<string, EntityBeliefs> beliefs;

	public BeliefFactory(EntityBrain agent, Dictionary<string, EntityBeliefs> beliefs)
	{
		this.agent = agent;
		this.beliefs = beliefs;
	}

	public void AddBelief(string key, Func<bool> condition)
	{
		beliefs.Add(key, new EntityBeliefs.Builder(key)
			.WithCondition(condition)
			.Build());
	}

	public void AddSensorBelief(string key, EntitySensor sensor)
	{
		beliefs.Add(key, new EntityBeliefs.Builder(key)
			.WithCondition(() => sensor.IsTargetInRange)
			.WithLocation(() => sensor.TargetPosition)
			.Build());
	}

	public void AddTargetBelief(string key, EntitySensor sensor)
	{
		beliefs.Add(key, new EntityBeliefs.Builder(key)
			.WithCondition(() => sensor.IsTargetInRange)
			.WithTargetLocation(() => sensor.TargetPosition)
			.WithTargetRef(() => sensor.target)
			.Build());
	}

	public void AddTargetBelief(string key, Func<EntityStats> entity)
	{
		beliefs.Add(key, new EntityBeliefs.Builder(key)
			.WithCondition(() => entity())
			.WithTargetLocation(() => entity().transform.position)
			.WithEntityTargetRef(() => entity())
			.Build());
	}

	public void AddLocationBelief(string key, float distance, Vector3 locationCondition)
	{
		beliefs.Add(key, new EntityBeliefs.Builder(key)
			.WithCondition(() => InRangeOf(locationCondition, distance))
			.WithLocation(() => locationCondition)
			.Build());
	}

	bool InRangeOf(Vector3 pos, float range) => Vector3.Distance(agent.transform.position, pos) < range;
}

public class EntityBeliefs
{
	public string Name { get; }

	Func<bool> condition = () => false;

	Func<Vector3> observedLocation = () => Vector3.zero;
	public Vector3 Location => observedLocation();

	Func<Vector3> targetLocation = () => Vector3.zero;
	public Vector3 TargetLocation => targetLocation();

	Func<TargetData> targetData = () => null;
	public TargetData TargetData => targetData();

	Func<EntityStats> entityTarget = () => null;
	public EntityStats EntityTarget => entityTarget();

	EntityBeliefs(string name)
	{
		Name = name;
	}

	public bool Evaluate() => condition();

	public class Builder
	{
		readonly EntityBeliefs belief;

		public Builder(string name)
		{
			belief = new EntityBeliefs(name);
		}

		public Builder WithCondition(Func<bool> condition)
		{
			belief.condition = condition;
			return this;
		}

		public Builder WithLocation(Func<Vector3> observedLocation)
		{
			belief.observedLocation = observedLocation;
			return this;
		}

		public Builder WithTargetLocation(Func<Vector3> observedLocation)
		{
			belief.targetLocation = observedLocation;
			return this;
		}

		public Builder WithTargetRef(Func<TargetData> target)
		{
			belief.targetData = target;
			return this;
		}

		public Builder WithEntityTargetRef(Func<EntityStats> target)
		{
			belief.entityTarget = target;
			return this;
		}

		public EntityBeliefs Build()
		{
			return belief;
		}
	}
}
