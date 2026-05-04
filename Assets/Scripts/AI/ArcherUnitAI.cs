using UnityEngine;
using UnityEngine.AI;

public class ArcherUnitAI : MonoBehaviour
{
    private UnitTeam team;

    [Header("Detection")]
    public float baseDetectionRange = 12f;
    public float maxDetectionRange = 35f;
    public float expandRate = 6f;

    [Header("Tactical Points")]
    public float tacticalPointSearchRadius = 20f;

    [Header("Attack")]
    public float attackRange = 10f;
    public float damage = 15f;
    public float attackCooldown = 1.5f;

    [Header("Line of Sight (Obstacles)")]
    public LayerMask obstacleMask;
    public float heightOffset = 1.5f;

    [Header("Debug")]
    public bool drawTargetLine = true;

    private NavMeshAgent agent;
    private Health currentTarget;
    private LineRenderer shotLine;

    [Header("Reactivity")]
    public float reScanDelay = 1f;
    private float outOfRangeTimer;

    private float currentDetectionRange;
    private float nextAttackTime;

    private Vector3 selectedTacticalPoint;
    private bool hasSelectedTacticalPoint;

    // NUEVO: Guardamos el punto táctico que hemos "reservado"
    private ArcherTacticalPoint reservedPoint;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        shotLine = GetComponent<LineRenderer>();
        UnitSensor sensor = GetComponent<UnitSensor>();

        if (sensor != null) team = sensor.GetTeam();
        if (agent != null) agent.isStopped = true;
        if (shotLine != null) shotLine.enabled = false;

