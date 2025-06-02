using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
	public static GameManager instance;

	/// <summary> IDEAS TO TRY AND IMPLEMENT
	/// POI RELATED IDEAS:
	/// resoure nodes for worker entities to 'mine' extra resource from.
	/// different resource types to use on better entity types.
	/// 
	/// ENTITY RELATED IDEAS:
	/// split entity brain strategies up further into melee, ranged etc... for more unique behaviour
	/// allow entities to patrol around/defend pois them.
	/// commander type that can organise regular combat type entities (ATM worry about adding simpler things)
	/// 
	/// AI DIRECTOR IDEAS:
	/// a higher level system that will direct entities to achieve certian goals, either for a specific or multiple teams.
	/// tracks references to all entities and pois.
	/// can direct/force entities to do things via a belief that checks hasCommand bool
	///		an action called EntityCommandStrategy that takes in an interface as an argument (similar concept to IEntityInteractStrategies)
	///		a goal that has its priority adjusted based on the type of command issued (move command priority shouldnt excede basic attack priority
	///		but should excede its wander/move/idle behaviour)
	///	will take over parts of what GM script is currently doing like tracking and storing refs of the following:
	///	to all pois and entity refs + counters for poi ownership + team resource counters.
	/// </summary>

	void Awake()
	{
		instance = this;
	}
}
