using System;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class GameManager : MonoBehaviour
{
	public static GameManager instance;

	public static event Action<GameObject> OnEntityDeathEvent;

	public static event Action<GameObject> OnPoiCaptureEvent;

	[Header("Global Locations")]
	public GameObject foodShack;

	/// <summary>
	/// TODO:
	/// ENTITIY REQUEST/ANSWER HELP CALL
	/// add a way for entities to call for help from surrounding ones as a RequestHelpCall strategy
	/// add a way for entities to answer a call for help as a AnswerHelpCall strategy
	/// need beliefs for it to trigger these things like getting attacked by too many enemies or low on health etc..
	/// a way to filter out too many entities answering a call or none answering a call, possibly via checking current goals
	/// adding a sensor to detect friendlies in call range
	/// 
	/// ENTITY SPAWNING
	/// 
	/// ENTITY POI's
	/// team base's/neutral bases: (ATM just get team bases working)
	///		supplies resources entities can use to replenish there population and spawn new ones here
	///		restore health at these + capture and defend them.
	/// ???resoure nodes worker entities can get extra resources from.
	/// 
	/// 
	/// UPDATE ENTITY TYPES/BRAINS:
	/// have a worker (or different worker types) entity type that focuses on collecting resources or using them etc...
	/// have multiple different offensive bot types like ranged and melee
	///	???a commander type that can organise regular combat type entities (ATM worry about adding simpler things)
	/// 
	/// MIGHT NOT NEED BUT COULD CONSIDER ADDING AN AI DIRECTOR:
	/// a higher level system that will direct entities to achieve certian goals
	/// send entities to capture points based on info of all capture points (update CapturePoi goal priority for worker brains by +- it)
	/// help organize combat entities to attack a poi as a group or defend it based on locational positions of all entities.
	/// manage pop counts of both teams based on current pop + resources accumalated. (update a SpawnEntity goal priority for worker brains)
	/// 
	/// </summary>

	private void Awake()
	{
		instance = this;
	}

	public static void OnEntityDeath(GameObject entity)
	{
		OnEntityDeathEvent?.Invoke(entity);
	}

	public static void OnPoiCapture(GameObject poi)
	{
		OnPoiCaptureEvent?.Invoke(poi);
	}
}
