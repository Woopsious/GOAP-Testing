using System.Collections.Generic;
using UnityEngine;

public class EntityGoals
{
	public string Name { get; }
	public float Priority { get; private set; }
	public HashSet<EntityBeliefs> DesiredEffects { get; } = new();

	EntityGoals(string name)
	{
		Name = name;
	}

	public void UpdateGoalPriority(float newPriority)
	{
		Priority = newPriority;
	}

	public class Builder
	{
		readonly EntityGoals goal;

		public Builder(string name)
		{
			goal = new EntityGoals(name);
		}

		public Builder WithPriority(float priority)
		{
			goal.Priority = priority;
			return this;
		}

		public Builder WithDesiredEffect(EntityBeliefs effect)
		{
			goal.DesiredEffects.Add(effect);
			return this;
		}

		public EntityGoals Build()
		{
			return goal;
		}
	}
}
