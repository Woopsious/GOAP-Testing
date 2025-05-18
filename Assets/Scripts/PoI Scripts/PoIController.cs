using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using static EntityData;

public class PoIController : MonoBehaviour, IInteractable
{
	public PoiData _PoiData;
	private MeshRenderer meshRenderer;
	private SphereCollider entityDetection;

	public EntityTeam poiOwner;

	public int accumilatedResources;
	public bool resourcesNeedTransfering;

	public List<EntityStats> entitiesInRange = new List<EntityStats>();

	public EntityStats entityCurrentlyCapturing;

	public int redTeamEntitiesCount;
	public int greenTeamEntitiesCount;
	public int playerTeamEntitiesCount;

	public HashSet<IPoIStrategies> poIBehaviour;

	public Material neutralTeamMaterial;
	public Material redTeamMaterial;
	public Material greenTeamMaterial;
	public Material playerTeamMaterial;

	private void Awake()
	{
		if (_PoiData == null)
			Debug.LogError("Poi Data not set for gameobject: " + gameObject.name);

		name = _PoiData.PoiName;
		meshRenderer = GetComponent<MeshRenderer>();
		entityDetection = GetComponent<SphereCollider>();
		entityDetection.isTrigger = true;
		entityDetection.radius = _PoiData.PoiDetectionRadius;
	}

	private void OnEnable()
	{
		GameManager.OnEntityDeathEvent += ClearUpDeadEntities;
	}
	private void OnDisable()
	{
		GameManager.OnEntityDeathEvent -= ClearUpDeadEntities;
	}

	private void Start()
	{
		Initilize();
	}
	private void Update()
	{
		foreach(IPoIStrategies poIStrategies in poIBehaviour)
		{
			poIStrategies.Update(Time.deltaTime);
		}
	}

	void Initilize()
	{
		UpdatePoiOwner(_PoiData.startingOwner);

		poIBehaviour = new HashSet<IPoIStrategies> 
		{
			new PoiResourcesStrategy(this),
			new PoiHealFriendliesStrategy(this),
		};

		foreach (IPoIStrategies poIStrategies in poIBehaviour)
		{
			poIStrategies.Start();
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		TryAddEntityToEntitiesInRange(other.GetComponent<EntityStats>());
	}
	private void OnTriggerExit(Collider other)
	{
		TryRemoveEntitiesFromEntitiesInRange(other.GetComponent<EntityStats>());
	}
	void TryAddEntityToEntitiesInRange(EntityStats entity)
	{
		if (entity == null)
			return;

		if (entitiesInRange.Count == 0)
		{
			entitiesInRange.Add(entity);
			UpdateTeamCounts();
			return;
		}

		for (int i = 0; i < entitiesInRange.Count; i++)
		{
			if (entitiesInRange[i] == entity)
				continue;
			else
			{
				entitiesInRange.Add(entity);
				UpdateTeamCounts();
			}
		}
	}
	void TryRemoveEntitiesFromEntitiesInRange(EntityStats entity)
	{
		if (entity == null)
			return;

		for (int i = entitiesInRange.Count - 1; i >= 0; i--)
		{
			if (entitiesInRange[i] == entity)
			{
				entitiesInRange.RemoveAt(i);
				UpdateTeamCounts();
			}
		}
	}

	void UpdateTeamCounts()
	{
		redTeamEntitiesCount = 0;
		greenTeamEntitiesCount = 0;
		playerTeamEntitiesCount = 0;

		for (int i = 0;i < entitiesInRange.Count; i++)
		{
			EntityTeam team = entitiesInRange[i]._Data.team;

			switch (team)
			{
				case EntityTeam.neutral:
				Debug.LogError("team neutral this shouldnt happen");
				break;

				case EntityTeam.redTeam:
				redTeamEntitiesCount++;
				break;

				case EntityTeam.greenTeam:
				greenTeamEntitiesCount++;
				break;

				case EntityTeam.playerTeam:
				playerTeamEntitiesCount++;
				break;
			}
		}
	}
	public void UpdatePoiOwner(EntityTeam newOwner)
	{
		poiOwner = newOwner;

		switch (newOwner)
		{
			case EntityTeam.neutral:
			meshRenderer.sharedMaterial = neutralTeamMaterial;
			break;
			case EntityTeam.redTeam:
			meshRenderer.sharedMaterial = redTeamMaterial;
			break;
			case EntityTeam.greenTeam:
			meshRenderer.sharedMaterial = greenTeamMaterial;
			break;
		}

		GameManager.OnPoiCapture(gameObject);
	}

	void ClearUpDeadEntities(GameObject obj)
	{
		EntityStats entity;

		if (obj.GetComponent<EntityStats>() == null)
			return;
		else
			entity = obj.GetComponent<EntityStats>();

		for (int i = entitiesInRange.Count - 1; i >= 0; i--)
		{
			if (entitiesInRange[i] == entity || entitiesInRange[i] == null) //remove possible null refs
				entitiesInRange.RemoveAt(i);
		}

		UpdateTeamCounts();
	}

	public void StartInteract(EntityStats entityInteracting)
	{
		entityCurrentlyCapturing = entityInteracting;

		Debug.LogError("start capture");
	}

	public void CancelInteract(EntityStats entityInteracting)
	{
		entityCurrentlyCapturing = null;

		Debug.LogError("cancel capture");
	}

	public void CompleteInteract(EntityStats entityInteracting)
	{
		UpdatePoiOwner(entityInteracting._Data.team);
		entityCurrentlyCapturing = null;

		Debug.LogError("complete capture");
	}
}