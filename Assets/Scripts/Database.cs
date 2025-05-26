using UnityEngine;

public class Database : MonoBehaviour
{
	public static Database _Database;

	public EntityData[] entityData;

	private void Awake()
	{
		_Database = this;
	}

	public static EntityData GetEntity(EntityData.EntityDataType dataType)
	{
        foreach (EntityData data in _Database.entityData)
        {
            if (data.type == dataType)
				return data;
        }

		Debug.LogError("no matching entity data type");
		return null;
	}
}
