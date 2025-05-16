using UnityEngine;

public interface IInteractable
{
	public void StartInteract(EntityStats entityInteracting);
	public void CancelInteract(EntityStats entityInteracting);
	public void CompleteInteract(EntityStats entityInteracting);
}
