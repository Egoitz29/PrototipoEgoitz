using UnityEngine;

public class UnitVisualFeedback : MonoBehaviour
{
    [SerializeField] private Renderer bodyRenderer;
    [SerializeField] private Renderer stateRenderer;

    private void Awake()
    {
        if (bodyRenderer == null)
            bodyRenderer = GetComponent<Renderer>();

        ApplyTeamColor();
    }

    public void ApplyTeamColor()
    {
        UnitIdentity identity = GetComponent<UnitIdentity>();

        if (identity == null || bodyRenderer == null)
            return;

        if (identity.team == UnitTeam.Player)
            bodyRenderer.material.color = Color.blue;
        else
            bodyRenderer.material.color = Color.red;
    }

    public void SetColor(Color color)
    {
        if (stateRenderer != null)
            stateRenderer.material.color = color;
    }
}