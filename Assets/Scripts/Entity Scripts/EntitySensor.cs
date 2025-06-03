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
		fleeSensor, chaseSensor, friendlySensor, attackSensorOne, attackSensorTwo, friendlyPoiSensor, enemyPoiSensor
	}

	float detectionRadius;
	float timerInterval;

	EntityData.EntityTeam entityTeam;
	SphereCollider detectionRange;

	public event Action<TargetData, SensorType> OnTargetChanged = delegate { };

	[Header("Target Info")]
	public List<TargetData> targetsInRange = new List<TargetData>();
	public List<TargetData> friendliesInRange = new List<TargetData>();

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
		if (sensorType == SensorType.friendlySensor) return; //atm dont need to constantly run sensor logic on friendly targets
		timer.Tick(Time.deltaTime);
	}

	public void UpdateSensorSettings(SensorType sensorType, EntityAttackData attackData)
	{
		this.attackData = attackData;
		UpdateSensorSettings(sensorType, attackData.attackMaxRange);
	}
	public void UpdateSensorSettings(SensorType sensorType, float detectionRadius)
	{
		this.sensorType = sensorType;

		if (sensorType == SensorType.fleeSensor || sensorType == SensorType.chaseSensor)
			timerInterval = 0.25f;
		else if (sensorType == SensorType.friendlyPoiSensor || sensorType == SensorType.enemyPoiSensor)
			timerInterval = 1f;
		else
			timerInterval = 0.5f;

		this.detectionRadius = detectionRadius;
		detectionRange.radius = detectionRadius;

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
		UpdateSensorTargets();
	}
	void SortTargetsInSensorRange()
	{
		if (sensorType == SensorType.friendlySensor) return; //atm dont need to constantly run sensor logic on friendly targets

		for (int i = targetsInRange.Count - 1; i >= 0; i--)
		{
			if (targetsInRange[i].obj == null)
				targetsInRange.RemoveAt(i);
			else
				targetsInRange[i].UpdateTargetDistance(transform.position);
		}

		targetsInRange.Sort((a, b) => a.targetDistance.CompareTo(b.targetDistance));
	}

	//new fetch target
	public EntityStats GetClosestEntityWithinRange(EntityAttackData attackData)
	{
		EntityStats foundTarget = null;

		for (int i = 0; i < targetsInRange.Count; i++)
		{
			if (targetsInRange[i].targetDistance >= attackData.attackMinRange && targetsInRange[i].targetDistance <= attackData.attackMaxRange)
			{
				foundTarget = targetsInRange[i].entity;

				Debug.LogError("found target entity: " + foundTarget);
			}
		}
		return foundTarget;
	}

	//sensor type target logic
	void UpdateSensorTargets()
	{
		if (sensorType == SensorType.fleeSensor || sensorType == SensorType.chaseSensor)
			UpdateOtherSensorsTargets();

		else if (sensorType == SensorType.attackSensorOne || sensorType == SensorType.attackSensorTwo)
			UpdateAttackSensorTargets();
		else if (sensorType == SensorType.friendlyPoiSensor || sensorType == SensorType.enemyPoiSensor)
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
		if (sensorType == SensorType.friendlyPoiSensor)
		{
			int index = FindClosestFriendlyPoi();
			if (index >= 0)
				target = targetsInRange[index];
			else
				target.ClearTarget();
		}
		else if (sensorType == SensorType.enemyPoiSensor)
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
			PoIController poIController = targetsInRange[i].poi; 

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
			PoIController poIController = targetsInRange[i].poi;

			if (poIController == null) continue;

			if (poIController.poiOwner == entityTeam)
				return i;
		}
		return -10;
	}

	void OnTriggerEnter(Collider other)
	{
		if (sensorType == SensorType.friendlyPoiSensor || sensorType == SensorType.enemyPoiSensor)
		{
			if (other.GetComponent<PoIController>() == null) return;
			AddTargetToList(targetsInRange, other.gameObject, TargetData.TargetType.poi);
		}
		else
		{
			if (other.GetComponent<EntityStats>() == null) return;
			EntityStats entity = other.GetComponent<EntityStats>();

            if (entityTeam == entity._Data.team)
            {
				if (sensorType == SensorType.chaseSensor)
					AddTargetToList(friendliesInRange, entity.gameObject, TargetData.TargetType.entity);
				else if (sensorType == SensorType.friendlySensor)
					AddTargetToList(targetsInRange, entity.gameObject, TargetData.TargetType.entity);
			}
			else if (sensorType != SensorType.friendlySensor)
				AddTargetToList(targetsInRange, entity.gameObject, TargetData.TargetType.entity);
		}
	}
	void OnTriggerExit(Collider other)
	{
		if (sensorType == SensorType.friendlyPoiSensor || sensorType == SensorType.enemyPoiSensor)
		{
			if (other.GetComponent<PoIController>() == null) return;
			RemoveTargetFromList(targetsInRange, other.gameObject);
		}
		else
		{
			if (other.GetComponent<EntityStats>() == null) return;
			EntityStats entity = other.GetComponent<EntityStats>();

			if (entityTeam == entity._Data.team)
			{
				if (sensorType == SensorType.chaseSensor)
					RemoveTargetFromList(friendliesInRange, entity.gameObject);
				else if (sensorType == SensorType.friendlySensor)
					RemoveTargetFromList(targetsInRange, entity.gameObject);
			}
			else if (sensorType != SensorType.friendlySensor)
				RemoveTargetFromList(targetsInRange, entity.gameObject);
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
				continue;
			else
				list.Add(new(targetType, obj, transform.position));
		}
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

	public PoIController poi;
	public EntityStats entity;

	public TargetType type;
	public enum TargetType
	{
		entity, poi
	}

	public float targetDistance { get; private set; }

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

	public void UpdateTargetDistance(Vector3 position)
	{
		targetDistance = Vector3.Distance(position, obj.transform.position);
	}

	public void ClearTarget()
	{
		obj = null;
		poi = null;
		entity = null;
	}
}