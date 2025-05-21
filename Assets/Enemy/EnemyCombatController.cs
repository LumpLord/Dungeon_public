using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyCombatController : MonoBehaviour
{
    [HideInInspector] public float currentStateEndTime;

    [Header("Fallback Search")]
    public EnemyCombatStateBase searchState;

    public System.Action OnCombatDisengaged;

    public Transform player;
    public EquippedWeaponController weaponController;

    [Header("Combat Range Settings")]
    public float engageDistance = 5f;
    public float disengageDistance = 10f;

    [Header("Combat Vision Settings")]
    public float combatVisionDistance = 12f;
    public float combatVisionAngle = 150f;
    public float combatLoseTimeout = 3f;

    [Header("Experimental AI Settings")]
    public bool useModularAI = true;
    public EnemyCombatBehaviorProfile behaviorProfile;
    public EnemyCombatStateBase searchFallbackState;

    private NavMeshAgent agent;
    private bool isEngaged = false;
    private Coroutine stateRoutine;

    private Queue<IEnemyCombatState> modularCombatQueue = new();
    public string lastExecutedState = "";
    private float timeSincePlayerSeen = 0f;
    private Vector3 lastKnownPlayerPosition;

    [Header("Fail-Safe Settings")]
    public float retargetRadius = 20f;
    public LayerMask playerLayer;

    public bool isInCombat = false;

    // Stage 2 additions
    private float nextAvailableStateTime = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        weaponController ??= GetComponent<EquippedWeaponController>();

        // Auto-assign player if not already set
        if (player == null)
        {
            GameObject foundPlayer = GameObject.FindGameObjectWithTag("Player");
            if (foundPlayer != null)
            {
                player = foundPlayer.transform;
                Debug.Log($"{name} auto-assigned player: {player.name}");
            }
            else
            {
                Debug.LogWarning($"{name} could not auto-assign player — no GameObject with tag 'Player' found.");
            }
        }
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
    }

    public void EnterCombat(Transform target)
    {
        agent.updateRotation = false;
        agent ??= GetComponent<NavMeshAgent>();
        weaponController ??= GetComponent<EquippedWeaponController>();
        player = target;
        EngageCombat();
        Debug.Log($"{name} has entered combat mode against: {target.name}");
    }

    public void EngageCombat()
    {
        if (isInCombat) return;

        Debug.Log($"{name} EngageCombat() called with target: {(player != null ? player.name : "NULL")}");
        isEngaged = true;
        timeSincePlayerSeen = 0f;

        if (stateRoutine != null)
            StopCoroutine(stateRoutine);

        isInCombat = true;
        stateRoutine = StartCoroutine(ModularCombatLoop());
    }

    public void DisengageCombat()
    {
        isEngaged = false;
        isInCombat = false;
        agent.ResetPath();

        if (stateRoutine != null)
        {
            StopCoroutine(stateRoutine);
            stateRoutine = null;
        }

        modularCombatQueue.Clear();
        Debug.Log($"{name} disengaging from combat. Returning to patrol.");
        OnCombatDisengaged?.Invoke();
    }

    IEnumerator ModularCombatLoop()
    {
        Debug.Log($"{name} started ModularCombatLoop. Target is {(player != null ? player.name : "NULL")}");

        while (isEngaged)
        {
            if (player != null)
                lastKnownPlayerPosition = player.position;

            if (!PlayerInCombatVision())
            {
                timeSincePlayerSeen += Time.deltaTime;

                if (timeSincePlayerSeen >= combatLoseTimeout)
                {
                    Debug.LogWarning($"{name} lost player. Initiating fallback search...");
                    modularCombatQueue.Clear();

                    if (searchFallbackState != null)
                    {
                        EnqueueState(searchFallbackState);
                    }
                    else
                    {
                        Debug.LogWarning($"{name} has no search fallback. Disengaging.");
                        DisengageCombat();
                        yield break;
                    }
                }
            }
            else
            {
                timeSincePlayerSeen = 0f;
            }

            if (modularCombatQueue.Count == 0)
            {
                EnqueueRandomBehaviorState();
                yield return new WaitForSeconds(0.1f);
            }

            if (modularCombatQueue.Count > 0)
            {
                IEnemyCombatState next = modularCombatQueue.Dequeue();
                if (next == null)
                {
                    Debug.LogWarning($"{name} dequeued a null combat state. Skipping...");
                    continue;
                }

                if (!next.CanExecute(this))
                {
                    string stateName = (next as EnemyCombatStateBase)?.GetStateName() ?? "Unknown";
                    lastExecutedState = stateName;
                    Debug.LogWarning($"{name} tried to execute invalid state: {stateName}");
                    continue;
                } 

                lastExecutedState = (next as EnemyCombatStateBase)?.GetStateName() ?? "Unknown";
                Debug.Log($"{name} is starting behavior: {lastExecutedState}");

                if (Time.time < nextAvailableStateTime)
                {
                    float waitTime = nextAvailableStateTime - Time.time;
                    yield return new WaitForSeconds(waitTime);
                }

                if (next is EnemyCombatStateBase concreteState)
                    concreteState.EnterState(this);
                else
                    Debug.LogWarning($"{name} tried to call EnterState on non-EnemyCombatStateBase: {next.GetType().Name}");

                Coroutine behaviorCoroutine = StartCoroutine(WrapWithVisionCheck(next.Execute(this), next));
                yield return behaviorCoroutine;
                Debug.Log($"{name} finished behavior: {lastExecutedState}");

                yield return new WaitForSeconds(0.05f);
            }
            else
            {
                if (TryRetargetPlayer())
                {
                    Debug.Log($"{name} retargeted successfully.");
                    continue;
                }
                else if (searchFallbackState != null)
                {
                    Debug.LogWarning($"{name} failed to retarget. Using fallback search...");
                    yield return StartCoroutine(searchFallbackState.Execute(this));
                    yield break;
                }
                else
                {
                    Debug.LogError($"{name} has no options left. Disengaging.");
                    DisengageCombat();
                    yield break;
                }
            }
        }
    }

    public void EnqueueState(IEnemyCombatState state) => modularCombatQueue.Enqueue(state);

    private EnemyCombatStateBase GetStateByName(string stateName)
    {
        return behaviorProfile.weightedStates
            .Select(ws => ws.stateComponent as EnemyCombatStateBase)
            .FirstOrDefault(state => state != null && state.GetStateName() == stateName);
    }

    public void FaceTargetLocked()
    {
        if (player == null) return;

        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    public void FaceTargetSmooth()
    {
        if (player == null) return;

        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }

    private void EnqueueRandomBehaviorState()
    {
        if (behaviorProfile == null || behaviorProfile.weightedStates.Count == 0) return;

        bool isFirstState = string.IsNullOrEmpty(lastExecutedState);
        EnemyCombatStateBase previousState = GetStateByName(lastExecutedState);

        var validStates = behaviorProfile.weightedStates
            .Where(ws =>
                ws.stateComponent is EnemyCombatStateBase state &&
                state.CanExecute(this) &&
                (isFirstState || state.CanRunAfter(previousState))
            )
            .ToList();

        if (validStates.Count == 0)
        {
            Debug.LogWarning($"{name} has no valid combat states available.");
            return;
        }

        if (isFirstState)
        {
            Debug.Log($"{name} is choosing initial combat state (no previous state).");
        }

        float totalWeight = validStates.Sum(ws => ws.weight);
        float roll = Random.Range(0, totalWeight);
        float cumulative = 0f;

        foreach (var entry in validStates)
        {
            cumulative += entry.weight;
            if (roll <= cumulative)
            {
                if (entry.stateComponent is IEnemyCombatState state)
                {
                    EnqueueState(state);
                    return;
                }
            }
        }
    }

    private IEnumerator WrapWithVisionCheck(IEnumerator behavior, IEnemyCombatState state)
    {
        timeSincePlayerSeen = 0f;

        while (true)
        {
            if (!behavior.MoveNext())
            {
                Debug.Log($"{name} finished WrapWithVisionCheck for: {lastExecutedState}");
                yield break;
            }

            if (!PlayerInCombatVision())
            {
                timeSincePlayerSeen += Time.deltaTime;
                if (timeSincePlayerSeen >= combatLoseTimeout)
                {
                    Debug.LogWarning($"{name} lost player mid-behavior: {lastExecutedState}");
                    modularCombatQueue.Clear();
                    DisengageCombat();
                    yield break;
                }
            }
            else
            {
                timeSincePlayerSeen = 0f;
                lastKnownPlayerPosition = player.position;
            }

            yield return behavior.Current;
        }
    }

    public bool PlayerInCombatVision()
    {
        if (player == null) return false;

        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, dirToPlayer);
        float distance = Vector3.Distance(transform.position, player.position);

        return angle < combatVisionAngle / 2f && distance <= combatVisionDistance;
    }

    private bool TryRetargetPlayer()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, retargetRadius, playerLayer);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                player = hit.transform;
                lastKnownPlayerPosition = player.position;
                Debug.Log($"{name} re-acquired player via fail-safe retarget.");
                return true;
            }
        }
        return false;
    }

    public Transform GetTarget() => player;
    public NavMeshAgent GetAgent() => agent;
    public EquippedWeaponController GetWeaponController() => weaponController;
    public Vector3 GetLastKnownPlayerPosition() => lastKnownPlayerPosition;
}