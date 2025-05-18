using UnityEditor;
using UnityEngine;

public interface IInteractable
{
	public void StartInteract(EntityStats entityInteracting);
	public void CancelInteract(EntityStats entityInteracting);
	public void CompleteInteract(EntityStats entityInteracting);
}

public interface IInteractableNew
{
	public void StartInteract(EntityStats entityInteracting);
	public void CancelInteract(EntityStats entityInteracting);
	public void CompleteInteract(EntityStats entityInteracting);
}

public class EntityInteractions
{
	readonly EntityStats _stats;
	readonly EntityBrain _brain;

	readonly IInteractableNew Interactable;

	readonly PoIController PoIController;

	readonly float InteractTime;

	public EntityInteractions(IInteractableNew interactable, PoIController poiController)
	{
		Interactable = interactable;
		PoIController = poiController;
	}
}


public class CapturePoiInteract : IInteractableNew
{
	readonly PoIController poiController;

	public float InteractTime;

	public CapturePoiInteract(PoIController poiController)
	{
		this.poiController = poiController;
		InteractTime = poiController._PoiData.timeToCapture;
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

