using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyCombatController : MonoBehaviour
{
    public Transform player;
    public EquippedWeaponController weaponController;

    [Header("Combat Range Settings")]
    public float engageDistance = 5f;
    public float disengageDistance = 10f;

    [Header("Attack Behavior")]
    public float attackCooldownMin = 1.5f;
    public float attackCooldownMax = 3f;

    [Range(0f, 1f)] public float rushChance = 0.5f;
    public float rushAttackRange = 2f;

    [Header("Stalk Behavior")]
    public float circleRadius = 3.5f;
    public float stalkMoveSpeed = 2f;
    public float timeBetweenCirclesMin = 1.5f;
    public float timeBetweenCirclesMax = 3f;

    [Header("Follow-up Behavior")]
    public float followUpAttackChance = 0.5f;
    public float retreatDistance = 6f;
    public float retreatSpeed = 5f;

    [Header("Stalking Settings")]
    public float minStalkTime = 1.5f;
    public float maxStalkTime = 3.5f;

    private enum CombatState { Idle, Stalking, Rushing, Attacking, Retreating }

    private NavMeshAgent agent;
    private CombatState currentState;

    private bool isEngaged = false;
    private bool isAttacking = false;
    private float stateTimer;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (weaponController == null)
            weaponController = GetComponent<EquippedWeaponController>();
    }

    void Update()
    {
        if (player == null) return;

        float distToPlayer = Vector3.Distance(transform.position, player.position);

        if (!isEngaged && distToPlayer <= engageDistance)
        {
            EngageCombat();
        }
        else if (isEngaged && distToPlayer >= disengageDistance)
        {
            DisengageCombat();
        }

        if (isEngaged)
        {
            UpdateCombatState();
        }
    }

    public void EnterCombat(Transform target)
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (weaponController == null) weaponController = GetComponent<EquippedWeaponController>();

        player = target;
        EngageCombat();
        Debug.Log($"{name} has entered combat mode against: {target.name}");
    }

    void EngageCombat()
    {
        isEngaged = true;
        currentState = CombatState.Stalking;
        stateTimer = Random.Range(minStalkTime, maxStalkTime);
    }

    void DisengageCombat()
    {
        isEngaged = false;
        currentState = CombatState.Idle;
        agent.ResetPath();
        isAttacking = false;
    }

    void UpdateCombatState()
    {
        if (player == null || weaponController == null) return;

        stateTimer -= Time.deltaTime;

        switch (currentState)
        {
            case CombatState.Stalking:
                HandleStalking();
                break;

            case CombatState.Rushing:
                HandleRushing();
                break;

            case CombatState.Retreating:
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                {
                    TransitionToStalking();
                }
                break;
        }
    }

    void HandleStalking()
    {
        Vector3 direction = Vector3.Cross(Vector3.up, (player.position - transform.position).normalized);
        Vector3 destination = player.position + direction.normalized * circleRadius;

        if (NavMesh.SamplePosition(destination, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
        {
            agent.speed = stalkMoveSpeed;
            agent.SetDestination(hit.position);
        }

        if (stateTimer <= 0f)
        {
            float roll = Random.value;
            if (roll < rushChance)
            {
                currentState = CombatState.Rushing;
            }
            else
            {
                currentState = CombatState.Attacking;
                StartCoroutine(PerformAttackSequence());
            }
        }
    }

    void HandleRushing()
    {
        agent.speed = retreatSpeed;
        agent.SetDestination(player.position);

        if (Vector3.Distance(transform.position, player.position) <= rushAttackRange)
        {
            agent.ResetPath();
            currentState = CombatState.Attacking;
            StartCoroutine(PerformAttackSequence());
        }
    }

    IEnumerator PerformAttackSequence()
    {
        isAttacking = true;

        if (weaponController != null)
            weaponController.PerformAttack();

        yield return new WaitForSeconds(Random.Range(attackCooldownMin, attackCooldownMax));

        float roll = Random.value;
        if (roll < followUpAttackChance)
        {
            currentState = CombatState.Attacking;
            StartCoroutine(PerformAttackSequence());
        }
        else
        {
            RetreatFromPlayer();
        }

        isAttacking = false;
    }

    void RetreatFromPlayer()
    {
        Vector3 retreatDir = (transform.position - player.position).normalized;
        Vector3 retreatPos = transform.position + retreatDir * retreatDistance;

        if (NavMesh.SamplePosition(retreatPos, out NavMeshHit hit, retreatDistance, NavMesh.AllAreas))
        {
            agent.speed = retreatSpeed;
            agent.SetDestination(hit.position);
            currentState = CombatState.Retreating;
            stateTimer = Random.Range(minStalkTime, maxStalkTime);
        }
        else
        {
            TransitionToStalking();
        }
    }

    void TransitionToStalking()
    {
        currentState = CombatState.Stalking;
        stateTimer = Random.Range(minStalkTime, maxStalkTime);
    }
}