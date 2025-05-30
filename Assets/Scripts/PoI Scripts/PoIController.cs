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

	[SerializeField] private EntityStats entityCurrentlyCapturing;

	public int redTeamEntitiesCount;
	public int greenTeamEntitiesCount;
	public int playerTeamEntitiesCount;

	public HashSet<IPoIStrategies> poIBehaviour;

	public GameObject EntityPrefab;

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
		};

		if (_PoiData.isTeamHomeBase)
			poIBehaviour.Add(new TeamPopStrategy(this));

		foreach (IPoIStrategies poIStrategies in poIBehaviour)
			poIStrategies.Start();
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
		AiDirector.OnPoiCapture(this);
	}

	//transfering poi resources
	public bool PoiNeedsResourceTransfer()
	{
		if (_PoiData.isTeamHomeBase) return false;
		if (accumilatedResources < 200) return false;
		else return true;
	}

	//entitySpawning
	public EntityStats SpawnNewEntity()
	{
		GameObject go = Instantiate(EntityPrefab, gameObject.transform.position, Quaternion.identity);
		go.transform.SetParent(null);
		return go.GetComponent<EntityStats>();
	}
}