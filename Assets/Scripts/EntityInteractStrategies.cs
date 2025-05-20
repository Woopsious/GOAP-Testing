using System;

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
		//poiController.StartInteract(entity);
		//Interacting = true;
		poiController.entityCurrentlyCapturing = entity;
	}

	public void CancelInteract(EntityStats entity)
	{
		//poiController.CancelInteract(entity);
		//Interacting = false;
		poiController.entityCurrentlyCapturing = null;
	}

	public void CompleteInteract(EntityStats entity)
	{
		//poiController.CompleteInteract(entity);
		//Interacting = false;
		poiController.UpdatePoiOwner(entity._Data.team);
		poiController.entityCurrentlyCapturing = null;
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

	}

	public void CancelInteract(EntityStats entity)
	{

	}

	public void CompleteInteract(EntityStats entity)
	{
		entity.RecieveHealing(poiController._PoiData.HealAmount);
	}
}

