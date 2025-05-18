using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EntityData", menuName = "ScriptableObjects/PoIData")]
public class PoiData : ScriptableObject
{
	public string PoiName;
	public EntityData.EntityTeam startingOwner;

	public bool isTeamHomeBase;

	public bool CanBeCaptured;
	public float timeToCapture;

	public int ResourcesProvided;
	public float ResourcesTimerCooldown;

	public float HealAmount;
	public float HealTimerCooldown;

	public float PoiDetectionRadius;
}
