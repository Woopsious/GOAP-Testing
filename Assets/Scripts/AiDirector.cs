using System;
using System.Collections.Generic;
using UnityEngine;

public class AiDirector : MonoBehaviour
{
	public static AiDirector instance;

	//game events
	public static event Action<PoIController> OnPoiCaptureEvent;
	public static event Action<EntityStats> OnEntitySpawnEvent;
	public static event Action<EntityStats> OnEntityDeathEvent;

	//ui events
	public static event Action UpdateUiPoiCounters;
	public static event Action<EntityData.EntityTeam, int> UpdateUiResourceCounters;
	public static event Action<EntityData.EntityTeam> UpdateUiPopDataEvent;

	[Header("Global Locations")]
	public GameObject redTeamHomeBase;
	public GameObject greenTeamHomeBase;

	[Header("Red Team Global Info")]
	public int RedTeamResourceCounter;
	public List<PoIController> RedTeamCapturedPois = new List<PoIController>();
	public List<EntityStats> RedTeamEntities = new List<EntityStats>();

	[Header("Green Team Global Info")]
	public int GreenTeamResourceCounter;
	public List<PoIController> GreenTeamCapturedPois = new List<PoIController>();
	public List<EntityStats> GreenTeamEntities = new List<EntityStats>();

	private void Awake()
	{
		instance = this;
	}

	//events
	public static void OnEntitySpawn(EntityStats entity)
	{
		OnEntitySpawnEvent?.Invoke(entity);
		instance.AddEntityToList(entity);
	}
	void AddEntityToList(EntityStats entity)
	{
		if (entity._Data.team == EntityData.EntityTeam.redTeam)
			instance.RedTeamEntities.Add(entity);
		else if (entity._Data.team == EntityData.EntityTeam.greenTeam)
			instance.GreenTeamEntities.Add(entity);
		else
			Debug.LogError("no matching team found");

		UpdateUiPopDataEvent?.Invoke(entity._Data.team);
	}

	public static void OnEntityDeath(EntityStats entity)
	{
		OnEntityDeathEvent?.Invoke(entity);
		instance.RemoveEntityFromList(entity);
	}
	void RemoveEntityFromList(EntityStats entity)
	{
		if (entity._Data.team == EntityData.EntityTeam.redTeam)
			instance.RedTeamEntities.Remove(entity);
		else if (entity._Data.team == EntityData.EntityTeam.greenTeam)
			instance.GreenTeamEntities.Remove(entity);
		else
			Debug.LogError("no matching team found");

		UpdateUiPopDataEvent?.Invoke(entity._Data.team);
	}

	public static void UpdateTeamResourceCounter(EntityData.EntityTeam team, int resourceAmount)
	{
		if (team == EntityData.EntityTeam.redTeam)
			instance.RedTeamResourceCounter = resourceAmount;
		else if (team == EntityData.EntityTeam.greenTeam)
			instance.GreenTeamResourceCounter = resourceAmount;
		else
			Debug.LogError("no team match found for resources");

		UpdateUiResourceCounters?.Invoke(team, resourceAmount);
	}

	public static void OnPoiCapture(PoIController poi)
	{
		OnPoiCaptureEvent?.Invoke(poi);
		instance.UpdatePoiOwners(poi);
	}
	void UpdatePoiOwners(PoIController poi)
	{
		if (poi.previousPoiOwner == EntityData.EntityTeam.neutral && poi.poiOwner == EntityData.EntityTeam.neutral) return;

		if (poi.previousPoiOwner == EntityData.EntityTeam.redTeam)
			RedTeamCapturedPois.Remove(poi);
		else if (poi.previousPoiOwner == EntityData.EntityTeam.greenTeam)
			GreenTeamCapturedPois.Remove(poi);
		else if (poi.previousPoiOwner == EntityData.EntityTeam.neutral)
			Debug.Log("neutral poi captured");
		else
			Debug.LogError("previous poi owner not set up");

		if (poi.poiOwner == EntityData.EntityTeam.redTeam)
			RedTeamCapturedPois.Add(poi);
		else if (poi.poiOwner == EntityData.EntityTeam.greenTeam)
			GreenTeamCapturedPois.Add(poi);
		else if (poi.poiOwner == EntityData.EntityTeam.neutral)
			Debug.LogError("poi captured to neutral team, shouldnt be possible");
		else
			Debug.LogError("poi owner not set up");

		UpdateUiPoiCounters?.Invoke();
	}
}
