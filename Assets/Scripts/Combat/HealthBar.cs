using UnityEngine;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Transform fillBar;
    [SerializeField] private Health health;

    private Vector3 originalScale;

    private void Awake()
    {
        if (health == null)
            health = GetComponentInParent<Health>();

        if (fillBar != null)
            originalScale = fillBar.localScale;
    }

    private void Update()
    {
        if (health == null || fillBar == null)
            return;

        float percent = health.GetHealthPercent();

        fillBar.localScale = new Vector3(
            originalScale.x * percent,
            originalScale.y,
            originalScale.z
        );

        transform.rotation = Camera.main.transform.rotation;
    }
}