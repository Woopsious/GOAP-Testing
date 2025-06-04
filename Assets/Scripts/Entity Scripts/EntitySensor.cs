using System;
using System.Collections.Generic;
using UnityEngine;

public class EntitySensor : MonoBehaviour
{
	private EntityStats entityStats;

	[Header("Sensor Info")]
	[SerializeField] SensorType sensorType;
	public enum SensorType
	{
		chaseSensor, longRangeSensor, poiSensor
	}

	float detectionRadius;
	float timerInterval;

	EntityData.EntityTeam entityTeam;
	SphereCollider detectionRange;

	public event Action<SensorType> OnTargetChanged = delegate { };

	[Header("Target Info")]
	public List<TargetData> targetsInRange = new List<TargetData>();
	public List<TargetData> friendliesInRange = new List<TargetData>();

	public TargetData target;
	Vector3 lastKnownPosition;
	CountdownTimer timer;

	List<EntityAttackData> attackData;
	float fleeDistance;

	//logic for beliefs
	public Vector3 TargetPosition => target.obj ? target.obj.transform.position : Vector3.zero;
	public bool IsTargetInRange => TargetPosition != Vector3.zero;

	void Awake()
	{
		entityStats = GetComponentInParent<EntityStats>();
		detectionRange = GetComponent<SphereCollider>();
	}
	void Start()
	{
		entityTeam = entityStats._Data.team;
		detectionRange.isTrigger = true;
		detectionRange.radius = detectionRadius;
	}

	void OnEnable()
	{
		AiDirector.OnEntityDeathEvent += ClearDeadEntitiesFromTargetList;
	}
	void OnDisable()
	{
		AiDirector.OnEntityDeathEvent -= ClearDeadEntitiesFromTargetList;
	}

	void Update()
	{
		timer.Tick(Time.deltaTime);
	}

	public void UpdateSensorSettings(SensorType sensorType, float detectionRadius, List<EntityAttackData> attackData)
	{
		this.sensorType = sensorType;
		timerInterval = 0.25f;
		detectionRange.radius = detectionRadius;
		this.attackData = attackData;
		fleeDistance = entityStats._Data.fleeRange;

		for (int i = 0; i < attackData.Count; i++)
		{
			if (attackData[i].attackMinRange > fleeDistance)
				fleeDistance = attackData[i].attackMinRange;
		}

		timer = new CountdownTimer(timerInterval);
		timer.OnTimerStop += () => {
			UpdateSensorLogic();
			timer.Start();
		};
		timer.Start();
	}

	void UpdateSensorLogic()
	{
		SortTargetsInSensorRange();
		SortFriendliesInSensorRange();

		OnTargetChanged?.Invoke(sensorType);
	}
	void SortTargetsInSensorRange()
	{
		for (int i = targetsInRange.Count - 1; i >= 0; i--)
		{
			if (targetsInRange[i].obj == null)
				targetsInRange.RemoveAt(i);
			else
				targetsInRange[i].UpdateTargetDistance(transform.position);
		}

		targetsInRange.Sort((a, b) => a.TargetDistance.CompareTo(b.TargetDistance));
	}
	void SortFriendliesInSensorRange()
	{
		for (int i = friendliesInRange.Count - 1; i >= 0; i--)
		{
			if (friendliesInRange[i].obj == null)
				friendliesInRange.RemoveAt(i);
			else
				friendliesInRange[i].UpdateTargetDistance(transform.position);
		}

		friendliesInRange.Sort((a, b) => a.TargetDistance.CompareTo(b.TargetDistance));
	}

	//fetch targets based on whats needed
	public TargetData GetClosestEntityInFleeRange()
	{
		TargetData foundTarget = new(TargetData.TargetType.nullRef);

		for (int i = 0; i < targetsInRange.Count; i++)
		{
			if (targetsInRange[i].TargetDistance < entityStats._Data.fleeRange)
			{
				foundTarget = targetsInRange[i];
			}
		}

		return foundTarget;
	}
	public TargetData GetClosestEntity()
	{
		TargetData foundTarget = new(TargetData.TargetType.nullRef);

		if (targetsInRange.Count > 0)
			foundTarget = targetsInRange[0];

		return foundTarget;
	}
	public TargetData GetClosestTargetWithinRange(EntityAttackData attackData)
	{
		TargetData foundTarget = GetClosestEntity();

		if (foundTarget.TargetDistance >= attackData.attackMinRange && foundTarget.TargetDistance <= attackData.attackMaxRange)
			return foundTarget;
		else
		{
			foundTarget = new(TargetData.TargetType.nullRef);
			return foundTarget;
		}
	}
	public TargetData GetClosestEnemyPoi()
	{
		TargetData foundTarget = new(TargetData.TargetType.nullRef);

		for (int i = 0; i < targetsInRange.Count; i++)
		{
			if (foundTarget.poi.poiOwner != entityTeam)
				foundTarget = targetsInRange[i];
		}

		return foundTarget;
	}
	public TargetData GetClosestFriendlyPoi()
	{
		TargetData foundTarget = new(TargetData.TargetType.nullRef);

		for (int i = 0; i < targetsInRange.Count; i++)
		{
			if (foundTarget.poi.poiOwner == entityTeam)
				foundTarget = targetsInRange[i];
		}

		return foundTarget;
	}

