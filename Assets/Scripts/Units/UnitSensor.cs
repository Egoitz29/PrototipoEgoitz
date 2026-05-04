using UnityEngine;

public enum UnitType
{
    Melee,
    Archer,
    Cavalry,
    Siege
}

public class UnitSensor : MonoBehaviour
{
    public UnitTeam team;
    public UnitType unitType;

    private Health health;

    void Awake()
    {
        health =
            GetComponent<Health>();
    }

    public Vector3 GetPosition()
    {
        return transform.position;
    }

    public bool IsAlive()
    {
        if (health == null)
            return false;

        return gameObject.activeInHierarchy;
    }

    public UnitTeam GetTeam()
    {
        return team;
    }

    public UnitType GetUnitType()
    {
        return unitType;
    }
}