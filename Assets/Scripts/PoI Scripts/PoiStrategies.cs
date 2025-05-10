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

public class PoiCapture : IPoIStrategies
{
	/// <summary>
	/// poi capture could/should be some sort of interact action entities can use through entity actions
	/// or a func called via poi controller (figure it out when it comes to it)
	/// account for starting, cancelling and completing 
	/// </summary>

	readonly PoIController poIController;
	readonly PoiData _Data;

	CountdownTimer timer;

	bool CaptureInProgress => EntityCapturingPoint != null && EntityCapturingPoint._Data.team != poIController.poiOwner;
	EntityStats EntityCapturingPoint => poIController.entityCurrentlyCapturing;

	public PoiCapture(PoIController poIController)
	{
		this.poIController = poIController;
		_Data = poIController._PoiData;

		timer = new CountdownTimer(_Data.timeToCapture);
		timer.OnTimerStart += () => StartCapture();
		timer.OnTimerStop += () => CompleteCapture();
		timer.OnTimerCancel += () => CancelCapture();
	}

	public void Update(float deltaTime)
	{
		if (CaptureInProgress && timer.IsRunning)
		{
			timer.Tick(deltaTime, false);
		}
		else if (CaptureInProgress && timer.IsFinished)
		{
			timer.Start();
		}
		else if (!CaptureInProgress && timer.IsRunning)
		{
			timer.Cancel();
		}
	}

	public void StartCapture()
	{
		//noop
	}
	public void CancelCapture()
	{
		poIController.entityCurrentlyCapturing = null;
	}

	public void CompleteCapture()
	{
		poIController.UpdatePoiOwner(EntityCapturingPoint._Data.team);
		poIController.entityCurrentlyCapturing = null;
	}
}

public class PoiAddResources : IPoIStrategies
{
	readonly PoIController poIController;
	readonly PoiData _Data;

	CountdownTimer timer;

	public PoiAddResources(PoIController poIController)
	{
		this.poIController = poIController;
		_Data = poIController._PoiData;

		timer = new CountdownTimer(_Data.ResourcesTimerCooldown);
		timer.OnTimerStart += () => AddResources();
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

	public void AddResources()
	{
		poIController.accumilatedResources += _Data.ResourcesProvided;

		//Debug.LogError("poi res timer add resources");
	}
}
