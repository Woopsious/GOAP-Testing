using System;
using System.Collections.Generic;
using UnityEngine;

public class EntityPopManager : MonoBehaviour
{
	public static EntityPopManager instance;

	public static event Action<EntityStats> OnEntitySpawnEvent;
	public static event Action<EntityStats> OnEntityDeathEvent;

	public static event Action<EntityData.EntityTeam> UpdateUiPopDataEvent;

	public EntityPopData redWorkerPopData;
	public EntityPopData redDualistPopData;
	public EntityPopData redMeleePopData;
	public EntityPopData redRangedPopData;

	public EntityPopData greenWorkerPopData;
	public EntityPopData greenDualistPopData;
	public EntityPopData greenMeleePopData;
	public EntityPopData greenRangedPopData;

	private void Awake()
	{
		instance = this;
		SetUpInitialTeamPopData();
	}

	//shared team funcs
	public void SetUpInitialTeamPopData()
	{
		redWorkerPopData = new EntityPopData(Database.GetEntity(EntityData.EntityDataType.redWorker), 1f, 1.25f, 0.1f, 3f);
		redDualistPopData = new EntityPopData(Database.GetEntity(EntityData.EntityDataType.redDualist), 1f, 1.5f, 0.3f, 2f);
		redMeleePopData = new EntityPopData(Database.GetEntity(EntityData.EntityDataType.redMelee), 2f, 2.5f, 0.5f, 1.5f);
		redRangedPopData = new EntityPopData(Database.GetEntity(EntityData.EntityDataType.redRanged), 2f, 2.5f, 0.5f, 1.5f);

		greenWorkerPopData = new EntityPopData(Database.GetEntity(EntityData.EntityDataType.greenWorker), 1f, 1.25f, 0.1f, 3f);
		greenDualistPopData = new EntityPopData(Database.GetEntity(EntityData.EntityDataType.greenDualist), 1f, 1.5f, 0.3f, 2f);
		greenMeleePopData = new EntityPopData(Database.GetEntity(EntityData.EntityDataType.greenMelee), 2f, 2.5f, 0.5f, 1.5f);
		greenRangedPopData = new EntityPopData(Database.GetEntity(EntityData.EntityDataType.greenRanged), 2f, 2.5f, 0.5f, 1.5f);
	}

	public void UpdateTeamPopData(EntityData.EntityTeam team, int accumilatedResources)
	{
		if (team == EntityData.EntityTeam.redTeam)
			UpdateRedTeamPopData(accumilatedResources);
		else if (team == EntityData.EntityTeam.greenTeam)
			UpdateGreenTeamPopData(accumilatedResources);
		else
			Debug.LogError("team pop not set up");
	}
	public EntityPopData GetMostNeededTeamPop(EntityData.EntityTeam team)
	{
		if (team == EntityData.EntityTeam.redTeam)
			return GetMostNeededRedTeamPop();
		else if (team == EntityData.EntityTeam.greenTeam)
			return GetMostNeededGreenTeamPop();
		else
		{
			Debug.LogError("team pop not set up");
			return null;
		}
	}
	public void RandomizePopGoals(EntityData.EntityTeam team)
	{
		if (team == EntityData.EntityTeam.redTeam)
			RandomizeRedTeamPopGoals();
		else if (team == EntityData.EntityTeam.greenTeam)
			RandomizeGreenTeamPopGoals();
		else
			Debug.LogError("team pop not set up");
	}

	public List<EntityPopData> SpawnStartingEntities(EntityData.EntityTeam team)
	{
		List<EntityPopData> startingEntities;

		if (team == EntityData.EntityTeam.redTeam)
		{
			startingEntities = new List<EntityPopData>
			{
				redWorkerPopData, redMeleePopData, redRangedPopData
			};
		}
		else if (team == EntityData.EntityTeam.greenTeam)
		{
			startingEntities = new List<EntityPopData>
			{
				greenWorkerPopData, greenMeleePopData, greenRangedPopData
			};
		}
		else
		{
			Debug.LogError("starting entities not set up for team");
			return null;
		}

		return startingEntities;
	}

