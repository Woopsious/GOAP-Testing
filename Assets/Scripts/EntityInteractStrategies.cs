using UnityEngine;

public interface IEntityInteractStrategies
{
	public float GetInteractTimer();

	public void StartInteract(EntityStats entityInteracting);
	public void CancelInteract(EntityStats entityInteracting);
	public void CompleteInteract(EntityStats entityInteracting);
}

public class CapturePoiInteract : IEntityInteractStrategies
{
	readonly EntityBeliefs belief;

	public CapturePoiInteract(EntityBeliefs belief)
	{
		this.belief = belief;
	}

	public float GetInteractTimer()
	{
		return belief.TargetData.poiController._PoiData.timeToCapture;
	}

	public void StartInteract(EntityStats entity)
	{
		belief.TargetData.poiController.UpdateEntityCapturingPoint(entity);
	}

	public void CancelInteract(EntityStats entity)
	{
		belief.TargetData.poiController.UpdateEntityCapturingPoint(null);
	}

	public void CompleteInteract(EntityStats entity)
	{
		belief.TargetData.poiController.UpdatePoiOwner(entity._Data.team);
		belief.TargetData.poiController.UpdateEntityCapturingPoint(null);
	}
}

public class HealAtPoiInteract : IEntityInteractStrategies
{
	readonly EntityBeliefs belief;

	public HealAtPoiInteract(EntityBeliefs belief)
	{
		this.belief = belief;
	}

	public float GetInteractTimer()
	{
		return belief.TargetData.poiController._PoiData.timeToCapture;
	}

	public void StartInteract(EntityStats entity)
	{
		//noop
	}

	public void CancelInteract(EntityStats entity)
	{
		//noop
	}

	public void CompleteInteract(EntityStats entity)
	{
		entity.RecieveHealing(belief.TargetData.poiController._PoiData.HealAmount);
	}
}

public class PickupResourcesInteract : IEntityInteractStrategies
{
	readonly EntityBeliefs belief;

	public PickupResourcesInteract(EntityBeliefs belief)
	{
		this.belief = belief;
	}

	public float GetInteractTimer()
	{
		return belief.TargetData.poiController._PoiData.ResourceTransferTime;
	}

	public void StartInteract(EntityStats entity)
	{
		//noop
	}

	public void CancelInteract(EntityStats entity)
	{
		//noop
	}

	public void CompleteInteract(EntityStats entity)
	{
		if (belief.TargetData.poiController.accumilatedResources >= 250)
		{
			entity.carriedResources = 250;
			belief.TargetData.poiController.accumilatedResources -= 250;
		}
		else
		{
			entity.carriedResources = belief.TargetData.poiController.accumilatedResources;
			belief.TargetData.poiController.accumilatedResources = 0;
		}
	}
}

public class DropOffResourcesInteract : IEntityInteractStrategies
{
	readonly EntityBeliefs belief;

	public DropOffResourcesInteract(EntityBeliefs belief)
	{
		this.belief = belief;
	}

	public float GetInteractTimer()
	{
		return belief.TargetData.poiController._PoiData.ResourceTransferTime;
	}

	public void StartInteract(EntityStats entity)
	{
		//noop
	}

	public void CancelInteract(EntityStats entity)
	{
		//noop
	}

	public void CompleteInteract(EntityStats entity)
	{
		if (belief.TargetData.poiController.accumilatedResources >= 250)
		{
			belief.TargetData.poiController.accumilatedResources += 250;
			entity.carriedResources = 0;
		}
		else
		{
			belief.TargetData.poiController.accumilatedResources += entity.carriedResources;
			entity.carriedResources = 0;
		}
	}
}