	void OnTriggerEnter(Collider other)
	{
		if (sensorType == SensorType.poiSensor)
		{
			if (other.GetComponent<PoIController>() == null) return;
			PoIController poi = other.GetComponent<PoIController>();

			if (poi.poiOwner == entityTeam)
				AddTargetToList(friendliesInRange, other.gameObject, TargetData.TargetType.poi);
			else
				AddTargetToList(targetsInRange, other.gameObject, TargetData.TargetType.poi);
		}
		else
		{
			if (other.GetComponent<EntityStats>() == null) return;
			EntityStats entity = other.GetComponent<EntityStats>();

			if (entity._Data.team == entityTeam)
				AddTargetToList(friendliesInRange, other.gameObject, TargetData.TargetType.entity);
			else
				AddTargetToList(targetsInRange, other.gameObject, TargetData.TargetType.entity);
		}
	}
	void OnTriggerExit(Collider other)
	{
		if (sensorType == SensorType.poiSensor)
		{
			if (other.GetComponent<PoIController>() == null) return;
			PoIController poi = other.GetComponent<PoIController>();

			if (poi.poiOwner == entityTeam)
				RemoveTargetFromList(friendliesInRange, other.gameObject);
			else
				RemoveTargetFromList(targetsInRange, other.gameObject);
		}
		else
		{
			if (other.GetComponent<EntityStats>() == null) return;
			EntityStats entity = other.GetComponent<EntityStats>();

			if (entity._Data.team == entityTeam)
				RemoveTargetFromList(friendliesInRange, other.gameObject);
			else
				RemoveTargetFromList(targetsInRange, other.gameObject);
		}
	}

	void AddTargetToList(List<TargetData> list, GameObject obj, TargetData.TargetType targetType)
	{
		if (list.Count == 0) //list empty no need to check
		{
			list.Add(new(targetType, obj, transform.position));
			return;
		}

		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].obj == obj)
				return;
		}

		list.Add(new(targetType, obj, transform.position));
	}
	void RemoveTargetFromList(List<TargetData> list, GameObject obj)
	{
		for (int i = list.Count - 1; i >= 0; i--)
		{
			if (list[i].obj == obj)
				list.RemoveAt(i);
		}
	}

	void ClearDeadEntitiesFromTargetList(EntityStats entity)
	{
		for (int i = targetsInRange.Count - 1; i >= 0; i--)
		{
			if (targetsInRange[i].obj == entity.gameObject || targetsInRange[i].obj == null)
				targetsInRange.RemoveAt(i); //also remove possible null refs
		}

		for (int i = friendliesInRange.Count - 1; i >= 0; i--)
		{
			if (friendliesInRange[i].obj == entity.gameObject || friendliesInRange[i].obj == null)
				friendliesInRange.RemoveAt(i); //also remove possible null refs
		}
	}

	void OnDrawGizmos()
	{
		Gizmos.color = IsTargetInRange ? Color.red : Color.green;
		Gizmos.DrawWireSphere(transform.position, detectionRadius);

		if (attackData == null || attackData.Count == 0) return;

		Gizmos.color = Color.blue;
		Gizmos.DrawWireSphere(transform.position, attackData[0].attackMaxRange);
		Gizmos.DrawWireSphere(transform.position, attackData[1].attackMaxRange);
	}
}

[Serializable]
public class TargetData
{
	public GameObject obj;

	public PoIController poi;
	public EntityStats entity;

	public TargetType type;
	public enum TargetType
	{
		nullRef, entity, poi
	}

	public float TargetDistance { get; private set; }

	public TargetData(TargetType type, GameObject obj, Vector3 position)
	{
		if (type == TargetType.poi)
			poi = obj.GetComponent<PoIController>();
		else if (type == TargetType.entity)
			entity = obj.GetComponent<EntityStats>();

		this.obj = obj;
		this.type = type;
		UpdateTargetDistance(position);
	}
	public TargetData(TargetType type)
	{
		ClearTarget();
		TargetDistance = 0f;
	}

	public void UpdateTargetDistance(Vector3 position)
	{
		TargetDistance = Vector3.Distance(position, obj.transform.position);
	}

	public void ClearTarget()
	{
		obj = null;
		poi = null;
		entity = null;
	}
}