using UnityEngine;
using UnityEngine.AI;

public class TroopPlacementManager : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject playerMeleePrefab;
    public GameObject playerArcherPrefab;

    [Header("Placement")]
    public Collider playerDeploymentZone;

    [Header("Spawns")]
    public Transform enemySpawn;

    private GameObject selectedPrefab;
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            ClearSelection();

        if (BattleManager.Instance != null &&
            BattleManager.Instance.BattleStarted)
            return;

        if (selectedPrefab == null)
            return;

        if (Input.GetMouseButtonDown(0))
            TryPlaceTroop();
    }

    public void SelectMelee()
    {
        selectedPrefab = playerMeleePrefab;
        Debug.Log("Tropa seleccionada: Melee");
    }

    public void SelectArcher()
    {
        selectedPrefab = playerArcherPrefab;
        Debug.Log("Tropa seleccionada: Archer");
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

        GameObject newTroop = Instantiate(
            selectedPrefab,
            navHit.position,
            Quaternion.identity);

        MeleeUnitAI meleeAI = newTroop.GetComponent<MeleeUnitAI>();

        if (meleeAI != null)
            meleeAI.enemySpawn = enemySpawn;
    }

    public void ClearSelection()
    {
        selectedPrefab = null;
        Debug.Log("Selección cancelada");
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
                Destroy(unit.gameObject);
            }
        }

        Debug.Log("Tropas del jugador eliminadas");
    }
}