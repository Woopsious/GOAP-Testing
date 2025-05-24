using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using static EntityData;

public class PoIController : MonoBehaviour
{
	public PoiData _PoiData;
	private MeshRenderer meshRenderer;
	private SphereCollider entityDetection;

	public EntityTeam previousPoiOwner;
	public EntityTeam poiOwner;

	public int accumilatedResources;
	public bool resourcesNeedTransfering;

	public List<EntityStats> entitiesInRange = new List<EntityStats>();

	[SerializeField] private EntityStats entityCurrentlyCapturing;

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
		previousPoiOwner = EntityTeam.neutral;
		UpdatePoiOwner(_PoiData.startingOwner);

		poIBehaviour = new HashSet<IPoIStrategies> 
		{
			new PoiResourcesStrategy(this),
			//new PoiHealFriendliesStrategy(this),
		};

		if (_PoiData.isTeamHomeBase)
			poIBehaviour.Add(new HomeBasePopulationStrategy(this));

		foreach (IPoIStrategies poIStrategies in poIBehaviour)
			poIStrategies.Start();
	}

	private void OnTriggerEnter(Collider other)
	{
		TryAddEntityToEntitiesInRange(other.GetComponent<EntityStats>());
	}
	private void OnTriggerExit(Collider other)
	{
		TryRemoveEntitiesFromEntitiesInRange(other.GetComponent<EntityStats>());
	}

	//track entities in range of poi
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

		for (int i = 0; i < entitiesInRange.Count; i++)
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
	void ClearUpDeadEntities(EntityStats entity)
	{
		for (int i = entitiesInRange.Count - 1; i >= 0; i--)
		{
			if (entitiesInRange[i] == entity || entitiesInRange[i] == null) //remove possible null refs
				entitiesInRange.RemoveAt(i);
		}

		UpdateTeamCounts();
	}

	//capturing pois
	public bool PoiCapturable(EntityStats entityChecking)
	{
		if (entityChecking._Data.team == poiOwner) return false;

		if (entityChecking == entityCurrentlyCapturing || entityCurrentlyCapturing == null)
			return true;
		else 
			return false;
	}
	public void UpdateEntityCapturingPoint(EntityStats entity)
	{
		entityCurrentlyCapturing = entity;
	}

	public void CapturePoi(EntityTeam newOwner)
	{
		previousPoiOwner = poiOwner;
		UpdatePoiOwner(newOwner);
	}
	void UpdatePoiOwner(EntityTeam newOwner)
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
		GameManager.OnPoiCapture(this);
	}

	//transfering poi resources
	public bool PoiNeedsResourceTransfer()
	{
		if (_PoiData.isTeamHomeBase) return false;
		if (accumilatedResources < 200) return false;
		else return true;
	}
}