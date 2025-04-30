using UnityEngine;

public class EntityStats : MonoBehaviour
{
	EntityBrain entityBrain;

	[Header("Stats")]
	public EntityTypes type;
	public float currentHealth;

	CountdownTimer statsTimer;

	private void Awake()
	{
		entityBrain = GetComponent<EntityBrain>();
	}

	private void Start()
	{
		Initilize();
	}

	private void Update()
	{
		statsTimer.Tick(Time.deltaTime, false);
	}
	private void Initilize()
	{
		name = type.Name;
		currentHealth = type.maxHealth;

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
		if (InRangeOf(entityBrain.foodShack.position, 3f))
			currentHealth += 50;
		else
		{
			if (currentHealth <= 80) return;
			currentHealth -= 5;
		}
		currentHealth = Mathf.Clamp(currentHealth, 0, 100);
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
