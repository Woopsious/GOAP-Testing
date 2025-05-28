using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public interface IPoIStrategies
{
	public void Start();

	public void Update(float deltaTime);

	public void Stop();
}

public class PoiResourcesStrategy : IPoIStrategies
{
	readonly PoIController poIController;
	readonly PoiData _Data;

	readonly CountdownTimer timer;

	public PoiResourcesStrategy(PoIController poIController)
	{
		this.poIController = poIController;
		_Data = poIController._PoiData;

		timer = new CountdownTimer(_Data.ResourcesTimerCooldown);
		timer.OnTimerStart += () => AddResourcesToCapturePoint();
		timer.OnTimerStop += () => Start();
		timer.OnTimerCancel += () => Stop();
	}

	public void Start()
	{
		timer.Start();
	}
	public void Update(float deltaTime)
	{
		timer.Tick(deltaTime, false);
	}
	public void Stop()
	{
		timer.Stop();
	}


	void AddResourcesToCapturePoint()
	{
		poIController.accumilatedResources += _Data.ResourcesProvided;
		CheckIfResourcesNeedMoving();

		if (_Data.isTeamHomeBase)
			GameManager.instance.UpdateTeamResourceCounter(poIController.poiOwner, poIController.accumilatedResources);
	}

	void CheckIfResourcesNeedMoving()
	{
		if (!_Data.isTeamHomeBase && poIController.accumilatedResources > 200)
			poIController.resourcesNeedTransfering = true;
		else
			poIController.resourcesNeedTransfering = false;
	}
}

public class RedTeamPopulationStrategy : IPoIStrategies
{
	readonly PoIController poIController;
	readonly PoiData _Data;

	readonly CountdownTimer popGoalTimer;
	readonly CountdownTimer popGoalRandomizerTimer;

	bool hasSpawnedStartingEntities;

	readonly EntityPopulationData workerPopData;
	readonly EntityPopulationData dualistPopData;
	readonly EntityPopulationData meleePopData;
	readonly EntityPopulationData rangedPopData;

	public RedTeamPopulationStrategy(PoIController poIController)
	{
		this.poIController = poIController;
		_Data = poIController._PoiData;
		hasSpawnedStartingEntities = false;

		popGoalTimer = new CountdownTimer(3f);
		popGoalTimer.OnTimerStart += () => RunPopulationLogic();
		popGoalTimer.OnTimerStop += () => Start();
		popGoalTimer.OnTimerCancel += () => Stop();

		popGoalRandomizerTimer = new CountdownTimer(15f);
		popGoalRandomizerTimer.OnTimerStart += () => RandomizePopulationGoals();
		popGoalRandomizerTimer.OnTimerStop += () => Start();
		popGoalRandomizerTimer.OnTimerCancel += () => Stop();

		workerPopData = new EntityPopulationData(Database.GetEntity(EntityData.EntityDataType.redWorker), 1f, 1.25f, 0.1f, 3f);
		dualistPopData = new EntityPopulationData(Database.GetEntity(EntityData.EntityDataType.redDualist), 1f, 1.5f, 0.3f, 2f);
		meleePopData = new EntityPopulationData(Database.GetEntity(EntityData.EntityDataType.redMelee), 2f, 2.5f, 0.5f, 1.5f);
		rangedPopData = new EntityPopulationData(Database.GetEntity(EntityData.EntityDataType.redRanged), 2f, 2.5f, 0.5f, 1.5f);
	}

	public void Start()
	{
		popGoalTimer.Start();
		popGoalRandomizerTimer.Start();
	}
	public void Update(float deltaTime)
	{
		popGoalTimer.Tick(deltaTime, false);
		popGoalRandomizerTimer.Tick(deltaTime, false);
	}
	public void Stop()
	{
		popGoalTimer.Stop();
		popGoalRandomizerTimer.Stop();
	}

