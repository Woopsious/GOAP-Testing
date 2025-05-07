using System;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class GameManager : MonoBehaviour
{
	public static GameManager instance;

	public static event Action<GameObject> OnEntityDeathEvent;

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
	/// </summary>

	private void Awake()
	{
		instance = this;
	}

	public static void OnEntityDeath(GameObject entity)
	{
		OnEntityDeathEvent.Invoke(entity);
	}
}
