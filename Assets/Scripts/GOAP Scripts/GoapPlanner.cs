using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public interface IGoapPlanner
{
	ActionPlan Plan(EntityBrain agent, HashSet<EntityGoals> goals, EntityGoals mostRecentGoal = null);
}

public class GoapPlanner : IGoapPlanner
{
	public ActionPlan Plan(EntityBrain agent, HashSet<EntityGoals> goals, EntityGoals mostRecentGoal = null)
	{
		// Order goals by priority, descending
		List<EntityGoals> orderedGoals = goals
			.Where(g => g.DesiredEffects.Any(b => !b.Evaluate()))
			.OrderByDescending(g => g == mostRecentGoal ? g.Priority - 0.01 : g.Priority)
			.ToList();

		// Try to solve each goal in order
		foreach (var goal in orderedGoals)
		{
			Node goalNode = new Node(null, null, goal.DesiredEffects, 0);

			// If we can find a path to the goal, return the plan
			if (FindPath(goalNode, agent.actions))
			{
				// If the goalNode has no leaves and no action to perform try a different goal
				if (goalNode.IsLeafDead) continue;

				Stack<EntityActions> actionStack = new Stack<EntityActions>();
				while (goalNode.Leaves.Count > 0)
				{
					var cheapestLeaf = goalNode.Leaves.OrderBy(leaf => leaf.Cost).First();
					goalNode = cheapestLeaf;
					actionStack.Push(cheapestLeaf.Action);
				}

				return new ActionPlan(goal, actionStack, goalNode.Cost);
			}
		}

		Debug.LogWarning("No plan found");
		return null;
	}

	// TODO: Consider a more powerful search algorithm like A* or D*
	bool FindPath(Node parent, HashSet<EntityActions> actions)
	{
		// Order actions by cost, ascending
		var orderedActions = actions.OrderBy(a => a.Cost);

		foreach (var action in orderedActions)
		{
			var requiredEffects = parent.RequiredEffects;

			// Remove any effects that evaluate to true, there is no action to take
			requiredEffects.RemoveWhere(b => b.Evaluate());

			// If there are no required effects to fulfill, we have a plan
			if (requiredEffects.Count == 0)
			{
				return true;
			}

			if (action.Effects.Any(requiredEffects.Contains))
			{
				var newRequiredEffects = new HashSet<EntityBeliefs>(requiredEffects);
				newRequiredEffects.ExceptWith(action.Effects);
				newRequiredEffects.UnionWith(action.Preconditions);

				var newAvailableActions = new HashSet<EntityActions>(actions);
				newAvailableActions.Remove(action);

				var newNode = new Node(parent, action, newRequiredEffects, parent.Cost + action.Cost);

				// Explore the new node recursively
				if (FindPath(newNode, newAvailableActions))
				{
					parent.Leaves.Add(newNode);
					newRequiredEffects.ExceptWith(newNode.Action.Preconditions);
				}

				// If all effects at this depth have been satisfied, return true
				if (newRequiredEffects.Count == 0)
				{
					return true;
				}
			}
		}

		return parent.Leaves.Count > 0;
	}
}

public class Node
{
	public Node Parent { get; }
	public EntityActions Action { get; }
	public HashSet<EntityBeliefs> RequiredEffects { get; }
	public List<Node> Leaves { get; }
	public float Cost { get; }

	public bool IsLeafDead => Leaves.Count == 0 && Action == null;

	public Node(Node parent, EntityActions action, HashSet<EntityBeliefs> effects, float cost)
	{
		Parent = parent;
		Action = action;
		RequiredEffects = new HashSet<EntityBeliefs>(effects);
		Leaves = new List<Node>();
		Cost = cost;
	}
}

public class ActionPlan
{
	public EntityGoals EntityGoals { get; }
	public Stack<EntityActions> Actions { get; }
	public float TotalCost { get; set; }

	public ActionPlan(EntityGoals goal, Stack<EntityActions> actions, float totalCost)
	{
		EntityGoals = goal;
		Actions = actions;
		TotalCost = totalCost;
	}
}
