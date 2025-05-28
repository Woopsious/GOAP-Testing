using TMPro;
using UnityEngine;

public class TeamInfoUi : MonoBehaviour
{
	public GameObject teamInfoBackground;

	public EntityData.EntityTeam teamToTrack;

	public TMP_Text teamInfo;

	public TMP_Text ownedPois;
	public TMP_Text resources;

	public TMP_Text workers;
	public TMP_Text dualists;
	public TMP_Text melee;
	public TMP_Text ranged;

	void Awake()
	{
		if (teamToTrack == EntityData.EntityTeam.redTeam)
			teamInfo.text = "Red Team Info";
		else if (teamToTrack == EntityData.EntityTeam.greenTeam)
			teamInfo.text = "Green Team Info";
	}

	void OnEnable()
	{
		GameManager.UpdateUiPoiCounters += UpdatePoiCounter;
		GameManager.UpdateUiResourceCounters += UpdateResourceCounter;
		EntityPopManager.UpdateUiPopDataEvent += UpdatePopCounters;
	}
	void OnDisable()
	{
		GameManager.UpdateUiPoiCounters -= UpdatePoiCounter;
		GameManager.UpdateUiResourceCounters -= UpdateResourceCounter;
		EntityPopManager.UpdateUiPopDataEvent -= UpdatePopCounters;
	}

	void UpdatePoiCounter()
	{
		if (teamToTrack == EntityData.EntityTeam.redTeam)
			ownedPois.text = "Owned Pois: " + GameManager.instance.RedTeamCapturedPois;
		else if (teamToTrack == EntityData.EntityTeam.greenTeam)
			ownedPois.text = "Owned Pois: " + GameManager.instance.GreenTeamCapturedPois;
	}

	void UpdateResourceCounter(EntityData.EntityTeam team, int resourceAmount)
	{
		if (teamToTrack == team)
			resources.text = "Resources: " + resourceAmount;
	}

	void UpdatePopCounters(EntityData.EntityTeam team)
	{
		if (team == EntityData.EntityTeam.redTeam)
		{
			workers.text = "Workers: " + EntityPopManager.instance.redWorkerPopData.CurrentPop();
			dualists.text = "Dualists: " + EntityPopManager.instance.redDualistPopData.CurrentPop();
			melee.text = "Melee: " + EntityPopManager.instance.redMeleePopData.CurrentPop();
			ranged.text = "Ranged: " + EntityPopManager.instance.redRangedPopData.CurrentPop();
		}
		else if (team == EntityData.EntityTeam.greenTeam)
		{
			workers.text = "Workers: " + EntityPopManager.instance.greenWorkerPopData.CurrentPop();
			dualists.text = "Dualists: " + EntityPopManager.instance.greenDualistPopData.CurrentPop();
			melee.text = "Melee: " + EntityPopManager.instance.greenMeleePopData.CurrentPop();
			ranged.text = "Ranged: " + EntityPopManager.instance.greenRangedPopData.CurrentPop();
		}
	}
}
