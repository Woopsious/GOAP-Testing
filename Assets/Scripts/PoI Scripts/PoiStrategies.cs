using System;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.AI;

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
		//noop
	}


	void AddResourcesToCapturePoint()
	{
		poIController.accumilatedResources += _Data.ResourcesProvided;
		CheckIfResourcesNeedMoving();
	}

	void CheckIfResourcesNeedMoving()
	{
		if (!_Data.isTeamHomeBase && poIController.accumilatedResources > 200)
			poIController.resourcesNeedTransfering = true;
		else
			poIController.resourcesNeedTransfering = false;
	}
}

public class HomeBasePopulationStrategy : IPoIStrategies
{
	readonly PoIController poIController;
	readonly PoiData _Data;

	readonly CountdownTimer timer;

	bool hasSpawnedStartingEntities;

	public HomeBasePopulationStrategy(PoIController poIController)
	{
		this.poIController = poIController;
		_Data = poIController._PoiData;
		hasSpawnedStartingEntities = false;

		timer = new CountdownTimer(5f);
		timer.OnTimerStart += () => RunPopulationLogic();
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
		//noop
	}

	//track pop counts for each type of entity type (use game manager + events to add/remove from lists, then read list counts here)

	//have pop limits/goals (eg: have 2 workers, 3 melee, 3 ranged, 2 dualists)
	//pop goals are semi random to have pop fluctuations (eg worker base goal = 15, random adjust = +- 3. final worker goal = 12-18)
	//goals scaled with how many resources currently banked + how many pois are controlled etc...
	//goals are reevaluated + checked every 15s to see if something needs spawning
	//goals are semi randomized every 120s

	//if under said goals follow a simple weighted scaling system
	//example: (goal Workers: 5, current workers: 3, worker need: 2x2.5f = 5 need) (goal melee: 8, current melee: 5, melee need: 3x1.5f = 4.5 need)
	//worker need is greater so a worker is spawned over other types etc...

	void RunPopulationLogic()
	{
		if (!_Data.isTeamHomeBase) return;
		TrySpawnStartingEntities();
	}

	void TrySpawnStartingEntities()
	{
		if (hasSpawnedStartingEntities) return;

		hasSpawnedStartingEntities = true;

		//spawn starting entities
	}

	void ChoseWhatToSpawnBasedOnExternalFactors()
	{

	}

	void UpdatePopulationGoals()
	{

	}
	void RandomizePopulationGoals()
	{

	}

	//spawn specific types of entities
	void SpawnWorkerEntity()
	{
		if (poIController.poiOwner == EntityData.EntityTeam.redTeam)
			SpawnNewEntity(Database.GetEntityFromDatabase(EntityData.EntityDataType.redWorker));
		else if (poIController.poiOwner == EntityData.EntityTeam.greenTeam)
			SpawnNewEntity(Database.GetEntityFromDatabase(EntityData.EntityDataType.greenWorker));
	}
	void SpawnMeleeCombatEntity()
	{
		if (poIController.poiOwner == EntityData.EntityTeam.redTeam)
			SpawnNewEntity(Database.GetEntityFromDatabase(EntityData.EntityDataType.redMelee));
		else if (poIController.poiOwner == EntityData.EntityTeam.greenTeam)
			SpawnNewEntity(Database.GetEntityFromDatabase(EntityData.EntityDataType.greenMelee));
	}

	//instantiate and spawn entity
	void SpawnNewEntity(EntityData entityData)
	{

	}
}
