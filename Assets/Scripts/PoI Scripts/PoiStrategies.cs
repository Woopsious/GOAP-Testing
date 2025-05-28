using System;
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
		popGoalRandomizerTimer.OnTimerStart += () => EntityPopManager.instance.RandomizeRedTeamPopulationGoals();
		popGoalRandomizerTimer.OnTimerStop += () => Start();
		popGoalRandomizerTimer.OnTimerCancel += () => Stop();
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

		EntityPopManager.instance.UpdateRedTeamPopData(poIController.accumilatedResources);
		EntityPopData popToSpawn = EntityPopManager.instance.GetMostNeededRedTeamPop();

		if (popToSpawn.PopNeed() <= 5)
		{
			//Debug.LogWarning("no pop spawned, pop needs not high enough: " + popToSpawn.PopNeed() + " <= 5");
			return;
		}

		else if (popToSpawn.CurrentPop() >= 1)
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

	void SpawnStartingEntities()
	{
		hasSpawnedStartingEntities = true;

		SpawnNewPop(EntityPopManager.instance.redWorkerPopData, true);
		SpawnNewPop(EntityPopManager.instance.redMeleePopData, true);
		SpawnNewPop(EntityPopManager.instance.redRangedPopData, true);
	}

	//instantiate and spawn entity
	void SpawnNewPop(EntityPopData popToSpawn, bool freeCost)
	{
		if (!freeCost)
		{
			//Debug.LogWarning("pop spawned, pop need: " + pop.PopNeed() + " | pop cost: " + pop.PopData().resourceCost);
			poIController.accumilatedResources -= popToSpawn.PopData().resourceCost;
		}
		else
		{
			//Debug.LogWarning("pop spawned, pop need: " + pop.PopNeed() + " | pop cost: 0");
		}

		EntityStats entity = poIController.SpawnNewEntity();
		entity._Data = popToSpawn.PopData();
		EntityPopManager.OnEntitySpawn(entity);
		GameManager.instance.UpdateTeamResourceCounter(poIController.poiOwner, poIController.accumilatedResources);
	}
}

public class GreenTeamPopulationStrategy : IPoIStrategies
{
	readonly PoIController poIController;
	readonly PoiData _Data;

	readonly CountdownTimer popGoalTimer;
	readonly CountdownTimer popGoalRandomizerTimer;

	bool hasSpawnedStartingEntities;

	public GreenTeamPopulationStrategy(PoIController poIController)
	{
		this.poIController = poIController;
		_Data = poIController._PoiData;
		hasSpawnedStartingEntities = false;

		popGoalTimer = new CountdownTimer(3f);
		popGoalTimer.OnTimerStart += () => RunPopulationLogic();
		popGoalTimer.OnTimerStop += () => Start();
		popGoalTimer.OnTimerCancel += () => Stop();

		popGoalRandomizerTimer = new CountdownTimer(15f);
		popGoalRandomizerTimer.OnTimerStart += () => EntityPopManager.instance.RandomizeGreenTeamPopulationGoals();
		popGoalRandomizerTimer.OnTimerStop += () => Start();
		popGoalRandomizerTimer.OnTimerCancel += () => Stop();
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

		EntityPopManager.instance.UpdateGreenTeamPopData(poIController.accumilatedResources);
		EntityPopData popToSpawn = EntityPopManager.instance.GetMostNeededGreenTeamPop();

		if (popToSpawn.PopNeed() <= 5)
		{
			//Debug.LogWarning("no pop spawned, pop needs not high enough: " + popToSpawn.PopNeed() + " <= 5");
			return;
		}
		else if (popToSpawn.CurrentPop() >= 1)
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

	void SpawnStartingEntities()
	{
		hasSpawnedStartingEntities = true;

		SpawnNewPop(EntityPopManager.instance.greenWorkerPopData, true);
		SpawnNewPop(EntityPopManager.instance.greenMeleePopData, true);
		SpawnNewPop(EntityPopManager.instance.greenRangedPopData, true);
	}

	//instantiate and spawn entity
	void SpawnNewPop(EntityPopData popToSpawn, bool freeCost)
	{
		if (!freeCost)
		{
			//Debug.LogWarning("pop spawned, pop need: " + pop.PopNeed() + " | pop cost: " + pop.PopData().resourceCost);
			poIController.accumilatedResources -= popToSpawn.PopData().resourceCost;
		}
		else
		{
			//Debug.LogWarning("pop spawned, pop need: " + pop.PopNeed() + " | pop cost: 0");
		}

		EntityStats entity = poIController.SpawnNewEntity();
		entity._Data = popToSpawn.PopData();
		EntityPopManager.OnEntitySpawn(entity);
		GameManager.instance.UpdateTeamResourceCounter(poIController.poiOwner, poIController.accumilatedResources);
	}
}
