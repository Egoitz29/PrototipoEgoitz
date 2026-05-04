using UnityEngine;
using UnityEngine.AI;
using TMPro;

public class TroopPlacementManager : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject playerMeleePrefab;
    public GameObject playerArcherPrefab;

    [Header("Placement")]
    public Collider playerDeploymentZone;

    [Header("Spawns")]
    public Transform enemySpawn;

    [Header("Resources")]
    public int resources = 10;
    public int meleeCost = 2;
    public int archerCost = 3;
    public TMP_Text resourcesText;

    private GameObject selectedPrefab;
    private Camera mainCamera;
    private bool deleteMode;

    private void Start()
    {
        mainCamera = Camera.main;
        UpdateResourcesUI();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            ClearSelection();

        if (BattleManager.Instance != null &&
            BattleManager.Instance.BattleStarted)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            if (deleteMode)
                TryDeleteTroop();
            else if (selectedPrefab != null)
                TryPlaceTroop();
        }
    }

    public void SelectMelee()
    {
        selectedPrefab = playerMeleePrefab;
        deleteMode = false;
        Debug.Log("Tropa seleccionada: Melee");
    }

    public void SelectArcher()
    {
        selectedPrefab = playerArcherPrefab;
        deleteMode = false;
        Debug.Log("Tropa seleccionada: Archer");
    }

    public void SelectDeleteMode()
    {
        selectedPrefab = null;
        deleteMode = true;
        Debug.Log("Modo eliminar activado");
    }

    public void ClearSelection()
    {
        selectedPrefab = null;
        deleteMode = false;
        Debug.Log("Selección cancelada");
    }

    private void TryPlaceTroop()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, 200f))
            return;

        if (playerDeploymentZone != null &&
            !playerDeploymentZone.bounds.Contains(hit.point))
        {
            Debug.LogWarning("No puedes colocar tropas fuera de la zona de despliegue");
            return;
        }

        if (!NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, 2f, NavMesh.AllAreas))
        {
            Debug.LogWarning("No hay NavMesh válido en esa posición");
            return;
        }

        int cost = GetSelectedTroopCost();

        if (resources < cost)
        {
            Debug.LogWarning("No tienes recursos suficientes");
            return;
        }

        GameObject newTroop = Instantiate(
            selectedPrefab,
            navHit.position,
            Quaternion.identity);

        MeleeUnitAI meleeAI = newTroop.GetComponent<MeleeUnitAI>();

        if (meleeAI != null)
            meleeAI.enemySpawn = enemySpawn;

        resources -= cost;
        UpdateResourcesUI();
    }

    private void TryDeleteTroop()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, 200f))
            return;

        UnitSensor unit = hit.collider.GetComponent<UnitSensor>();

        if (unit == null)
            return;

        if (unit.GetTeam() != UnitTeam.Player)
            return;

        int refund = GetRefundForTroop(unit);

        resources += refund;
        UpdateResourcesUI();

        Destroy(unit.gameObject);

        Debug.Log("Tropa eliminada. Recursos devueltos: " + refund);
    }

    public void ClearPlacedPlayerTroops()
    {
        if (BattleManager.Instance != null &&
            BattleManager.Instance.BattleStarted)
            return;

        UnitSensor[] units =
            FindObjectsByType<UnitSensor>(FindObjectsSortMode.None);

        foreach (UnitSensor unit in units)
        {
            if (unit.GetTeam() == UnitTeam.Player)
            {
                resources += GetRefundForTroop(unit);
                Destroy(unit.gameObject);
            }
        }

        UpdateResourcesUI();
        Debug.Log("Tropas del jugador eliminadas y recursos devueltos");
    }

    private int GetSelectedTroopCost()
    {
        if (selectedPrefab == playerMeleePrefab)
            return meleeCost;

        if (selectedPrefab == playerArcherPrefab)
            return archerCost;

        return 0;
    }

    private int GetRefundForTroop(UnitSensor unit)
    {
        if (unit.GetUnitType() == UnitType.Melee)
            return meleeCost;

        if (unit.GetUnitType() == UnitType.Archer)
            return archerCost;

        return 0;
    }

    private void UpdateResourcesUI()
    {
        if (resourcesText != null)
            resourcesText.text = "Recursos: " + resources;
    }
}