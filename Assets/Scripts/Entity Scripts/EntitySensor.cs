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

	SphereCollider sphereCollider;
	float detectionRadius = 0;
	float timerInterval;

	public event Action<SensorType> OnTargetChanged = delegate { };

	[Header("Target Info")]
	public List<TargetData> targetsInRange = new List<TargetData>();
	public List<TargetData> friendliesInRange = new List<TargetData>();

	public ISensorStrategy[] sensorBehaviours = new ISensorStrategy[0];
	CountdownTimer sensorBehaviourstimer;

	List<EntityAttackData> attackData;
	float fleeDistance;

	void Awake()
	{
		entityStats = GetComponentInParent<EntityStats>();
		sphereCollider = GetComponent<SphereCollider>();
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
		sensorBehaviourstimer.Tick(Time.deltaTime);
	}

	//sensor setup
	public void UpdateSensorSettings(float detectionRadius, List<EntityAttackData> attackData)
	{
		timerInterval = 0.25f;
		sphereCollider.radius = detectionRadius;
		this.detectionRadius = detectionRadius;
		this.attackData = attackData;
		fleeDistance = entityStats._Data.fleeRange;

		for (int i = 0; i < attackData.Count; i++)
		{
			if (attackData[i].attackMinRange > fleeDistance)
				fleeDistance = attackData[i].attackMinRange;
		}

		SetupSensorBehaviour();

		sensorBehaviourstimer = new CountdownTimer(timerInterval);
		sensorBehaviourstimer.OnTimerStop += () => {
			UpdateSensorLogic();
			sensorBehaviourstimer.Start();
		};
		sensorBehaviourstimer.Start();
	}
	void SetupSensorBehaviour()
	{
		if (sensorType == SensorType.chaseSensor)
		{
			sensorBehaviours = new ISensorStrategy[4];
			sensorBehaviours[0] = new FleeClosestEnemyEntityStrategy(this, entityStats._Data);
			sensorBehaviours[1] = new ClosestEnemyEntityStrategy(this);

			sensorBehaviours[0].OnTargetChanged += TargetChangedFromSensorBehaviour;
			sensorBehaviours[1].OnTargetChanged += TargetChangedFromSensorBehaviour;

			if (attackData.Count > 0)
			{
				sensorBehaviours[2] = new ClosestEntityToAttackStrategy(this, attackData[0]);
				sensorBehaviours[3] = new ClosestEntityToAttackStrategy(this, attackData[1]);

				sensorBehaviours[2].OnTargetChanged += TargetChangedFromSensorBehaviour;
				sensorBehaviours[3].OnTargetChanged += TargetChangedFromSensorBehaviour;
			}
		}
		else if (sensorType == SensorType.longRangeSensor)
		{
			//noop
		}
		else if (sensorType == SensorType.poiSensor)
		{
			sensorBehaviours = new ISensorStrategy[2];
			sensorBehaviours[0] = new ClosestEnemyPoiStrategy(this);
			sensorBehaviours[1] = new ClosestFriendlyPoiStrategy(this);

			sensorBehaviours[0].OnTargetChanged += TargetChangedFromSensorBehaviour;
			sensorBehaviours[1].OnTargetChanged += TargetChangedFromSensorBehaviour;
		}
		else
			Debug.LogError("sensor type has no sensor behaviour logic set up");
	}

	//sensor logic
	void UpdateSensorLogic()
	{
		SortTargetsInSensorRange();
		SortFriendliesInSensorRange();

		foreach (ISensorStrategy sensorBehaviour in sensorBehaviours)
		{
			if (sensorBehaviour == null) return;
			sensorBehaviour.EvaluateTargets();
		}
	}
	void SortTargetsInSensorRange()
	{
		if (sensorType == SensorType.poiSensor)
		{
			for (int i = targetsInRange.Count - 1; i >= 0; i--)
			{
				if (targetsInRange[i].obj == null || targetsInRange[i].poi.poiOwner == entityStats._Data.team)
				{
					targetsInRange.RemoveAt(i);
					friendliesInRange.Add(targetsInRange[i]);
				}
				else
					targetsInRange[i].UpdateTargetDistance(transform.position);
			}
		}
		else
		{
			for (int i = targetsInRange.Count - 1; i >= 0; i--)
			{
				if (targetsInRange[i].obj == null)
					targetsInRange.RemoveAt(i);
				else
					targetsInRange[i].UpdateTargetDistance(transform.position);
			}
		}

		targetsInRange.Sort((a, b) => a.TargetDistance.CompareTo(b.TargetDistance));
	}
	void SortFriendliesInSensorRange()
	{
		if (sensorType == SensorType.poiSensor)
		{
			for (int i = friendliesInRange.Count - 1; i >= 0; i--)
			{
				if (friendliesInRange[i].obj == null || friendliesInRange[i].poi.poiOwner != entityStats._Data.team)
				{
					friendliesInRange.RemoveAt(i);
					targetsInRange.Add(friendliesInRange[i]);
				}
				else
					friendliesInRange[i].UpdateTargetDistance(transform.position);
			}
		}
		else
		{
			for (int i = friendliesInRange.Count - 1; i >= 0; i--)
			{
				if (friendliesInRange[i].obj == null)
					friendliesInRange.RemoveAt(i);
				else
					friendliesInRange[i].UpdateTargetDistance(transform.position);
			}
		}

		friendliesInRange.Sort((a, b) => a.TargetDistance.CompareTo(b.TargetDistance));
	}

	//target trigger colliders
	void OnTriggerEnter(Collider other)
	{
		if (sensorType == SensorType.poiSensor)
		{
			if (other.GetComponent<PoIController>() == null) return;
			PoIController poi = other.GetComponent<PoIController>();

			if (poi.poiOwner == entityStats._Data.team)
				AddTargetToList(friendliesInRange, other.gameObject, TargetData.TargetType.poi);
			else
				AddTargetToList(targetsInRange, other.gameObject, TargetData.TargetType.poi);
		}
		else
		{
			if (other.GetComponent<EntityStats>() == null) return;
			EntityStats entity = other.GetComponent<EntityStats>();

			if (entity._Data.team == entityStats._Data.team)
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

			if (poi.poiOwner == entityStats._Data.team)
				RemoveTargetFromList(friendliesInRange, other.gameObject);
			else
				RemoveTargetFromList(targetsInRange, other.gameObject);
		}
		else
		{
			if (other.GetComponent<EntityStats>() == null) return;
			EntityStats entity = other.GetComponent<EntityStats>();

			if (entity._Data.team == entityStats._Data.team)
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

	//events
	void TargetChangedFromSensorBehaviour()
	{
		OnTargetChanged?.Invoke(sensorType);
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
		Gizmos.color = Color.green;
		Gizmos.DrawWireSphere(transform.position, detectionRadius);

		if (attackData == null || attackData.Count == 0) return;

		if (targetsInRange.Count != 0 && targetsInRange[0].TargetDistance <= attackData[0].attackMinRange)
			Gizmos.color = Color.red;
		else
			Gizmos.color = Color.blue;

		Gizmos.DrawWireSphere(transform.position, attackData[0].attackMaxRange);

		if (targetsInRange.Count != 0 && targetsInRange[0].TargetDistance <= attackData[1].attackMinRange)
			Gizmos.color = Color.red;
		else
			Gizmos.color = Color.blue;

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

		this.type = type;
		this.obj = obj;
		UpdateTargetDistance(position);
	}
	public TargetData(TargetType type)
	{
		this.type = type;
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
		TargetDistance = 0f;
	}
}