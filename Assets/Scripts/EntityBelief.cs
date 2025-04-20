using System;
using System.Collections.Generic;
using UnityEngine;

public class BeliefFactory
{
	readonly EntityAgent agent;
	readonly Dictionary<string, EntityBeliefs> beliefs;

	public BeliefFactory(EntityAgent agent, Dictionary<string, EntityBeliefs> beliefs)
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

	public void AddLocationBelief(string key, float distance, Transform locationCondition)
	{
		AddLocationBelief(key, distance, locationCondition.position);
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

		public EntityBeliefs Build()
		{
			return belief;
		}
	}
}
