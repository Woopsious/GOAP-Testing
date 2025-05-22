using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class EntitySensor : MonoBehaviour
{
	/// <summary>
	/// sensor types work as they are now, needing 1 target to be set per sensor
	/// 
	/// attack sensors focus on finding attack targets fo
	/// </summary>


	[Header("Sensor Info")]
	[SerializeField] SensorType sensorType;
	public enum SensorType
	{
		flee, chase, attackSensorOne, attackSensorTwo, closestFriendlyPoi, closestEnemyPoi
	}

	[SerializeField] float detectionRadius = 5f;
	[SerializeField] float timerInterval = 1f;

	EntityBrain entityBrain;
	EntityData.EntityTeam entityTeam;
	SphereCollider detectionRange;

	public event Action<TargetData, SensorType> OnTargetChanged = delegate { };

	[Header("Target Info")]
	public List<TargetData> targetsInRange = new List<TargetData>();

	public TargetData target;
	Vector3 lastKnownPosition;
	CountdownTimer timer;

	//logic for beliefs
	public Vector3 TargetPosition => target.obj ? target.obj.transform.position : Vector3.zero;
	public bool IsTargetInRange => TargetPosition != Vector3.zero;

	[Header("Attack Sensor Info")]
	[SerializeField] EntityAttackData attackData;

	void Awake()
	{
		entityBrain = GetComponentInParent<EntityBrain>();
		EntityStats stats = GetComponentInParent<EntityStats>();
		entityTeam = stats._Data.team;

		detectionRange = GetComponent<SphereCollider>();
		detectionRange.isTrigger = true;
		detectionRange.radius = detectionRadius;
	}

	void OnEnable()
	{
		GameManager.OnEntityDeathEvent += ClearDeadEntitiesFromTargetList;
	}
	void OnDisable()
	{
		GameManager.OnEntityDeathEvent -= ClearDeadEntitiesFromTargetList;
	}

	void Start()
	{
		timer = new CountdownTimer(timerInterval);
		timer.OnTimerStop += () => {
			UpdateSensorLogic();
			timer.Start();
		};
		timer.Start();
	}
	void Update()
	{
		timer.Tick(Time.deltaTime, false);
	}

	public void UpdateSensorSettings(SensorType sensorType, float detectionRadius)
	{
		this.sensorType = sensorType;
		UpdateSensorSettings(detectionRadius);
	}
	public void UpdateSensorSettings(EntityAttackData attackData)
	{
		this.attackData = attackData;
		UpdateSensorSettings(attackData.attackMaxRange);
	}
	public void UpdateSensorSettings(float detectionRadius)
	{
		this.detectionRadius = detectionRadius;
		detectionRange.radius = detectionRadius;
	}

	void UpdateSensorLogic()
	{
		SortTargetsInSensorRange();
		UpdateSensorTargets();
	}
	void SortTargetsInSensorRange()
	{
		for (int i = targetsInRange.Count - 1; i >= 0; i--)
		{
			if (targetsInRange[i].obj == null)
				targetsInRange.RemoveAt(i);
			else
				targetsInRange[i].targetDistance = GetTargetDistance(targetsInRange[i].obj);
		}

		targetsInRange.Sort((a, b) => a.targetDistance.CompareTo(b.targetDistance));
	}

	//sensor type target logic
	void UpdateSensorTargets()
	{
		if (sensorType == SensorType.flee || sensorType == SensorType.chase)
			UpdateOtherSensorsTargets();

		else if (sensorType == SensorType.attackSensorOne || sensorType == SensorType.attackSensorTwo)
			UpdateAttackSensorTargets();
		else if (sensorType == SensorType.closestFriendlyPoi || sensorType == SensorType.closestEnemyPoi)
			UpdatePoiDetectorTargets();
		else
			Debug.LogError("No matching sensor type");
	}
	void UpdateOtherSensorsTargets()
	{
		int index = FindClosestTarget();
		if (index >= 0)
			target = targetsInRange[index];
		else
			target.ClearTarget();

		//attack sensors shouldnt need to worry about tracking last known pos as chase/flee sensors handle that
		if (IsTargetInRange && (lastKnownPosition != TargetPosition || lastKnownPosition != Vector3.zero))
		{
			OnTargetChanged.Invoke(target, sensorType);
			lastKnownPosition = TargetPosition;
		}
	}
	void UpdateAttackSensorTargets()
	{
		int index = FindClosestTargetWithinValues(attackData.attackMinRange, attackData.attackMaxRange);
		if (index >= 0)
			target = targetsInRange[index];
		else
		{
			index = FindClosestTarget();
			if (index >= 0)
				target = targetsInRange[index];
			else
				target.ClearTarget();
		}

		OnTargetChanged.Invoke(target, sensorType);
	}
	void UpdatePoiDetectorTargets()
	{
		if (sensorType == SensorType.closestFriendlyPoi)
		{
			int index = FindClosestFriendlyPoi();
			if (index >= 0)
				target = targetsInRange[index];
			else
				target.ClearTarget();
		}
		else if (sensorType == SensorType.closestEnemyPoi)
		{
			int index = FindClosestEnemyPoi();
			if (index >= 0)
				target = targetsInRange[index];
			else
				target.ClearTarget();
		}

		//attack sensors shouldnt need to worry about tracking last known pos as chase/flee sensors handle that
		if (IsTargetInRange && (lastKnownPosition != TargetPosition || lastKnownPosition != Vector3.zero))
		{
			OnTargetChanged.Invoke(target, sensorType);
			lastKnownPosition = TargetPosition;
		}
	}

	//try fetch targets matching params
	int FindClosestTarget()
	{
		if (targetsInRange.Count <= 0)
			return -10;
		else
			return 0;
	}
	int FindClosestTargetWithinValues(float min, float max)
	{
		for (int i = 0; i < targetsInRange.Count; i++)
		{
			if (targetsInRange[i].targetDistance >= min && targetsInRange[i].targetDistance <= max)
				return i;
		}
		return -10;
	}
	int FindClosestEnemyPoi()
	{
		for (int i = 0; i < targetsInRange.Count; i++)
		{
			PoIController poIController = targetsInRange[i].GetTarget<PoIController>(); 

			if (poIController == null) continue;

			if (poIController.poiOwner != entityTeam)
				return i;
		}
		return -10;
	}
	int FindClosestFriendlyPoi()
	{
		for (int i = 0; i < targetsInRange.Count; i++)
		{
			PoIController poIController = targetsInRange[i].GetTarget<PoIController>();

			if (poIController == null) continue;

			if (poIController.poiOwner == entityTeam)
				return i;
		}
		return -10;
	}

	void OnTriggerEnter(Collider other)
	{
		if (sensorType == SensorType.closestFriendlyPoi || sensorType == SensorType.closestEnemyPoi)
		{
			if (other.GetComponent<PoIController>() == null) return;
			AddTargetToList(other.gameObject, TargetData.TargetType.poi);
		}
		else
		{
			if (other.GetComponent<EntityStats>() == null) return;
			EntityStats entity = other.GetComponent<EntityStats>();

			if (entityTeam != entity._Data.team)
				AddTargetToList(entity.gameObject, TargetData.TargetType.entity);
		}
	}
	void OnTriggerExit(Collider other)
	{
		if (sensorType == SensorType.closestFriendlyPoi || sensorType == SensorType.closestEnemyPoi)
		{
			if (other.GetComponent<PoIController>() == null) return;
			RemoveTargetFromList(other.gameObject);
		}
		else
		{
			if (other.GetComponent<EntityStats>() == null) return;
			EntityStats entity = other.GetComponent<EntityStats>();

			if (entityTeam != entity._Data.team)
				RemoveTargetFromList(entity.gameObject);
		}
	}

	void AddTargetToList(GameObject obj, TargetData.TargetType targetType)
	{
		if (targetsInRange.Count == 0) //list empty no need to check
		{
			targetsInRange.Add(new(targetType, obj, GetTargetDistance(obj)));
			return;
		}

		for (int i = 0; i < targetsInRange.Count; i++)
		{
			if (targetsInRange[i].obj == obj)
				continue;
			else
				targetsInRange.Add(new(targetType, obj, GetTargetDistance(obj)));
		}
	}
	void RemoveTargetFromList(GameObject obj)
	{
		for (int i = targetsInRange.Count - 1; i >= 0; i--)
		{
			if (targetsInRange[i].obj == obj)
				targetsInRange.RemoveAt(i);
		}
	}

	float GetTargetDistance(GameObject target)
	{
		float aggroDistance = Vector3.Distance(transform.position, target.transform.position);
		return aggroDistance;
	}

	void ClearDeadEntitiesFromTargetList(GameObject entity)
	{
		for (int i = targetsInRange.Count - 1; i >= 0; i--)
		{
			if (targetsInRange[i].obj == entity || targetsInRange[i].obj == null)
				targetsInRange.RemoveAt(i); //also remove possible null refs
		}
	}

	void OnDrawGizmos()
	{
		if (sensorType == SensorType.attackSensorOne ||  sensorType == SensorType.attackSensorTwo)
			Gizmos.color = target.obj ? Color.blue : Color.green;
		else
			Gizmos.color = IsTargetInRange ? Color.red : Color.green;

		Gizmos.DrawWireSphere(transform.position, detectionRadius);
	}
}

[Serializable]
public class TargetData
{
	public GameObject obj;

	public PoIController poiController;
	public EntityBrain entityBrain;

	public TargetType type;
	public enum TargetType
	{
		entity, poi
	}

	public float targetDistance;

	public TargetData(TargetType type, GameObject obj, float targetDistance)
	{
		if (type == TargetType.poi)
			poiController = obj.GetComponent<PoIController>();
		else if (type == TargetType.entity)
			entityBrain = obj.GetComponent<EntityBrain>();

		this.obj = obj;
		this.type = type;
		this.targetDistance = targetDistance;
	}

	public T GetTarget<T>()
	{
		if (type == TargetType.poi)
			return (T)Convert.ChangeType(poiController, typeof(T));
		else if (type == TargetType.entity)
			return (T)Convert.ChangeType(entityBrain, typeof(T));
		else
			return (T)Convert.ChangeType(null, typeof(T));
	}

	public void ClearTarget()
	{
		obj = null;
		poiController = null;
		entityBrain = null;
	}
}