	//red team funcs
	void UpdateRedTeamPopData(int accumilatedResources)
	{
		redWorkerPopData.CalculatePopGoals(GameManager.instance.RedTeamCapturedPois, accumilatedResources);
		redDualistPopData.CalculatePopGoals(GameManager.instance.RedTeamCapturedPois, accumilatedResources);
		redMeleePopData.CalculatePopGoals(GameManager.instance.RedTeamCapturedPois, accumilatedResources);
		redRangedPopData.CalculatePopGoals(GameManager.instance.RedTeamCapturedPois, accumilatedResources);

		redWorkerPopData.CalculatePopNeeds();
		redDualistPopData.CalculatePopNeeds();
		redMeleePopData.CalculatePopNeeds();
		redRangedPopData.CalculatePopNeeds();

		UpdateUiPopDataEvent?.Invoke(EntityData.EntityTeam.redTeam);
	}
	EntityPopData GetMostNeededRedTeamPop()
	{
		float[] popNeeds = new float[4];
		popNeeds[0] = redWorkerPopData.PopNeed();
		popNeeds[1] = redDualistPopData.PopNeed();
		popNeeds[2] = redMeleePopData.PopNeed();
		popNeeds[3] = redRangedPopData.PopNeed();

		int popIndex = 0;
		float popNeed = 0;

		for (int i = 0; i < popNeeds.Length; i++)
		{
			if (popNeeds[i] > popNeed)
			{
				popIndex = i;
				popNeed = popNeeds[i];
			}
		}

		switch (popIndex)
		{
			case 0:
			return redWorkerPopData;
			case 1:
			return redDualistPopData;
			case 2:
			return redMeleePopData;
			case 3:
			return redRangedPopData;
			default:
			Debug.LogError("failed to find pop data with matching index, returning worker as back up");
			return redWorkerPopData;
		}
	}
	void RandomizeRedTeamPopGoals()
	{
		redWorkerPopData.RandomizePopCount();
		redDualistPopData.RandomizePopCount();
		redMeleePopData.RandomizePopCount();
		redRangedPopData.RandomizePopCount();
	}

	//green team funcs
	void UpdateGreenTeamPopData(int accumilatedResources)
	{
		greenWorkerPopData.CalculatePopGoals(GameManager.instance.RedTeamCapturedPois, accumilatedResources);
		greenDualistPopData.CalculatePopGoals(GameManager.instance.RedTeamCapturedPois, accumilatedResources);
		greenMeleePopData.CalculatePopGoals(GameManager.instance.RedTeamCapturedPois, accumilatedResources);
		greenRangedPopData.CalculatePopGoals(GameManager.instance.RedTeamCapturedPois, accumilatedResources);

		greenWorkerPopData.CalculatePopNeeds();
		greenDualistPopData.CalculatePopNeeds();
		greenMeleePopData.CalculatePopNeeds();
		greenRangedPopData.CalculatePopNeeds();

		UpdateUiPopDataEvent?.Invoke(EntityData.EntityTeam.greenTeam);
	}
	EntityPopData GetMostNeededGreenTeamPop()
	{
		float[] popNeeds = new float[4];
		popNeeds[0] = greenWorkerPopData.PopNeed();
		popNeeds[1] = greenDualistPopData.PopNeed();
		popNeeds[2] = greenMeleePopData.PopNeed();
		popNeeds[3] = greenRangedPopData.PopNeed();

		int popIndex = 0;
		float popNeed = 0;

		for (int i = 0; i < popNeeds.Length; i++)
		{
			if (popNeeds[i] > popNeed)
			{
				popIndex = i;
				popNeed = popNeeds[i];
			}
		}

		switch (popIndex)
		{
			case 0:
			return greenWorkerPopData;
			case 1:
			return greenDualistPopData;
			case 2:
			return greenMeleePopData;
			case 3:
			return greenRangedPopData;
			default:
			Debug.LogError("failed to find pop data with matching index, returning worker as back up");
			return greenWorkerPopData;
		}
	}
	void RandomizeGreenTeamPopGoals()
	{
		greenWorkerPopData.RandomizePopCount();
		greenDualistPopData.RandomizePopCount();
		greenMeleePopData.RandomizePopCount();
		greenRangedPopData.RandomizePopCount();
	}

