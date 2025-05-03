using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class EntitySensor : MonoBehaviour
{
	[SerializeField] SensorType sensorType;
	public enum SensorType
	{
		chase, flee, meleeAttack, rangedAttack
	}

	[SerializeField] float detectionRadius = 5f;
	[SerializeField] float timerInterval = 1f;

	EntityTypes.EntityTeam entityTeam;
	SphereCollider detectionRange;

	//used for ranged attack sensors
	float minAttackRange;
	float maxAttackRange;

	public event Action<GameObject> OnTargetChanged = delegate { };

	public List<EntityAggro> targetsInRange = new List<EntityAggro>();

	public Vector3 TargetPosition => target ? target.transform.position : Vector3.zero;
	public bool IsTargetInRange => TargetPosition != Vector3.zero;

	GameObject target;
	Vector3 lastKnownPosition;
	CountdownTimer timer;

	void Awake()
	{
		detectionRange = GetComponent<SphereCollider>();
		detectionRange.isTrigger = true;
		detectionRange.radius = detectionRadius;
	}

	public void UpdateSensorSettings(float minDetectionRadius, float maxDetectionRadius)
	{
		minAttackRange = minDetectionRadius;
		maxAttackRange = maxDetectionRadius;
		detectionRadius = maxDetectionRadius;
		detectionRange.radius = maxDetectionRadius;
	}

	void Start()
	{
		entityTeam = GetComponentInParent<EntityStats>().type.team;

		timer = new CountdownTimer(timerInterval);
		timer.OnTimerStop += () => {
			UpdateTargetsInRange();
			timer.Start();
		};
		timer.Start();
	}

	void Update()
	{
		timer.Tick(Time.deltaTime, false);
	}

	void UpdateTargetsInRange()
	{
		if (targetsInRange.Count > 0)
		{
			for (int i = 0; i < targetsInRange.Count; i++)
				targetsInRange[i].targetAggroDistance = GetAggoDistance(targetsInRange[i].target);

			if (sensorType == SensorType.rangedAttack)
				target = FindClosestTargetWithinMinMaxValues(minAttackRange, maxAttackRange);
			else
				target = FindClosestTarget();
		}
		else
			target = null;


		if (IsTargetInRange && (lastKnownPosition != TargetPosition || lastKnownPosition != Vector3.zero))
		{
			lastKnownPosition = TargetPosition;
			OnTargetChanged.Invoke(target);
		}
	}

	GameObject FindClosestTarget()
	{
		targetsInRange.Sort((a, b) => a.targetAggroDistance.CompareTo(b.targetAggroDistance));
		return targetsInRange[0].target;
	}

	GameObject FindClosestTargetWithinMinMaxValues(float min, float max)
	{
		List<EntityAggro> targetsWithinValues = new List<EntityAggro>();

		for (int i = 0; i < targetsInRange.Count; i++)
		{
			if (targetsInRange[i].targetAggroDistance >= min && targetsInRange[i].targetAggroDistance <= max)
				targetsWithinValues.Add(targetsInRange[i]);
		}

		targetsWithinValues.Sort((a, b) => a.targetAggroDistance.CompareTo(b.targetAggroDistance));
		return targetsWithinValues[0].target;
	}

	void OnTriggerEnter(Collider other)
	{
		EntityTypes.EntityTeam otherAgentTeam;

		if (other.GetComponent<EntityStats>() != null)
			otherAgentTeam = other.GetComponent<EntityStats>().type.team;
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
		EntityTypes.EntityTeam otherAgentTeam;

		if (other.GetComponent<EntityStats>() != null)
			otherAgentTeam = other.GetComponent<EntityStats>().type.team;
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

	void ClearDeadEntitiesFromTargetList(GameObject entity)
	{
		for (int i = targetsInRange.Count - 1; i >= 0; i--)
		{
			if (targetsInRange[i].target == entity || targetsInRange[i].target == null) //remove possible null refs
				targetsInRange.RemoveAt(i);
		}
	}

	float GetAggoDistance(GameObject target)
	{
		float aggroDistance = Vector3.Distance(transform.position, target.transform.position);
		return aggroDistance;
	}

	void OnDrawGizmos()
	{
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