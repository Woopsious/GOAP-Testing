using UnityEngine;

public interface IEntityInteractStrategies
{
	public void SetInteractType<T>(T interactedObject);
	public float GetInteractTimer();

	public void StartInteract(EntityStats entityInteracting);
	public void CancelInteract(EntityStats entityInteracting);
	public void CompleteInteract(EntityStats entityInteracting);
}

public class CapturePoiInteract : IEntityInteractStrategies
{
	public PoIController poiController;

	public void SetInteractType<T>(T interactedObject)
	{
		poiController = interactedObject as PoIController;
	}
	public float GetInteractTimer()
	{
		return poiController._PoiData.timeToCapture;
	}

	public void StartInteract(EntityStats entity)
	{
		poiController.UpdateEntityCapturingPoint(entity);
	}

	public void CancelInteract(EntityStats entity)
	{
		poiController.UpdateEntityCapturingPoint(null);
	}

	public void CompleteInteract(EntityStats entity)
	{
		poiController.UpdatePoiOwner(entity._Data.team);
		poiController.UpdateEntityCapturingPoint(null);
	}
}

public class HealAtPoiInteract : IEntityInteractStrategies
{
	public PoIController poiController;

	public void SetInteractType<T>(T interactedObject)
	{
		poiController = interactedObject as PoIController;
	}
	public float GetInteractTimer()
	{
		return poiController._PoiData.timeToCapture;
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
		entity.RecieveHealing(poiController._PoiData.HealAmount);
	}
}

public class PickupResourcesInteract : IEntityInteractStrategies
{
	public PoIController poiController;

	public void SetInteractType<T>(T interactedObject)
	{
		poiController = interactedObject as PoIController;
	}
	public float GetInteractTimer()
	{
		return poiController._PoiData.ResourceTransferTime;
	}

	public void StartInteract(EntityStats entity)
	{
		Debug.LogError("pick up res interact start");
		//noop
	}

	public void CancelInteract(EntityStats entity)
	{
		Debug.LogError("pick up res interact cancel");
		//noop
	}

	public void CompleteInteract(EntityStats entity)
	{
		Debug.LogError("pick up res interact complete");

		if (poiController.accumilatedResources >= 250)
		{
			entity.carriedResources = 250;
			poiController.accumilatedResources -= 250;
		}
		else
		{
			entity.carriedResources = poiController.accumilatedResources;
			poiController.accumilatedResources = 0;
		}
	}
}

public class DropOffResourcesInteract : IEntityInteractStrategies
{
	public PoIController poiController;

	public void SetInteractType<T>(T interactedObject)
	{
		poiController = interactedObject as PoIController;
	}
	public float GetInteractTimer()
	{
		return poiController._PoiData.ResourceTransferTime;
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
		if (poiController.accumilatedResources >= 250)
		{
			poiController.accumilatedResources += 250;
			entity.carriedResources = 0;
		}
		else
		{
			poiController.accumilatedResources += entity.carriedResources;
			entity.carriedResources = 0;
		}
	}
}

