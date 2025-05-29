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

	/// <summary> IDEAS TO TRY AND IMPLEMENT
	/// ENTITIY REQUEST/ANSWER HELP CALL BEHAVIOUR:
	/// add a way for entities to call for help from surrounding ones as a RequestHelpCall strategy
	/// add a way for entities to answer a call for help as a AnswerHelpCall strategy
	/// need beliefs for it to trigger these things like getting attacked by too many enemies or low on health etc..
	/// a way to filter out too many entities answering a call or none answering a call, possibly via checking current goals
	/// adding a sensor to detect friendlies in call range
	/// 
	/// POI RELATED IDEAS:
	/// resoure nodes for worker entities to 'mine' extra resource from.
	/// different resource types to use on better entity types.
	/// 
	/// ENTITY RELATED IDEAS:
	/// split entity brain strategies up further into melee, ranged etc... for more unique behaviour
	/// allow entities to patrol around/defend pois them.
	/// commander type that can organise regular combat type entities (ATM worry about adding simpler things)
	/// 
	/// AI DIRECTOR IDEAS:
	/// a higher level system that will direct entities to achieve certian goals, either for a specific or multiple teams.
	/// tracks references to all entities and pois.
	/// can direct/force entities to do things via a belief that checks hasCommand bool
	///		an action called EntityCommandStrategy that takes in an interface as an argument (similar concept to IEntityInteractStrategies)
	///		a goal that has its priority adjusted based on the type of command issued (move command priority shouldnt excede basic attack priority
	///		but should excede its wander/move/idle behaviour)
	///	will take over parts of what GM script is currently doing like tracking and storing refs of the following:
	///	to all pois and entity refs + counters for poi ownership + team resource counters.
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
