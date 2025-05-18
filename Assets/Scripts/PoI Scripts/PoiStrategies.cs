using System;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.AI;

public interface IPoIStrategies
{
	public void Start()
	{

	}

	public void Update(float deltaTime)
	{

	}

	public void Stop()
	{

	}
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
	}

	public void Start()
	{
		timer.Start();

		//Debug.LogError("poi res timer start");
	}

	public void Update(float deltaTime)
	{
		timer.Tick(deltaTime, false);

		//Debug.LogError("poi res timer update");
	}

	void AddResourcesToCapturePoint()
	{
		poIController.accumilatedResources += _Data.ResourcesProvided;
		CheckIfResourcesNeedMoving();

		//Debug.LogError("poi res timer add resources");
	}

	void CheckIfResourcesNeedMoving()
	{
		if (!_Data.isTeamHomeBase && poIController.accumilatedResources > 200)
			poIController.resourcesNeedTransfering = true;
		else
			poIController.resourcesNeedTransfering = false;
	}
}

public class PoiHealFriendliesStrategy : IPoIStrategies
{
	readonly PoIController poIController;
	readonly PoiData _Data;

	readonly CountdownTimer timer;

	public PoiHealFriendliesStrategy(PoIController poIController)
	{
		this.poIController = poIController;
		_Data = poIController._PoiData;

		timer = new CountdownTimer(_Data.HealTimerCooldown);
		timer.OnTimerStart += () => HealFriendlies();
		timer.OnTimerStop += () => Start();
	}

	public void Start()
	{
		timer.Start();
	}

	public void Update(float deltaTime)
	{
		timer.Tick(deltaTime, false);
	}

	public void HealFriendlies()
	{
		foreach(EntityStats entity in poIController.entitiesInRange)
		{
			if (entity._Data.team == poIController.poiOwner)
				entity.RecieveHealing(_Data.HealAmount);
		}
	}
}