	//entity spawn/destroy events + counter
	public static void OnEntitySpawn(EntityStats entity)
	{
		OnEntitySpawnEvent?.Invoke(entity);
		instance.AddEntityToCounters(entity);
	}
	void AddEntityToCounters(EntityStats entity)
	{
		if (entity._Data.team == EntityData.EntityTeam.redTeam)
		{
			if (entity._Data.type == EntityData.EntityDataType.redWorker)
				redWorkerPopData.AddPop();
			else if (entity._Data.type == EntityData.EntityDataType.redDualist)
				redDualistPopData.AddPop();
			else if (entity._Data.type == EntityData.EntityDataType.redMelee)
				redMeleePopData.AddPop();
			else if (entity._Data.type == EntityData.EntityDataType.redRanged)
				redRangedPopData.AddPop();
			else
				Debug.LogError("no matching entity data type set up");
		}
		else if (entity._Data.team == EntityData.EntityTeam.greenTeam)
		{
			if (entity._Data.type == EntityData.EntityDataType.greenWorker)
				greenWorkerPopData.AddPop();
			else if (entity._Data.type == EntityData.EntityDataType.greenDualist)
				greenDualistPopData.AddPop();
			else if (entity._Data.type == EntityData.EntityDataType.greenMelee)
				greenMeleePopData.AddPop();
			else if (entity._Data.type == EntityData.EntityDataType.greenRanged)
				greenRangedPopData.AddPop();
			else
				Debug.LogError("no matching entity data type set up");
		}
		else
			Debug.LogError("no matching team set up");
	}

	public static void OnEntityDeath(EntityStats entity)
	{
		OnEntityDeathEvent?.Invoke(entity);
		instance.MinusEntityFromCounters(entity);
	}
	void MinusEntityFromCounters(EntityStats entity)
	{
		if (entity._Data.team == EntityData.EntityTeam.redTeam)
		{
			if (entity._Data.type == EntityData.EntityDataType.redWorker)
				redWorkerPopData.RemovePop();
			else if (entity._Data.type == EntityData.EntityDataType.redDualist)
				redDualistPopData.RemovePop();
			else if (entity._Data.type == EntityData.EntityDataType.redMelee)
				redMeleePopData.RemovePop();
			else if (entity._Data.type == EntityData.EntityDataType.redRanged)
				redRangedPopData.RemovePop();
			else
				Debug.LogError("no matching entity data type set up");
		}
		else if (entity._Data.team == EntityData.EntityTeam.greenTeam)
		{
			if (entity._Data.type == EntityData.EntityDataType.greenWorker)
				greenWorkerPopData.RemovePop();
			else if (entity._Data.type == EntityData.EntityDataType.greenDualist)
				greenDualistPopData.RemovePop();
			else if (entity._Data.type == EntityData.EntityDataType.greenMelee)
				greenMeleePopData.RemovePop();
			else if (entity._Data.type == EntityData.EntityDataType.greenRanged)
				greenRangedPopData.RemovePop();
			else
				Debug.LogError("no matching entity data type set up");
		}
		else
			Debug.LogError("no matching team set up");
	}
}

[Serializable]
public class EntityPopData
{
	//pop data
	readonly EntityData popData;

	//goal weights
	[SerializeField] readonly float popGoalPerCapturePoint;
	[SerializeField] readonly float popGoalPerThousandResources;
	[SerializeField] int popGoalRandomizer;

