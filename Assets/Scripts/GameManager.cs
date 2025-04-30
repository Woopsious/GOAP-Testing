using System;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class GameManager : MonoBehaviour
{
	public static GameManager instance;

	public static event Action<GameObject> OnEntityDeathEvent;

	[Header("Global Locations")]
	public GameObject foodShack;

	private void Awake()
	{
		instance = this;
	}

	public static void OnEntityDeath(GameObject entity)
	{
		OnEntityDeathEvent.Invoke(entity);
	}
}
