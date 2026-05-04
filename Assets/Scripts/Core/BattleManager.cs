using UnityEngine;
using TMPro;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    public bool BattleStarted { get; private set; }
    public bool BattleFinished { get; private set; }

    [Header("UI")]
    public TMP_Text resultText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        if (!BattleStarted || BattleFinished)
            return;

        CheckBattleResult();
    }

    public void StartBattle()
    {
        BattleStarted = true;
        BattleFinished = false;

        if (resultText != null)
            resultText.text = "";
        RTSCameraController cam =
FindFirstObjectByType<RTSCameraController>();

        if (cam != null)
        {
            cam.FrameAllUnits();
        }
        Debug.Log("Battle started");
    }

    private void CheckBattleResult()
    {
        UnitIdentity[] units =
            FindObjectsByType<UnitIdentity>(FindObjectsSortMode.None);

        int playerAlive = 0;
        int enemyAlive = 0;

        foreach (UnitIdentity unit in units)
        {
            Health hp = unit.GetComponent<Health>();

            if (hp == null)
                continue;

            if (unit.team == UnitTeam.Player)
                playerAlive++;

            if (unit.team == UnitTeam.Enemy)
                enemyAlive++;
        }

        if (playerAlive == 0)
            FinishBattle("DERROTA");

        else if (enemyAlive == 0)
            FinishBattle("VICTORIA");
    }

    private void FinishBattle(string result)
    {
        BattleFinished = true;
        BattleStarted = false;

        Debug.Log(result);

        if (resultText != null)
            resultText.text =
result +
"\nKills jugador: " + Health.playerKills +
"\nKills enemigo: " + Health.enemyKills;
    }
}