        currentDetectionRange = baseDetectionRange;
    }

    private void Update()
    {
        if (BattleManager.Instance == null || !BattleManager.Instance.BattleStarted)
            return;

        HandleTargetLogic();
        TryAttack();
    }

    private void HandleTargetLogic()
    {
        if (currentTarget == null)
        {
            DetectEnemy();
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);

        if (distanceToTarget > attackRange)
        {
            outOfRangeTimer += Time.deltaTime;

            if (outOfRangeTimer >= reScanDelay)
            {
                ClearTarget(); // Esto ahora también libera la torre automáticamente
                outOfRangeTimer = 0f;
                DetectEnemy();
            }
        }
        else
        {
            outOfRangeTimer = 0f;

            if (!hasSelectedTacticalPoint && agent != null)
            {
                if (HasLineOfSight(transform.position, currentTarget.transform.position))
                {
                    agent.isStopped = true;
                }
                else
                {
                    agent.isStopped = false;
                    agent.SetDestination(currentTarget.transform.position);
                }
            }
        }
    }

    private void DetectEnemy()
    {
        if (currentTarget != null)
        {
            if (!currentTarget.gameObject.activeInHierarchy)
            {
                ClearTarget();
                return;
            }

            float distanceToCurrent = Vector3.Distance(transform.position, currentTarget.transform.position);
            if (distanceToCurrent > maxDetectionRange)
            {
                ClearTarget();
                return;
            }

            if (HasReachedTacticalPoint())
            {
                if (distanceToCurrent > attackRange || !HasLineOfSight(transform.position, currentTarget.transform.position))
                {
                    Health closerEnemy = FindEnemyInAttackRange();
                    if (closerEnemy != null)
                    {
                        currentTarget = closerEnemy;
                    }
                }
                return;
            }

            FindHighestTacticalPoint();
            return;
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, currentDetectionRange);
        Health bestTarget = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            if (hit.gameObject == gameObject) continue;

            UnitSensor unit = hit.GetComponent<UnitSensor>();
            if (unit == null || unit.GetTeam() == team) continue;

            Health hp = hit.GetComponent<Health>();
            if (hp == null) continue;

            float distance = Vector3.Distance(transform.position, unit.GetPosition());
            if (distance < closestDistance)
            {
                closestDistance = distance;
                bestTarget = hp;
            }
        }

        if (bestTarget == null)
        {
            currentDetectionRange += expandRate * Time.deltaTime;
            currentDetectionRange = Mathf.Clamp(currentDetectionRange, baseDetectionRange, maxDetectionRange);
        }
        else
        {
            currentTarget = bestTarget;
            currentDetectionRange = baseDetectionRange;
            FindHighestTacticalPoint();
        }
    }

    // NUEVO: Función auxiliar para soltar la torre que teníamos pillada
    private void ReleaseReservedPoint()
    {
        if (reservedPoint != null && reservedPoint.currentOccupant == this)
        {
            reservedPoint.currentOccupant = null;
        }
        reservedPoint = null;
    }

    private void ClearTarget()
    {
        currentTarget = null;
        hasSelectedTacticalPoint = false;
        currentDetectionRange = baseDetectionRange;

        // Si nos olvidamos del target, soltamos nuestra mesa en el restaurante
        ReleaseReservedPoint();
    }

    // NUEVO: Si el arquero muere o se desactiva, ¡tiene que dejar el sitio libre para otro!
    private void OnDisable()
    {
        ReleaseReservedPoint();
    }

    private Health FindEnemyInAttackRange()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, attackRange);

        foreach (Collider hit in hits)
        {
            if (hit.gameObject == gameObject) continue;

            UnitSensor unit = hit.GetComponent<UnitSensor>();
            if (unit != null && unit.GetTeam() != team)
            {
                Health hp = hit.GetComponent<Health>();
                if (hp != null && hp.gameObject.activeInHierarchy && HasLineOfSight(transform.position, hp.transform.position))
                {
                    return hp;
                }
            }
        }
        return null;
    }

    // --- NUEVO SISTEMA DE LÍNEA DE VISIÓN REAL (MULTIPUNTO) ---
    private bool HasLineOfSight(Vector3 origin, Vector3 target)
    {
        // El punto desde donde dispara nuestro arquero (sus ojos/arco)
        Vector3 startPos = origin + Vector3.up * heightOffset;

        // Calculamos 3 puntos distintos en el cuerpo del enemigo
        // (Asumimos que 'target' son los pies del enemigo)
        Vector3[] targetPoints = new Vector3[]
        {
            target + Vector3.up * (heightOffset * 1.2f), // 1. Cabeza (un poco por encima del centro)
            target + Vector3.up * heightOffset,          // 2. Pecho (el centro exacto)
            target + Vector3.up * (heightOffset * 0.4f)  // 3. Piernas (un poco por encima del suelo)
        };

        // Comprobamos los 3 puntos uno por uno
        foreach (Vector3 pointToCheck in targetPoints)
        {
            Vector3 direction = pointToCheck - startPos;
            float distance = direction.magnitude;

            // Disparamos el rayo. 
            // Si el rayo NO choca con ningún obstáculo de la máscara de paredes...
            if (!Physics.Raycast(startPos, direction, distance, obstacleMask))
            {
                // ¡Hemos encontrado un punto visible! 
                // No nos importa si el resto del cuerpo está tapado, tenemos tiro.
                return true;
            }
        }

        // Si el código llega hasta aquí, significa que los 3 rayos (cabeza, pecho y piernas) 
        // chocaron contra una pared. El enemigo está 100% a cubierto.
        return false;
    }

    private void FindHighestTacticalPoint()
    {
        if (currentTarget == null) return;

        UnitSensor targetSensor = currentTarget.GetComponent<UnitSensor>();
        if (targetSensor == null) return;

        Vector3 enemyPosition = targetSensor.GetPosition();
        ArcherTacticalPoint[] points = FindObjectsByType<ArcherTacticalPoint>(FindObjectsSortMode.None);

        ArcherTacticalPoint bestPoint = null;
        float bestScore = -9999f;
        bool canAttackFromAnyPoint = false;

        foreach (ArcherTacticalPoint point in points)
        {
            // NUEVO FILTRO: ¿Está ocupado por otro arquero? Si es así, pasamos al siguiente punto directamente.
            if (!point.IsAvailable(this)) continue;

            float distanceToEnemy = Vector3.Distance(point.transform.position, enemyPosition);

            NavMeshPath path = new NavMeshPath();
            if (agent.CalculatePath(point.transform.position, path) && path.status != NavMeshPathStatus.PathComplete)
            {
                continue;
            }

            if (distanceToEnemy > tacticalPointSearchRadius) continue;

            float score = point.transform.position.y * 10f;

            if (distanceToEnemy <= attackRange)
            {
                if (HasLineOfSight(point.transform.position, enemyPosition))
                {
                    score += 500f;
                    canAttackFromAnyPoint = true;
                }
                else
                {
                    score -= 50f;
                }
            }
            else
            {
                score -= (distanceToEnemy - attackRange) * 5f;
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestPoint = point;
            }
        }

        if (bestPoint != null && canAttackFromAnyPoint)
        {
            // Hemos encontrado un sitio guay. 
            // 1. Soltamos el sitio anterior si teníamos uno.
            ReleaseReservedPoint();

            // 2. Nos apoderamos del nuevo sitio.
            reservedPoint = bestPoint;
            reservedPoint.currentOccupant = this;

            selectedTacticalPoint = bestPoint.transform.position;
            hasSelectedTacticalPoint = true;
            agent.isStopped = false;
            agent.SetDestination(selectedTacticalPoint);
        }
        else
        {
            // Modo a la desesperada. Ya no necesitamos torre, la soltamos.
            ReleaseReservedPoint();
            hasSelectedTacticalPoint = false;
            agent.isStopped = false;

            Vector3 directionToSelf = (transform.position - enemyPosition).normalized;
            Vector3 desperatePosition = enemyPosition + (directionToSelf * (attackRange * 0.9f));

            agent.SetDestination(desperatePosition);
        }
    }

    private void TryAttack()
    {
        if (currentTarget == null) return;

        float distance = Vector3.Distance(transform.position, currentTarget.transform.position);
        if (distance > attackRange) return;
        if (!HasLineOfSight(transform.position, currentTarget.transform.position)) return;

        Vector3 lookPosition = currentTarget.transform.position;
        lookPosition.y = transform.position.y;
        transform.LookAt(lookPosition);

        if (Time.time < nextAttackTime) return;

        currentTarget.TakeDamage(damage);
        ShowShotLine();

        nextAttackTime = Time.time + attackCooldown;
    }

    private void ShowShotLine()
    {
        if (shotLine == null || currentTarget == null) return;
        shotLine.enabled = true;
        shotLine.SetPosition(0, transform.position + Vector3.up * heightOffset);
        shotLine.SetPosition(1, currentTarget.transform.position + Vector3.up * heightOffset);
        Invoke(nameof(HideShotLine), 0.08f);
    }

    private void HideShotLine()
    {
        if (shotLine != null) shotLine.enabled = false;
    }

    private bool HasReachedTacticalPoint()
    {
        if (agent == null || !hasSelectedTacticalPoint) return false;
        return !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.3f;
    }

    private void OnDrawGizmos()
    {
        float debugRange = Application.isPlaying ? currentDetectionRange : baseDetectionRange;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, debugRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (Application.isPlaying && currentTarget != null && drawTargetLine)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position + Vector3.up * heightOffset, currentTarget.transform.position + Vector3.up * heightOffset);
            Gizmos.DrawSphere(currentTarget.transform.position, 0.35f);
        }

        if (Application.isPlaying && hasSelectedTacticalPoint)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(selectedTacticalPoint, 0.4f);
            Gizmos.DrawLine(transform.position, selectedTacticalPoint);
        }
    }
}