using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PoiData", menuName = "ScriptableObjects/PoIData")]
public class PoiData : ScriptableObject
{
	[Header("Ownership")]
	public string PoiName;
	public EntityData.EntityTeam startingOwner;

	public bool isTeamHomeBase;

	[Header("Capture Settings")]
	public bool CanBeCaptured;
	public float timeToCapture;

	[Header("Resource Settings")]
	public int ResourcesProvided;
	public float ResourcesTimerCooldown;
	public float ResourceTransferTime;

	[Header("Healing Settings")]
	public float HealAmount;
	public float HealTimerCooldown;
}
