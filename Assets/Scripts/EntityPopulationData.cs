using System.Threading.Tasks;
using UnityEngine;

public class EntityPopulationData
{
	//pop data
	readonly EntityData popData;

	//goal weights
	readonly float popGoalPerCapturePoint;
	readonly float popGoalPerThousandResources;
	float popGoalRandomizer;

	//seperated goals
	float popBaseGoal;
	float popCapturePointGoal;
	float popResourcesGoal;

	//total goal
	float popGoal;

	//need weights
	readonly float popWeightedNeed;

	//need
	float popNeed;

	public float PopGoal()
	{
		return popGoal;
	}
	public float PopNeed()
	{
		return popNeed;
	}
	public EntityData GetPopData()
	{
		return popData;
	}

	public EntityPopulationData(EntityData popData, float popBaseGoal, float popGoalPerCapturePoint, float popGoalPerThousandResources, float popWeightedNeed)
	{
		this.popData = popData;
		this.popBaseGoal = popBaseGoal;
		this.popGoalPerCapturePoint = popGoalPerCapturePoint;
		this.popGoalPerThousandResources = popGoalPerThousandResources;
		this.popWeightedNeed = popWeightedNeed;
	}

	public void CalculatePopGoal(float ownedCapturePoints, float resourcesAmount)
	{
		popCapturePointGoal = GetCapturePointGoal(ownedCapturePoints);
		popResourcesGoal = GetResourcesGoal(resourcesAmount);
		popGoal = popBaseGoal + popCapturePointGoal + popResourcesGoal + popGoalRandomizer;
	}

	float GetCapturePointGoal(float ownedCapturePoints)
	{
		return popGoalPerCapturePoint * ownedCapturePoints;
	}
	float GetResourcesGoal(float resourcesAmount)
	{
		return popGoalPerThousandResources * Mathf.RoundToInt(resourcesAmount / 1000);
	}

	public void CalculateNeed(float currentPop)
	{
		popNeed = (popGoal - currentPop) * popWeightedNeed;
	}

	public void RandomizePopCount()
	{
		//popGoalRandomizer = Random.Range(-4, 4);
	}

	public void DebugData(bool debug)
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
			Debug.LogError("no data type match");

		Debug.LogError(popType + " base goal: " + popBaseGoal + " cap goal: " + popCapturePointGoal + " res goal: " + popResourcesGoal + 
			" random goal: " + popGoalRandomizer + " total goal: " + popGoal + " need: " + popNeed);
	}
}
