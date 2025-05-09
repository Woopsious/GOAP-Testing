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
	bool captureInProgress;

	public PoiCapture(PoIController poIController)
	{
		this.poIController = poIController;
		_Data = poIController._PoiData;

		timer = new CountdownTimer(_Data.timeToCapture);
		timer.OnTimerStart += () => StartCapture();
		timer.OnTimerStop += () => CompleteCapture();
	}

	public void Start()
	{
		timer.Start();
		Debug.LogError("poi capture timer start");
	}

	public void Update(float deltaTime)
	{
		timer.Tick(deltaTime, false);

		Debug.LogError("poi capture timer update");
	}

	public void StartCapture()
	{
		captureInProgress = true;
	}
	public void CancelCapture()
	{
		captureInProgress = false;
	}

	public void CompleteCapture()
	{
		captureInProgress = false;
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

		Debug.LogError("poi res timer start");
	}

	public void Update(float deltaTime)
	{
		timer.Tick(deltaTime, false);

		Debug.LogError("poi res timer update");
	}

	public void AddResources()
	{
		poIController.accumilatedResources += _Data.ResourcesProvided;

		Debug.LogError("poi res timer add resources");
	}
}
