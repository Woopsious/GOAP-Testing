using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class EntitySensor : MonoBehaviour
{
	[Header("Sensor Info")]
	[SerializeField] SensorType sensorType;
	public enum SensorType
	{
		chase, flee, attackSensorOne, attackSensorTwo
	}

	[SerializeField] float detectionRadius = 5f;
	[SerializeField] float timerInterval = 1f;

	EntityBrain entityBrain;
	EntityData.EntityTeam entityTeam;
	SphereCollider detectionRange;

	public event Action<GameObject, SensorType> OnTargetChanged = delegate { };

	private List<EntityAggro> targetsInRange = new List<EntityAggro>();

	public Vector3 TargetPosition => target ? target.transform.position : Vector3.zero;
	public Vector3 TargetBackupPosition => targetBackup ? targetBackup.transform.position : Vector3.zero;

	public bool IsTargetInRange => TargetPosition != Vector3.zero;
	public bool HasBackUpTarget => targetBackup != null;

	[Header("Attack Sensor Info")]
	[SerializeField] EntityAttackData attackData;

	public GameObject target;
	public GameObject targetBackup;
	Vector3 lastKnownPosition;
	CountdownTimer timer;

	void Awake()
	{
		entityBrain = GetComponentInParent<EntityBrain>();
		EntityStats stats = GetComponentInParent<EntityStats>();
		entityTeam = stats._Data.team;

		detectionRange = GetComponent<SphereCollider>();
		detectionRange.isTrigger = true;
		detectionRange.radius = detectionRadius;
	}

	public void UpdateSensorSettings(float detectionRadius)
	{
		this.detectionRadius = detectionRadius;
		detectionRange.radius = detectionRadius;
	}
	public void UpdateSensorSettings(EntityAttackData attackData)
	{
		this.attackData = attackData;
		UpdateSensorSettings(attackData.attackMaxRange);
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

	void UpdateSensorLogic()
	{
		SortTargetsInSensorRange();
		UpdateSensorTargets();
	}

	void SortTargetsInSensorRange()
	{
		for (int i = 0; i < targetsInRange.Count; i++)
			targetsInRange[i].targetAggroDistance = GetAggoDistance(targetsInRange[i].target);

		targetsInRange.Sort((a, b) => a.targetAggroDistance.CompareTo(b.targetAggroDistance));
	}
	void UpdateSensorTargets()
	{
		if (sensorType == SensorType.attackSensorOne || sensorType == SensorType.attackSensorTwo)
		{
			target = FindClosestTargetWithinValues(attackData.attackMinRange, attackData.attackMaxRange);

			if (target == null)
			{
				OnTargetChanged.Invoke(null, sensorType);
				targetBackup = FindClosestTarget();

			}
			else
			{
				OnTargetChanged.Invoke(target, sensorType);
				targetBackup = null;
			}
		}
		else
		{
			target = FindClosestTarget();

			//attack sensors shouldnt need to worry about tracking last known pos as chase/flee sensors handle that
			if (IsTargetInRange && (lastKnownPosition != TargetPosition || lastKnownPosition != Vector3.zero))
			{
				OnTargetChanged.Invoke(target, sensorType);
				lastKnownPosition = TargetPosition;
			}
		}
	}

	GameObject FindClosestTarget()
	{
		if (targetsInRange.Count <= 0)
			return null;
		else
			return targetsInRange[0].target;
	}
	GameObject FindClosestTargetWithinValues(float min, float max)
	{
		for (int i = 0; i < targetsInRange.Count; i++)
		{
			if (targetsInRange[i].targetAggroDistance >= min && targetsInRange[i].targetAggroDistance <= max)
				return targetsInRange[i].target;
		}
		return null;
	}

	void OnTriggerEnter(Collider other)
	{
		EntityData.EntityTeam otherAgentTeam;

		if (other.GetComponent<EntityStats>() != null)
			otherAgentTeam = other.GetComponent<EntityStats>()._Data.team;
		else
			return;

		if (entityTeam != otherAgentTeam)
		{
			if (targetsInRange.Count == 0) //list empty no need to check
			{
				targetsInRange.Add(new EntityAggro(other.gameObject, GetAggoDistance(other.gameObject)));
				return;
			}

			for (int i = 0; i < targetsInRange.Count; i++)
			{
				if (targetsInRange[i].target == other.gameObject) 
					continue;
				else
					targetsInRange.Add(new EntityAggro(other.gameObject, GetAggoDistance(other.gameObject)));
			}
		}
	}
	void OnTriggerExit(Collider other)
	{
		EntityData.EntityTeam otherAgentTeam;

		if (other.GetComponent<EntityStats>() != null)
			otherAgentTeam = other.GetComponent<EntityStats>()._Data.team;
		else
			return;

		if (entityTeam != otherAgentTeam)
		{
			for (int i = targetsInRange.Count - 1; i >= 0; i--)
			{
				if (targetsInRange[i].target == other.gameObject)
					targetsInRange.RemoveAt(i);
			}
		}
	}

	void OnEnable()
	{
		GameManager.OnEntityDeathEvent += ClearDeadEntitiesFromTargetList;
	}
	void OnDisable()
	{
		GameManager.OnEntityDeathEvent -= ClearDeadEntitiesFromTargetList;
	}

	float GetAggoDistance(GameObject target)
	{
		float aggroDistance = Vector3.Distance(transform.position, target.transform.position);
		return aggroDistance;
	}

	void ClearDeadEntitiesFromTargetList(GameObject entity)
	{
		for (int i = targetsInRange.Count - 1; i >= 0; i--)
		{
			if (targetsInRange[i].target == entity || targetsInRange[i].target == null) //remove possible null refs
				targetsInRange.RemoveAt(i);
		}
	}

	void OnDrawGizmos()
	{
		if (sensorType == SensorType.attackSensorOne ||  sensorType == SensorType.attackSensorTwo)
			Gizmos.color = target ? Color.blue : Color.green;
		else
			Gizmos.color = IsTargetInRange ? Color.red : Color.green;

		Gizmos.DrawWireSphere(transform.position, detectionRadius);
	}
}

public class EntityAggro
{
	public GameObject target;
	public float targetAggroDistance;

	public EntityAggro(GameObject target, float targetAggroDistance)
	{
		this.target = target;
		this.targetAggroDistance = targetAggroDistance;
	}
}