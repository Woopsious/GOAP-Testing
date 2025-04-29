using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EntitySensor : MonoBehaviour
{
	[SerializeField] SensorType sensorType;
	enum SensorType
	{
		chase, flee, attack
	}

	[SerializeField] float detectionRadius = 5f;
	[SerializeField] float timerInterval = 1f;

	EntityAgent agent;
	EntityTypes.EntityTeam entityTeam;
	SphereCollider detectionRange;

	public event Action OnTargetChanged = delegate { };

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

	public void UpdateSensorSettings(float detectionRadius)
	{
		this.detectionRadius = detectionRadius;
		detectionRange.radius = detectionRadius;
	}

	void Start()
	{
		agent = GetComponentInParent<EntityAgent>();
		entityTeam = agent.entityType.team;

		timer = new CountdownTimer(timerInterval);
		timer.OnTimerStop += () => {
			UpdateCurrentTarget();
			timer.Start();
		};
		timer.Start();
	}

	void Update()
	{
		timer.Tick(Time.deltaTime, false);
	}

	void UpdateCurrentTarget()
	{
		if (targetsInRange.Count > 0)
		{
			for (int i = 0; i < targetsInRange.Count; i++)
				targetsInRange[i].targetAggroDistance = GetAggoDistance(targetsInRange[i].target);

			targetsInRange.Sort((a, b) => a.targetAggroDistance.CompareTo(b.targetAggroDistance));

			//if (target == targetsInRange[0].target) //same target so skip force recalc of planw
				//return;

			target = targetsInRange[0].target;
			agent.target = targetsInRange[0].target;
		}
		else
		{
			target = null;
			agent.target = null;
		}


		if (IsTargetInRange && (lastKnownPosition != TargetPosition || lastKnownPosition != Vector3.zero))
		{
			lastKnownPosition = TargetPosition;
			OnTargetChanged.Invoke();
		}
	}

	void OnTriggerEnter(Collider other)
	{
		EntityTypes.EntityTeam otherAgentTeam;

		if (other.GetComponent<EntityAgent>() != null)
			otherAgentTeam = other.GetComponent<EntityAgent>().entityType.team;
		else if (other.GetComponent<PlayerMovement>() != null)
			otherAgentTeam = EntityTypes.EntityTeam.playerTeam;
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

		if (other.GetComponent<EntityAgent>() != null)
			otherAgentTeam = other.GetComponent<EntityAgent>().entityType.team;
		else if (other.GetComponent<PlayerMovement>() != null)
			otherAgentTeam = EntityTypes.EntityTeam.playerTeam;
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