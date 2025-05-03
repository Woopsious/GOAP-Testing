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
	/// something entiites can do to spawn another entitiy
	/// either via collecting something like food as a resource to spawn new entity
	/// something like having a pop cap for each team, if pop gets too low entitiy can chose to create more on a timer etc...
	/// 
	/// ENTITY POI's
	/// multiple places to heal or locations to fight over like resources or just random place with more activitiy
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
