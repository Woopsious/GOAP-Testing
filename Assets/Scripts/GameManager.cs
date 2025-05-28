using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
	public static GameManager instance;

	public static event Action<PoIController> OnPoiCaptureEvent;

	//ui events
	public static event Action UpdateUiPoiCounters;
	public static event Action<EntityData.EntityTeam, int> UpdateUiResourceCounters;

	[Header("Red Team Global Info")]
	public int RedTeamResourceCounter;
	public int RedTeamCapturedPois;

	[Header("Green Team Global Info")]
	public int GreenTeamResourceCounter;
	public int GreenTeamCapturedPois;
	public int GreenTeamWorkers;
	public int GreenTeamDualists;
	public int GreenTeamMelee;
	public int GreenTeamRanged;

	[Header("Global Locations")]
	public GameObject redTeamHomeBase;
	public GameObject greenTeamHomeBase;

	public PoIController[] AllPois;

	/// <summary>
	/// TODO:
	/// ENTITIY REQUEST/ANSWER HELP CALL
	/// add a way for entities to call for help from surrounding ones as a RequestHelpCall strategy
	/// add a way for entities to answer a call for help as a AnswerHelpCall strategy
	/// need beliefs for it to trigger these things like getting attacked by too many enemies or low on health etc..
	/// a way to filter out too many entities answering a call or none answering a call, possibly via checking current goals
	/// adding a sensor to detect friendlies in call range
	/// 
	/// ENTITY POI's
	/// team base's/neutral bases: (ATM just get team bases working)
	///		supplies resources entities can use to replenish there population and spawn new ones here
	///		restore health at these + capture and defend them.
	/// ???resoure nodes worker entities can get extra resources from.
	/// 
	/// UPDATE ENTITY TYPES/BRAINS:
	/// have a worker (or different worker types) entity type that focuses on collecting resources or using them etc...
	/// have multiple different offensive bot types like ranged and melee
	///	???a commander type that can organise regular combat type entities (ATM worry about adding simpler things)
	/// 
	/// MIGHT NOT NEED BUT COULD CONSIDER ADDING AN AI DIRECTOR:
	/// a higher level system that will direct entities to achieve certian goals
	/// send entities to capture points based on info of all capture points (update CapturePoi goal priority for worker brains by +- it)
	/// help organize combat entities to attack a poi as a group or defend it based on locational positions of all entities.
	/// manage pop counts of both teams based on current pop + resources accumalated. (update a SpawnEntity goal priority for worker brains)
	/// </summary>

	void Awake()
	{
		instance = this;
	}

	public void UpdateTeamResourceCounter(EntityData.EntityTeam team, int resourceAmount)
	{
		if (team == EntityData.EntityTeam.redTeam)
			RedTeamResourceCounter = resourceAmount;
		else if (team == EntityData.EntityTeam.greenTeam)
			GreenTeamResourceCounter = resourceAmount;
		else
			Debug.LogError("no team match found for resources");

		UpdateUiResourceCounters?.Invoke(team, resourceAmount);
	}

	public static void OnPoiCapture(PoIController poi)
	{
		OnPoiCaptureEvent?.Invoke(poi);
		instance.UpdateCapturePointCounters(poi);
	}
	void UpdateCapturePointCounters(PoIController poi)
	{
		if (poi.previousPoiOwner == EntityData.EntityTeam.neutral && poi.poiOwner == EntityData.EntityTeam.neutral) return;

		if (poi.previousPoiOwner == EntityData.EntityTeam.redTeam)
			RedTeamCapturedPois--;
		else if (poi.previousPoiOwner == EntityData.EntityTeam.greenTeam)
			GreenTeamCapturedPois--;
		else if (poi.previousPoiOwner == EntityData.EntityTeam.neutral)
			Debug.Log("neutral poi captured");
		else
			Debug.LogError("previous poi owner not set up");

		if (poi.poiOwner == EntityData.EntityTeam.redTeam)
			RedTeamCapturedPois++;
		else if (poi.poiOwner == EntityData.EntityTeam.greenTeam)
			GreenTeamCapturedPois++;
		else if (poi.poiOwner == EntityData.EntityTeam.neutral)
			Debug.LogError("poi captured to neutral team, shouldnt be possible");
		else
			Debug.LogError("poi owner not set up");

		UpdateUiPoiCounters?.Invoke();
	}
}
