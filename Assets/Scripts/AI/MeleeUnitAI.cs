using UnityEngine;
using UnityEngine.AI;


public class MeleeUnitAI : MonoBehaviour
{
    public float separationRadius = 1.2f;
    public float separationStrength = 2f;
    private bool battleInitialized;
    public UnitTeam team;
    public Transform enemySpawn;
    public float detectionRange = 4f;
    public float attackRange = 1.5f;
    public float attackCooldown = 1f;
    private LineRenderer pathLine;
    private NavMeshAgent agent;
    private UnitVisualFeedback visualFeedback;

    private Health lockedTarget;
    private float nextAttackTime;

    private enum State
    {
        Advancing,
        Chasing,
        Fighting
    }

    private State currentState;

    void Start()
    {
        pathLine = GetComponent<LineRenderer>();
        agent = GetComponent<NavMeshAgent>();
        visualFeedback = GetComponent<UnitVisualFeedback>();

        currentState = State.Advancing;
        agent.isStopped = true;
    }

    void Update()
    {
        if (BattleManager.Instance == null || !BattleManager.Instance.BattleStarted)
            return;

        if (!battleInitialized)
        {
            battleInitialized = true;
            agent.isStopped = false;

            if (enemySpawn != null)
                agent.SetDestination(enemySpawn.position);
        }
        //ApplySeparation();
        UpdateStateColor();
        DrawCurrentPath();
        switch (currentState)
        {
            case State.Advancing:
                AdvanceToEnemySpawn();
                SearchEnemy();
                break;

            case State.Chasing:
                ChaseTarget();
                break;

            case State.Fighting:
                AttackTarget();
                break;
        }
    }

    void SearchEnemy()
    {
        Collider[] hits =
            Physics.OverlapSphere(
                transform.position,
                detectionRange);

        Health bestTarget = null;
        float bestDistance = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            if (hit.gameObject == gameObject)
                continue;

            UnitIdentity identity =
                hit.GetComponent<UnitIdentity>();

            if (identity == null)
                continue;

            if (identity.team == team)
                continue;

            Health hp =
                hit.GetComponent<Health>();

            if (hp == null)
                continue;

            float distance =
                Vector3.Distance(
                    transform.position,
                    hit.transform.position);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestTarget = hp;
            }
        }

        if (bestTarget != null)
        {
            lockedTarget = bestTarget;
            currentState = State.Chasing;
        }
    }

    void ChaseTarget()
    {
        if (lockedTarget == null)
        {
            currentState = State.Advancing;
            agent.SetDestination(enemySpawn.position);
            return;
        }

        agent.SetDestination(
            lockedTarget.transform.position);

        float distance =
            Vector3.Distance(
                transform.position,
                lockedTarget.transform.position);

        if (distance <= attackRange)
        {
            agent.isStopped = true;
            currentState = State.Fighting;
        }
    }

    void AttackTarget()
    {
        if (lockedTarget == null)
        {
            agent.isStopped = false;
            currentState = State.Advancing;
            agent.SetDestination(enemySpawn.position);
            return;
        }
        Vector3 lookPosition = lockedTarget.transform.position;
        lookPosition.y = transform.position.y;
        transform.LookAt(lookPosition);

        float distance =
            Vector3.Distance(
                transform.position,
                lockedTarget.transform.position);

        if (distance > attackRange)
        {
            agent.isStopped = false;
            currentState = State.Chasing;
            return;
        }
        if (Time.time >= nextAttackTime)
        {
            lockedTarget.TakeDamage(25f);

            nextAttackTime =
                Time.time + attackCooldown;
        }
    }

    void ApplySeparation()
    {
        Collider[] hits =
            Physics.OverlapSphere(
                transform.position,
                separationRadius);

        Vector3 pushDir = Vector3.zero;

        foreach (Collider hit in hits)
        {
            if (hit.gameObject == gameObject)
                continue;

            MeleeUnitAI ally =
                hit.GetComponent<MeleeUnitAI>();

            if (ally == null)
                continue;

            if (ally.team != team)
                continue;

            Vector3 away =
                transform.position -
                ally.transform.position;

            pushDir += away.normalized;
        }

        if (pushDir != Vector3.zero)
        {
            agent.Move(
                pushDir.normalized *
                separationStrength *
                Time.deltaTime);
        }
    }

    void UpdateStateColor()
    {
        if (visualFeedback == null)
            return;

        switch (currentState)
        {
            case State.Advancing:
                visualFeedback.SetColor(Color.white);
                break;

            case State.Chasing:
                visualFeedback.SetColor(Color.yellow);
                break;

            case State.Fighting:
                visualFeedback.SetColor(new Color(1f, 0.5f, 0f));
                break;
        }
    }

    void DrawCurrentPath()
    {
        if (pathLine == null || agent == null || !agent.hasPath)
            return;

        NavMeshPath path = agent.path;

        if (path.corners.Length < 2)
        {
            pathLine.positionCount = 0;
            return;
        }

        pathLine.positionCount = path.corners.Length;

        for (int i = 0; i < path.corners.Length; i++)
        {
            Vector3 point = path.corners[i];
            point.y += 0.15f;
            pathLine.SetPosition(i, point);
        }
    }

    void AdvanceToEnemySpawn()
    {
        if (enemySpawn == null)
            return;

        if (agent.isStopped)
            agent.isStopped = false;

        agent.SetDestination(enemySpawn.position);
    }

    private void OnDrawGizmos()
    {
        // Radio de detección de enemigos
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Radio de ataque cuerpo a cuerpo
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Radio de separación con aliados
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, separationRadius);
    }
}   