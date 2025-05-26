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

		popGoalTimer = new CountdownTimer(5f);
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
		TrySpawnStartingEntities();

		workerPopData.CalculatePopGoal(GameManager.instance.RedTeamCapturedPois, GameManager.instance.RedTeamResourceCounter);
		dualistPopData.CalculatePopGoal(GameManager.instance.RedTeamCapturedPois, GameManager.instance.RedTeamResourceCounter);
		meleePopData.CalculatePopGoal(GameManager.instance.RedTeamCapturedPois, GameManager.instance.RedTeamResourceCounter);
		rangedPopData.CalculatePopGoal(GameManager.instance.RedTeamCapturedPois, GameManager.instance.RedTeamResourceCounter);

		workerPopData.CalculateNeed(GameManager.instance.RedTeamWorkers);
		dualistPopData.CalculateNeed(GameManager.instance.RedTeamDualists);
		meleePopData.CalculateNeed(GameManager.instance.RedTeamMelee);
		rangedPopData.CalculateNeed(GameManager.instance.RedTeamRanged);

		workerPopData.DebugData(true);
		dualistPopData.DebugData(true);
		meleePopData.DebugData(true);
		rangedPopData.DebugData(true);

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

		if (popNeed <= 5) //pop need has to be above X value to spawn new entity
		{
			Debug.LogError("no pop spawned, pop need: " + popNeed);
			return;
		}
		else
			Debug.LogError("pop spawned with need: " + popNeed);

		switch (popIndex)
		{
			case 0:
			SpawnNewEntity(workerPopData.GetPopData());
			break;
			case 1:
			SpawnNewEntity(dualistPopData.GetPopData());
			break;
			case 2:
			SpawnNewEntity(meleePopData.GetPopData());
			break;
			case 3:
			SpawnNewEntity(rangedPopData.GetPopData());
			break;
		}
	}

	void RandomizePopulationGoals()
	{
		workerPopData.RandomizePopCount();
		dualistPopData.RandomizePopCount();
		meleePopData.RandomizePopCount();
		rangedPopData.RandomizePopCount();
	}

	void TrySpawnStartingEntities()
	{
		if (hasSpawnedStartingEntities) return;

		hasSpawnedStartingEntities = true;
	}

	//instantiate and spawn entity
	void SpawnNewEntity(EntityData entityData)
	{
		EntityStats entity = poIController.SpawnNewEntity();
		entity._Data = entityData;
		GameManager.OnEntitySpawn(entity);
	}
}