	//seperated goals
	[SerializeField] readonly float popBaseGoal;
	[SerializeField] float popCapturePointGoal;
	[SerializeField] float popResourcesGoal;

	//total goal
	[SerializeField] int currentPop;
	[SerializeField] int popGoal;

	//need weights
	[SerializeField] readonly float popWeightedNeed;

	//need
	[SerializeField] float popNeed;

	public EntityData PopData()
	{
		return popData;
	}
	public void AddPop()
	{
		currentPop++;
	}
	public void RemovePop()
	{
		currentPop--;
	}
	public int CurrentPop()
	{
		return currentPop;
	}
	public int PopGoal()
	{
		return popGoal;
	}
	public float PopNeed()
	{
		return popNeed;
	}
	public bool CanAffordPopCost(int resourcesAmount)
	{
		if (popData.resourceCost > resourcesAmount)
			return false;
		else return true;
	}

	public EntityPopData(EntityData popData, float popBaseGoal,
		float popGoalPerCapturePoint, float popGoalPerThousandResources, float popWeightedNeed)
	{
		this.popData = popData;
		this.popBaseGoal = popBaseGoal;
		this.popGoalPerCapturePoint = popGoalPerCapturePoint;
		this.popGoalPerThousandResources = popGoalPerThousandResources;

		currentPop = 0;
		this.popWeightedNeed = popWeightedNeed;
	}

	public void CalculatePopGoals(float ownedCapturePoints, float resourcesAmount)
	{
		popCapturePointGoal = GetCapturePointGoal(ownedCapturePoints);
		popResourcesGoal = GetResourcesGoal(resourcesAmount);
		popGoal = Mathf.RoundToInt(popBaseGoal + popCapturePointGoal + popResourcesGoal + popGoalRandomizer);
	}
	float GetCapturePointGoal(float ownedCapturePoints)
	{
		return popGoalPerCapturePoint * ownedCapturePoints;
	}
	float GetResourcesGoal(float resourcesAmount)
	{
		return popGoalPerThousandResources * Mathf.RoundToInt(resourcesAmount / 1000);
	}

	public void RandomizePopCount()
	{
		int popGoalRandomizedAmount = Mathf.RoundToInt(popGoal / 4);
		popGoalRandomizer = UnityEngine.Random.Range(-popGoalRandomizedAmount, popGoalRandomizedAmount);
	}

	public void CalculatePopNeeds()
	{
		popNeed = (popGoal - currentPop) * popWeightedNeed;
	}

	public void DebugPopData(bool debug)
	{
		if (!debug) return;
		string popType = "";

		if (popData.type == EntityData.EntityDataType.redWorker)
			popType = "red worker pop Info:";
		else if (popData.type == EntityData.EntityDataType.greenWorker)
			popType = "green worker pop Info:";
		else if (popData.type == EntityData.EntityDataType.redDualist)
			popType = "red dualist pop Info:";
		else if (popData.type == EntityData.EntityDataType.greenDualist)
			popType = "green dualist pop Info:";
		else if (popData.type == EntityData.EntityDataType.redMelee)
			popType = "red melee pop Info:";
		else if (popData.type == EntityData.EntityDataType.greenMelee)
			popType = "green melee pop Info:";
		else if (popData.type == EntityData.EntityDataType.redRanged)
			popType = "red ranged pop Info:";
		else if (popData.type == EntityData.EntityDataType.greenRanged)
			popType = "green ranged pop Info:";
		else
			Debug.LogWarning("no data type match");

		Debug.LogWarning(popType + " POP GOALS BREAKDOWN: \nbase pop goal: " + popBaseGoal + " pop cap goal: " + popCapturePointGoal +
			" pop res goal: " + popResourcesGoal + " pop random goal: " + popGoalRandomizer + " \n" +
			"TOTALS: total pop goal: " + popGoal + " current pop: " + currentPop + " pop need: " + popNeed);
	}
}
