using System.Collections.Generic;

public class EntityActions
{
	public string Name { get; }
	public float Cost { get; private set; }

	public HashSet<EntityBeliefs> Preconditions { get; } = new();
	public HashSet<EntityBeliefs> Effects { get; } = new();

	IActionStrategy strategy;
	public bool Complete => strategy.Complete;

	EntityActions(string name)
	{
		Name = name;
	}

	public void Start() => strategy.Start();

	public void Update(float deltaTime)
	{
		// Check if the action can be performed and update the strategy
		if (strategy.CanPerform)
		{
			strategy.Update(deltaTime);
		}

		// Bail out if the strategy is still executing
		if (!strategy.Complete) return;

		// Apply effects
		foreach (var effect in Effects)
		{
			effect.Evaluate();
		}
	}

	public void Stop() => strategy.Stop();

	public class Builder
	{
		readonly EntityActions action;

		public Builder(string name)
		{
			action = new EntityActions(name)
			{
				Cost = 1
			};
		}

		public Builder WithCost(float cost)
		{
			action.Cost = cost;
			return this;
		}

		public Builder WithStrategy(IActionStrategy strategy)
		{
			action.strategy = strategy;
			return this;
		}

		public Builder AddPrecondition(EntityBeliefs precondition)
		{
			action.Preconditions.Add(precondition);
			return this;
		}

		public Builder AddEffect(EntityBeliefs effect)
		{
			action.Effects.Add(effect);
			return this;
		}

		public EntityActions Build()
		{
			return action;
		}
	}
}
