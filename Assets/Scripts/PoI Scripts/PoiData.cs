using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EntityData", menuName = "ScriptableObjects/PoIData")]
public class PoiData : ScriptableObject
{
	public string PoiName;
	public EntityData.EntityTeam startingOwner;

	public bool CanBeCaptured;
	public bool BeingCaptured;

	public float timeToCapture;

	public int ResourcesProvided;
	public float ResourcesTimerCooldown;

	public float PoiDetectionRadius;
}
