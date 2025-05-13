using UnityEditor;
using UnityEngine;

public class EntityStats : MonoBehaviour
{
	PlayerMovement player;
	[HideInInspector] public EntityBrain entityBrain;

	[Header("Stats")]
	public EntityData _Data;
	public float currentHealth;

	CountdownTimer statsTimer;

	public Material redTeamMaterial;
	public Material greenTeamMaterial;

	private void Awake()
	{
		if (_Data == null)
			Debug.LogError("Entity Data not set for gameobject: " + gameObject.name);

		if (_Data.team == EntityData.EntityTeam.redTeam)
			GetComponent<MeshRenderer>().sharedMaterial = redTeamMaterial;
		else if (_Data.team == EntityData.EntityTeam.greenTeam)
			GetComponent<MeshRenderer>().sharedMaterial = greenTeamMaterial;

		player = GetComponent<PlayerMovement>();
		entityBrain = GetComponent<EntityBrain>();
	}

	private void Start()
	{
		Initilize();
	}

	private void Update()
	{
		if (player != null) return;
		statsTimer.Tick(Time.deltaTime, false);
	}
	private void Initilize()
	{
		name = _Data.Name;
		currentHealth = _Data.maxHealth;

		if (player != null) return;

		statsTimer = new CountdownTimer(5f);
		statsTimer.OnTimerStop += () =>
		{
			UpdateStats();
			statsTimer.Start();
		};
		statsTimer.Start();
	}
	private void UpdateStats()
	{
		if (player != null) return;
		if (InRangeOf(entityBrain.foodShack.position, 3f))
			currentHealth += 50;
		else
		{
			if (currentHealth <= _Data.maxHealth * 0.8f) return;
			currentHealth -= 5;
		}
		currentHealth = Mathf.Clamp(currentHealth, 0, _Data.maxHealth);
	}
	private bool InRangeOf(Vector3 pos, float range) => Vector3.Distance(transform.position, pos) < range;

	public void RecieveDamage(float damageRecieved)
	{
		currentHealth -= damageRecieved;
		OnDeath();
	}

	private void OnDeath()
	{
		if (currentHealth > 0) return;

		GameManager.OnEntityDeath(gameObject);
		Destroy(gameObject);
	}
}
