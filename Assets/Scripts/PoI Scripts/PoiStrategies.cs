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
	readonly PoIController poIController;
	readonly PoiData _Data;

	CountdownTimer timer;
	bool captureInProgress;

	public PoiCapture(PoIController poIController, float duration)
	{
		this.poIController = poIController;
		poIController._PoiData = _Data;
		timer = new CountdownTimer(duration);
		timer.OnTimerStart += () => StartCapture();
		timer.OnTimerStop += () => FinishCapture();
	}

	public void Start()
	{
		timer.Start();
	}

	public void Update(float deltaTime)
	{
		timer.Tick(deltaTime, false);
	}

	public void StartCapture()
	{
		captureInProgress = true;
	}
	public void CancelCapture()
	{
		captureInProgress = false;
	}

	public void FinishCapture()
	{
		captureInProgress = false;
	}
}

public class PoiAddResources : IPoIStrategies
{
	CountdownTimer timer;

	public PoiAddResources(float duration)
	{
		timer = new CountdownTimer(duration);
		timer.OnTimerStart += () => AddResources();
		timer.OnTimerStop += () => AddResources();
	}

	public void Start()
	{
		timer.Start();
	}

	public void Update(float deltaTime)
	{
		timer.Tick(deltaTime, false);
	}

	public void AddResources()
	{

	}
}
