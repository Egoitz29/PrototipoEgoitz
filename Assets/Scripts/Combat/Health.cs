using UnityEngine;

public class Health : MonoBehaviour
{
    public float maxHealth = 100f;
    public static int playerKills;
    public static int enemyKills;

    float currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;

        Debug.Log(
            name +
            " vida: " +
            currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        UnitIdentity unit =
            GetComponent<UnitIdentity>();

        if (unit != null)
        {
            if (unit.team == UnitTeam.Enemy)
                playerKills++;

            if (unit.team == UnitTeam.Player)
                enemyKills++;
        }

        Debug.Log(name + " ha muerto");

        Destroy(gameObject);
    }

    public float GetHealthPercent()
    {
        return currentHealth / maxHealth;
    }
}