using UnityEngine;
using UnityEngine.AI;

public class PreparationUnitSelector : MonoBehaviour
{
    private Camera mainCamera;
    private GameObject selectedUnit;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (BattleManager.Instance != null && BattleManager.Instance.BattleStarted)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            TrySelectUnit();
        }

        if (Input.GetMouseButtonDown(1))
        {
            TryMoveSelectedUnit();
        }
    }

    void TrySelectUnit()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            UnitIdentity identity = hit.collider.GetComponent<UnitIdentity>();

            if (identity != null && identity.team == UnitTeam.Player)
            {
                selectedUnit = hit.collider.gameObject;
                Debug.Log("Unidad seleccionada: " + selectedUnit.name);
            }
        }
    }

    void TryMoveSelectedUnit()
    {
        if (selectedUnit == null)
            return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            NavMeshAgent agent = selectedUnit.GetComponent<NavMeshAgent>();

            if (agent != null)
            {
                agent.Warp(hit.point);
                Debug.Log("Unidad recolocada: " + selectedUnit.name);
            }
        }
    }
}