	void RunPopulationLogic()
	{
		if (!_Data.isTeamHomeBase) return;

		if (!hasSpawnedStartingEntities)
			SpawnStartingEntities();

		CalculatePopData();

		workerPopData.DebugPopData(false);
		dualistPopData.DebugPopData(false);
		meleePopData.DebugPopData(false);
		rangedPopData.DebugPopData(false);

		EntityPopulationData popToSpawn = GetMostNeededPop();

		if (popToSpawn == null)
		{
			Debug.LogError("failed to find pop data with matching index");
			return;
		}
		else if (popToSpawn.PopNeed() <= 5)
		{
			//Debug.LogWarning("no pop spawned, pop needs not high enough: " + popToSpawn.PopNeed() + " <= 5");
			return;
		}

		if (popToSpawn.CurrentPop() >= 1)
		{
			if (!popToSpawn.CanAffordPopCost(poIController.accumilatedResources))
			{
				//Debug.LogWarning("no pop spawned, low resources: " + poIController.accumilatedResources + "/" + popToSpawn.PopData().resourceCost);
				return;
			}
			else
				SpawnNewPop(popToSpawn, false);
		}
		else
			SpawnNewPop(popToSpawn, true);
	}

	void CalculatePopData()
	{
		workerPopData.CalculatePopGoals(GameManager.instance.RedTeamCapturedPois, poIController.accumilatedResources);
		dualistPopData.CalculatePopGoals(GameManager.instance.RedTeamCapturedPois, poIController.accumilatedResources);
		meleePopData.CalculatePopGoals(GameManager.instance.RedTeamCapturedPois, poIController.accumilatedResources);
		rangedPopData.CalculatePopGoals(GameManager.instance.RedTeamCapturedPois, poIController.accumilatedResources);

		workerPopData.CalculatePopNeeds(GameManager.instance.RedTeamWorkers);
		dualistPopData.CalculatePopNeeds(GameManager.instance.RedTeamDualists);
		meleePopData.CalculatePopNeeds(GameManager.instance.RedTeamMelee);
		rangedPopData.CalculatePopNeeds(GameManager.instance.RedTeamRanged);
	}
	EntityPopulationData GetMostNeededPop()
	{
		float[] popNeeds = new float[4];
		popNeeds[0] = workerPopData.PopNeed();
		popNeeds[1] = dualistPopData.PopNeed();
		popNeeds[2] = meleePopData.PopNeed();
		popNeeds[3] = rangedPopData.PopNeed();

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
			return workerPopData;
			case 1:
			return dualistPopData;
			case 2:
			return meleePopData;
			case 3:
			return rangedPopData;
			default:
			return null;
		}
	}

	void RandomizePopulationGoals()
	{
		workerPopData.RandomizePopCount();
		dualistPopData.RandomizePopCount();
		meleePopData.RandomizePopCount();
		rangedPopData.RandomizePopCount();
	}

	void SpawnStartingEntities()
	{
		hasSpawnedStartingEntities = true;

		SpawnNewPop(workerPopData, true);
		SpawnNewPop(meleePopData, true);
		SpawnNewPop(rangedPopData, true);
	}

	//instantiate and spawn entity
	void SpawnNewPop(EntityPopulationData pop, bool freeCost)
	{
		if (!freeCost)
		{
			//Debug.LogWarning("pop spawned, pop need: " + pop.PopNeed() + " | pop cost: " + pop.PopData().resourceCost);
			poIController.accumilatedResources -= pop.PopData().resourceCost;
		}
		else
		{
			//Debug.LogWarning("pop spawned, pop need: " + pop.PopNeed() + " | pop cost: 0");
		}

		EntityStats entity = poIController.SpawnNewEntity();
		entity._Data = pop.PopData();
		GameManager.OnEntitySpawn(entity);
		GameManager.instance.UpdateTeamResourceCounter(poIController.poiOwner, poIController.accumilatedResources);
	}
}
