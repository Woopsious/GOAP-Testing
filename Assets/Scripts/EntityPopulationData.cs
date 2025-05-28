using System.Threading.Tasks;
using UnityEngine;

public class EntityPopulationData
{
	//pop data
	readonly EntityData popData;

	//goal weights
	readonly float popGoalPerCapturePoint;
	readonly float popGoalPerThousandResources;
	int popGoalRandomizer;

	//seperated goals
	readonly float popBaseGoal;
	float popCapturePointGoal;
	float popResourcesGoal;

	//total goal
	int currentPop;
	int popGoal;

	//need weights
	readonly float popWeightedNeed;

	//need
	float popNeed;

	public EntityData PopData()
	{
		return popData;
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

	public EntityPopulationData(EntityData popData, 
		float popBaseGoal, float popGoalPerCapturePoint, float popGoalPerThousandResources, float popWeightedNeed)
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
		popGoalRandomizer = Random.Range(-popGoalRandomizedAmount, popGoalRandomizedAmount);
	}

	public void CalculatePopNeeds(int currentPop)
	{
		this.currentPop = currentPop;
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
