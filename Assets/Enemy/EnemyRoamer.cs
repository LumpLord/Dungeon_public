using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyRoamer : MonoBehaviour
{
    public enum RoamerState { Patrol, Alert, Chase, Searching }

    [Header("Combat Handoff")]
    public float combatStartDistance = 2.5f;
    public EnemyCombatController combatController;

    [Header("Patrol Settings")]
    public List<Transform> patrolRoute = new List<Transform>();
    public int currentPatrolIndex = 0;
    public float waitTimeAtPoint = 2f;

    [Header("Vision Settings")]
    public float alertViewAngle = 120f;
    public float chaseViewAngle = 90f;
    public float alertViewDistance = 20f;
    public float chaseViewDistance = 10f;
    public LayerMask playerLayer;
    public LayerMask obstacleLayer;

    [Header("Alert Settings")]
    public float alertDurationMin = 2f;
    public float alertDurationMax = 4f;
    public float smallMoveIntervalMin = 1f;
    public float smallMoveIntervalMax = 3f;
    public float smallStepDistance = 2f;
    public float alertCallRadius = 10f;

    [Header("Chase Settings")]
    public float chaseSpeed = 6f;
    public float patrolSpeed = 3.5f;
    public float losePlayerTimeout = 10f;
    public float initialChaseCallRadius = 30f;
    public float ongoingChaseCallRadius = 15f;

    [Header("Personality Traits")]
    [Range(0f, 1f)] public float bravery = 0.5f;
    [Range(0f, 1f)] public float groupBoost = 0.3f;
    [Range(0f, 1f)] public float interest = 0.5f;
    [Range(0f, 1f)] public float fear = 0.3f;
    [Range(0f, 1f)] public float aggressiveness = 0.5f;

    private RoamerState currentState = RoamerState.Patrol;
    private NavMeshAgent agent;
    private float waitTimer = 0f;
    private float alertTimer = 0f;
    private float timeWatchedPlayer = 0f;
    private float moveIntervalTimer = 0f;
    private Transform targetPlayer;
    private Vector3 lastKnownPlayerPosition;
    private float lostPlayerTimer = 0f;
    private bool waiting = false;
    private float helpCallCooldown = 1f;
    private float helpCallTimer = 0f;
    private float searchTimer = 0f;
    private float baseSearchTime = 8f;
    private bool searching = false;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if (combatController == null)
            combatController = GetComponent<EnemyCombatController>();

        if (combatController != null)
            combatController.OnCombatDisengaged += HandleCombatExit;
        else
            Debug.LogError($"{name} has no EnemyCombatController assigned!");

        if (patrolRoute != null && patrolRoute.Count > 0)
            MoveToNextPatrolPoint();
    }

    void Update()
    {
        switch (currentState)
        {
            case RoamerState.Patrol:
                PatrolBehavior();
                ScanForPlayer();
                break;

            case RoamerState.Alert:
                AlertBehavior();
                ScanForPlayer();
                break;

            case RoamerState.Chase:
                ChaseBehavior();
                break;

            case RoamerState.Searching:
                SearchBehavior();
                ScanForPlayer();
                break;
        }
    }

    void PatrolBehavior()
    {
        agent.speed = patrolSpeed;

        if (agent.pathPending)
            return;

        if (waiting)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitTimeAtPoint)
            {
                waiting = false;
                MoveToNextPatrolPoint();
            }
        }
        else if (agent.remainingDistance <= agent.stoppingDistance)
        {
            waitTimer = 0f;
            waiting = true;
        }
    }

    void AlertBehavior()
    {
        alertTimer -= Time.deltaTime;
        moveIntervalTimer -= Time.deltaTime;

        if (moveIntervalTimer <= 0f)
        {
            moveIntervalTimer = Random.Range(smallMoveIntervalMin, smallMoveIntervalMax);

            Vector3 direction = (lastKnownPlayerPosition - transform.position).normalized;
            Vector3 smallMoveTarget = transform.position + direction * smallStepDistance;

            if (NavMesh.SamplePosition(smallMoveTarget, out NavMeshHit hit, smallStepDistance, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
        }

        if (alertTimer <= 0f)
        {
            Debug.Log($"{name} finished investigating, returning to patrol.");
            currentState = RoamerState.Patrol;
            MoveToNextPatrolPoint();
        }
    }

    void ChaseBehavior()
    {
        agent.speed = chaseSpeed;

        if (targetPlayer != null)
        {
            float distance = Vector3.Distance(transform.position, targetPlayer.position);

            if (distance <= combatStartDistance)
            {
                Debug.Log($"{name} reached combat range — handing off to EnemyCombatController");
                if (combatController != null)
                {
                    combatController.EnterCombat(targetPlayer);
                }
                enabled = false;
                return;
            }

            lastKnownPlayerPosition = targetPlayer.position;
            agent.SetDestination(targetPlayer.position);

            helpCallTimer -= Time.deltaTime;
            if (helpCallTimer <= 0f)
            {
                CallNearbyAllies(ongoingChaseCallRadius, RoamerState.Chase);
                helpCallTimer = helpCallCooldown;
            }

            if (!CanSeePlayer(targetPlayer, chaseViewAngle, chaseViewDistance))
            {
                lostPlayerTimer += Time.deltaTime;
                if (lostPlayerTimer >= losePlayerTimeout)
                {
                    StartSearch();
                }
            }
            else
            {
                lostPlayerTimer = 0f;
            }
        }
    }

    void SearchBehavior()
    {
        if (!searching)
        {
            MoveToRandomNearbyPosition();
            searching = true;
        }

        searchTimer -= Time.deltaTime;

        if (searchTimer <= 0f)
        {
            Debug.Log($"{name} gave up searching, returning to patrol.");
            currentState = RoamerState.Patrol;
            MoveToNextPatrolPoint();
        }
    }

    void MoveToNextPatrolPoint()
    {
        if (patrolRoute.Count == 0) return;

        agent.SetDestination(patrolRoute[currentPatrolIndex].position);
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolRoute.Count;
    }

    void MoveToRandomNearbyPosition()
    {
        Vector3 randomOffset = Random.insideUnitSphere * 5f;
        randomOffset.y = 0f;
        Vector3 targetPos = lastKnownPlayerPosition + randomOffset;

        if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    public void GoToAlert(Vector3 alertPosition)
    {
        lastKnownPlayerPosition = alertPosition;
        alertTimer = Random.Range(alertDurationMin, alertDurationMax);
        moveIntervalTimer = Random.Range(smallMoveIntervalMin, smallMoveIntervalMax);
        currentState = RoamerState.Alert;
        Debug.Log($"{name} is investigating suspicious activity!");
    }

    void ScanForPlayer()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, alertViewDistance, playerLayer);

        foreach (var hit in hits)
        {
            Vector3 directionToPlayer = (hit.transform.position - transform.position).normalized;
            float distanceToPlayer = Vector3.Distance(transform.position, hit.transform.position);
            float angle = Vector3.Angle(transform.forward, directionToPlayer);

            if (!Physics.Raycast(transform.position, directionToPlayer, distanceToPlayer, obstacleLayer))
            {
                if (distanceToPlayer <= chaseViewDistance && angle < chaseViewAngle / 2f)
                {
                    Debug.Log($"{name} sees player in Chase Cone! Switching to chase mode.");
                    targetPlayer = hit.transform;
                    currentState = RoamerState.Chase;
                    CallNearbyAllies(initialChaseCallRadius, RoamerState.Chase);
                    return;
                }
                else if (distanceToPlayer <= alertViewDistance && angle < alertViewAngle / 2f)
                {
                    if (currentState != RoamerState.Alert)
                    {
                        Debug.Log($"{name} spotted player in Alert Cone! Starting to investigate.");
                        GoToAlert(hit.transform.position);
                        CallNearbyAllies(alertCallRadius, RoamerState.Alert);
                        timeWatchedPlayer = 0f;
                    }
                    else
                    {
                        timeWatchedPlayer += Time.deltaTime;

                        if (timeWatchedPlayer >= Random.Range(alertDurationMin, alertDurationMax))
                        {
                            Debug.Log($"{name} watched player too long. Switching to chase mode.");
                            targetPlayer = hit.transform;
                            currentState = RoamerState.Chase;
                            CallNearbyAllies(initialChaseCallRadius, RoamerState.Chase);
                        }
                    }
                    return;
                }
            }
        }
    }

    void HandleCombatExit()
    {
        Debug.Log($"{name} exited combat. Returning to patrol.");
        currentState = RoamerState.Patrol;
        MoveToNextPatrolPoint();
        enabled = true;
    }

    void StartSearch()
    {
        searchTimer = baseSearchTime * (0.5f + interest) * CalculateEffectiveBravery();
        Debug.Log($"{name} lost player. Starting search for {searchTimer:F1} seconds.");
        currentState = RoamerState.Searching;
        searching = false;
    }

    bool CanSeePlayer(Transform player, float viewAngle, float viewDistance)
    {
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        float angle = Vector3.Angle(transform.forward, directionToPlayer);

        if (angle < viewAngle / 2f && distanceToPlayer <= viewDistance)
        {
            if (!Physics.Raycast(transform.position, directionToPlayer, distanceToPlayer, obstacleLayer))
                return true;
        }
        return false;
    }

    public void ForceChase(Transform player)
    {
        targetPlayer = player;
        currentState = RoamerState.Chase;
        CallNearbyAllies(initialChaseCallRadius, RoamerState.Chase);
    }

    void CallNearbyAllies(float radius, RoamerState stateToForce)
    {
        Collider[] nearby = Physics.OverlapSphere(transform.position, radius);
        foreach (var collider in nearby)
        {
            if (collider.TryGetComponent(out EnemyRoamer ally) && ally != this)
            {
                if (stateToForce == RoamerState.Chase && ally.currentState != RoamerState.Chase)
                {
                    ally.ForceChase(targetPlayer);
                }
                else if (stateToForce == RoamerState.Alert && ally.currentState == RoamerState.Patrol)
                {
                    ally.GoToAlert(lastKnownPlayerPosition);
                }
            }
        }
    }

    float CalculateEffectiveBravery()
    {
        int nearbyAllies = CountNearbyAllies();
        float effectiveBravery = bravery - fear + (nearbyAllies * groupBoost) + (aggressiveness * 0.5f);
        return Mathf.Clamp01(effectiveBravery);
    }

    int CountNearbyAllies()
    {
        Collider[] nearby = Physics.OverlapSphere(transform.position, 10f);
        int count = 0;
        foreach (var collider in nearby)
        {
            if (collider.CompareTag("Enemy") && collider.gameObject != this.gameObject)
                count++;
        }
        return count;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, alertViewDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseViewDistance);
    }

    public void ReceiveDamage(float amount)
    {
        fear += amount * 0.1f;
        fear = Mathf.Clamp01(fear);
        Debug.Log($"{name} received damage, fear now {fear:F2}");
    }
}