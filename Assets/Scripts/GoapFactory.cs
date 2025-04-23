using UnityEngine;

public class GoapFactory : MonoBehaviour
{
	void Awake()
	{
		//ServiceLocator.Global.Register(this);
	}

	public GoapFactory ProvideFactory() => this;

	public IGoapPlanner CreatePlanner()
	{
		return new GoapPlanner();
	}
}
