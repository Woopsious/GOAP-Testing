using System;
using UnityEngine;

public interface ISensorStrategy
{
	public TargetData FoundTarget();

	public void EvaluateTargets();

	public event Action OnTargetChanged;
}

public class ClosestEnemyEntityStrategy : ISensorStrategy
{
	readonly EntitySensor sensor;

	TargetData foundTarget;
	public event Action OnTargetChanged;

	public Vector3 TargetPosition => foundTarget.obj ? foundTarget.obj.transform.position : Vector3.zero;
	public bool IsTargetInRange => TargetPosition != Vector3.zero;

	public ClosestEnemyEntityStrategy(EntitySensor sensor)
	{
		this.sensor = sensor;
		foundTarget = new(TargetData.TargetType.nullRef);
	}

	public TargetData FoundTarget()
	{
		return foundTarget;
	}

	public void EvaluateTargets()
	{
		TargetData foundTarget = GetClosestEntity();

		if (foundTarget.obj != null)
			OnTargetChanged?.Invoke();

		this.foundTarget = foundTarget;
	}

	public TargetData GetClosestEntity()
	{
		TargetData foundTarget = new(TargetData.TargetType.nullRef);

		if (sensor.targetsInRange.Count > 0)
			foundTarget = sensor.targetsInRange[0];

		return foundTarget;
	}
}

public class FleeClosestEnemyEntityStrategy : ISensorStrategy
{
	readonly EntitySensor sensor;
	readonly EntityData entityData;

	TargetData foundTarget;
	public event Action OnTargetChanged;

	public FleeClosestEnemyEntityStrategy(EntitySensor sensor, EntityData entityData)
	{
		this.sensor = sensor;
		this.entityData = entityData;
		foundTarget = new(TargetData.TargetType.nullRef);
	}

	public TargetData FoundTarget()
	{
		return foundTarget;
	}

	public void EvaluateTargets()
	{
		TargetData foundTarget = GetClosestEntityInFleeRange();
		//target change event here if needed
		this.foundTarget = foundTarget;
	}

	public TargetData GetClosestEntityInFleeRange()
	{
		TargetData foundTarget = new(TargetData.TargetType.nullRef);

		if (sensor.targetsInRange.Count > 0)
			foundTarget = sensor.targetsInRange[0];

		if (foundTarget.TargetDistance < entityData.fleeRange)
		{
			return foundTarget;
		}
		else
		{
			foundTarget = new(TargetData.TargetType.nullRef);
			return foundTarget;
		}
	}
}

public class ClosestEntityToAttackStrategy : ISensorStrategy
{
	readonly EntitySensor sensor;
	readonly EntityAttackData attackData;

	TargetData foundTarget;
	public event Action OnTargetChanged;

	public ClosestEntityToAttackStrategy(EntitySensor sensor, EntityAttackData attackData)
	{
		this.sensor = sensor;
		this.attackData = attackData;
		foundTarget = new(TargetData.TargetType.nullRef);
	}

	public TargetData FoundTarget()
	{
		return foundTarget;
	}

	public void EvaluateTargets()
	{
		TargetData foundTarget = GetClosestEntityWithinRange();
		//target change event here if needed
		this.foundTarget = foundTarget;
	}

	public TargetData GetClosestEntityWithinRange()
	{
		TargetData foundTarget = new(TargetData.TargetType.nullRef);

		if (sensor.targetsInRange.Count > 0)
			foundTarget = sensor.targetsInRange[0];

		if (foundTarget.TargetDistance >= attackData.attackMinRange && foundTarget.TargetDistance <= attackData.attackMaxRange)
		{
			return foundTarget;
		}
		else
		{
			foundTarget = new(TargetData.TargetType.nullRef);
			return foundTarget;
		}
	}
}

public class ClosestEnemyPoiStrategy : ISensorStrategy
{
	readonly EntitySensor sensor;

	TargetData foundTarget;
	public event Action OnTargetChanged;

	public ClosestEnemyPoiStrategy(EntitySensor sensor)
	{
		this.sensor = sensor;
		foundTarget = new(TargetData.TargetType.nullRef);
	}

	public TargetData FoundTarget()
	{
		return foundTarget;
	}

	public void EvaluateTargets()
	{
		TargetData foundTarget = GetClosestEnemyPoi();

		if (this.foundTarget.obj == null && foundTarget.obj != null)
		{
			Debug.LogError("cur target obj null and new one is not");

			OnTargetChanged?.Invoke();
		}

		this.foundTarget = foundTarget;
	}
	public TargetData GetClosestEnemyPoi()
	{
		TargetData foundTarget = new(TargetData.TargetType.nullRef);

		if (sensor.targetsInRange.Count > 0)
			foundTarget = sensor.targetsInRange[0];

		return foundTarget;
	}
}

public class ClosestFriendlyPoiStrategy : ISensorStrategy
{
	readonly EntitySensor sensor;

	TargetData foundTarget;
	public event Action OnTargetChanged;

	public ClosestFriendlyPoiStrategy(EntitySensor sensor)
	{
		this.sensor = sensor;
		foundTarget = new(TargetData.TargetType.nullRef);
	}

	public TargetData FoundTarget()
	{
		return foundTarget;
	}

	public void EvaluateTargets()
	{
		TargetData foundTarget = GetClosestEnemyPoi();

		if (this.foundTarget.obj == null && foundTarget.obj != null)
		{
			Debug.LogError("cur target obj null and new one is not");

			OnTargetChanged?.Invoke();
		}

		this.foundTarget = foundTarget;
	}
	public TargetData GetClosestEnemyPoi()
	{
		TargetData foundTarget = new(TargetData.TargetType.nullRef);

		if (sensor.friendliesInRange.Count > 0)
			foundTarget = sensor.friendliesInRange[0];

		return foundTarget;
	}
}
