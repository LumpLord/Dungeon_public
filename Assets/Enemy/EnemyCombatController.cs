using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
//using HealthSystem;   



[RequireComponent(typeof(NavMeshAgent))]
public class EnemyCombatController : MonoBehaviour
{
    [Header("Debug Settings")]
    public bool debugEnabled = false;
    [HideInInspector]
    public float currentStateEndTime;
    public bool overrideRushRangeUsed = false;

    [Header("Fallback Search")]
    public EnemyCombatStateBase searchState;

    public System.Action OnCombatDisengaged;

    public Transform player;
    public EquippedWeaponController weaponController;

    [Header("Combat Range Settings")]
    public float engageDistance = 5f;
    public float disengageDistance = 10f;
    public float maxAttackStateDistance = 10f;
    public float maxStalkStateDistance = 12f;
    [Header("Rush Behavior Settings")]
    public float rushCompleteDistance = 2f;
    public bool completeRushAfterReengageRange = true;

    [Header("Combat Vision Settings")]
    public float combatVisionDistance = 12f;
    public float combatVisionAngle = 150f;
    public float combatLoseTimeout = 3f;

    [Header("Experimental AI Settings")]
    public bool useModularAI = true;
    public EnemyCombatBehaviorProfile behaviorProfile;
    public EnemyCombatStateBase searchFallbackState;

    private HealthComponent health;
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

    [Header("Attack Behavior Settings")]
    public float attackReplanBuffer = 1.5f;

    [Header("Adaptive Behavior Settings")]
    public bool increaseAggressionAfterFailedAttack = true;

    [Header("Hit Engage Hold")]
    [Tooltip("Seconds to remain in combat after taking damage, regardless of distance.")]
    public float hitEngageHoldTime = 5f;
    private float stayEngagedUntil = 0f;

    private bool recentlyFailedAttack = false;
    private Vector3 investigatePoint;

    private Dictionary<string, float> stateCooldowns = new();
    private Dictionary<string, float> bannedStatesUntil = new();
    public void NotifyFailedAttack()
    {
        if (increaseAggressionAfterFailedAttack)
            recentlyFailedAttack = true;
    }

    public void InitializeCombatStates()
    {
        if (behaviorProfile == null)
        {
            if (debugEnabled)
            {
                Debug.LogError($"{name} has no behavior profile assigned.");
            }
            return;
        }

        foreach (var ws in behaviorProfile.weightedStates)
        {
            if (ws.stateComponent == null)
            {
                if (debugEnabled)
                {
                    Debug.LogError($"{name} behavior profile contains null stateComponent.");
                }
            }
            else
            {
                if (debugEnabled)
                {
                    Debug.Log($"{name} registered state: {ws.stateComponent.GetStateName()}");
                }
            }
        }

        // Confirm that PursuitState is registered
        var pursuit = GetStateByName("PursuitState");
        if (pursuit == null)
        {
            if (debugEnabled)
            {
                Debug.LogWarning($"{name} PursuitState not found in behavior profile. Ensure it's included for fallback logic.");
            }
        }
        else
        {
            if (debugEnabled)
            {
                Debug.Log($"{name} PursuitState registered successfully.");
            }
        }
    }

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        weaponController ??= GetComponent<EquippedWeaponController>();

        health = GetComponent<HealthComponent>();
        if (health != null)
            health.OnDamaged += HandleDamaged;

        InitializeCombatStates();

        // Auto-assign player if not already set
        if (player == null)
        {
            GameObject foundPlayer = GameObject.FindGameObjectWithTag("Player");
            if (foundPlayer != null)
            {
                player = foundPlayer.transform;
                if (debugEnabled)
                {
                    Debug.Log($"{name} auto-assigned player: {player.name}");
                }
            }
            else
            {
                if (debugEnabled)
                {
                    Debug.LogWarning($"{name} could not auto-assign player — no GameObject with tag 'Player' found.");
                }
            }
        }
    }
    private void HandleDamaged(float dmg, DamageType type, GameObject source, Vector3 hitPoint)
    {
        // Prevent immediate disengage for a short period
        stayEngagedUntil = Time.time + hitEngageHoldTime;

        // Cache where the hit landed for InvestigateState
        investigatePoint = hitPoint;

        if (!isEngaged && source != null && source.CompareTag("Player"))
        {
            EnterCombat(source.transform);
            ForceChargeIfFar();

            // Queue an investigate state when combat starts from a distant hit
            EnqueueForceState("InvestigateState");
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
        else if (isEngaged && Time.time >= stayEngagedUntil && distToPlayer >= disengageDistance)
        {
            DisengageCombat();
            agent.updateRotation = true;

        }
    }

    public void EnterCombat(Transform target)
    {
        agent.updateRotation = false;
        agent ??= GetComponent<NavMeshAgent>();
        weaponController ??= GetComponent<EquippedWeaponController>();
        player = target;
        EngageCombat();
        // Ensure enemy closes gap if combat starts at long distance
        ForceChargeIfFar();
        if (debugEnabled) 
        {
            Debug.Log($"{name} has entered combat mode against: {target.name}");
        }
    }

    /// <summary>
    /// Immediately enqueue a rush / pursuit state when the player is out of normal engage range.
    /// </summary>
    private void ForceChargeIfFar()
    {
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > engageDistance)
        {
            // Allow rush state to execute even outside its normal range
            overrideRushRange = true;

            // Prefer RushStateTest; fall back to PursuitState if not present
            if (GetStateByName("RushStateTest") != null)
                EnqueueForceState("RushStateTest");
            else if (GetStateByName("PursuitState") != null)
                EnqueueForceState("PursuitState");
        }
    }

    public void EngageCombat()
    {
        if (isInCombat) return;

        if (debugEnabled)
        {
            Debug.Log($"{name} EngageCombat() called with target: {(player != null ? player.name : "NULL")}");
        }
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
        if (debugEnabled)
        {
            Debug.Log($"{name} disengaging from combat. Returning to patrol.");
        }
        OnCombatDisengaged?.Invoke();
    }

    IEnumerator ModularCombatLoop()
    {
        if (debugEnabled)
        {
            Debug.Log($"{name} started ModularCombatLoop. Target is {(player != null ? player.name : "NULL")}");
        }

        while (isEngaged)
        {
            if (player != null)
                lastKnownPlayerPosition = player.position;

            if (!PlayerInCombatVision())
            {
                timeSincePlayerSeen += Time.deltaTime;

                if (timeSincePlayerSeen >= combatLoseTimeout)
                {
                    if (debugEnabled)
                    {
                        Debug.LogWarning($"{name} lost player. Initiating fallback search...");
                    }
                    modularCombatQueue.Clear();

                    if (searchFallbackState != null)
                    {
                        EnqueueState(searchFallbackState);
                    }
                    else
                    {
                        if (debugEnabled)
                        {
                            Debug.LogWarning($"{name} has no search fallback. Disengaging.");
                        }
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
                    if (debugEnabled)
                    {
                        Debug.LogWarning($"{name} dequeued a null combat state. Skipping...");
                    }
                    continue;
                }




                lastExecutedState = (next as EnemyCombatStateBase)?.GetStateName() ?? "Unknown";
                if (debugEnabled)
                {
                    Debug.Log($"{name} is starting behavior: {lastExecutedState}");
                }

                if (Time.time < nextAvailableStateTime)
                {
                    float waitTime = nextAvailableStateTime - Time.time;
                    yield return new WaitForSeconds(waitTime);
                }

                if (next is EnemyCombatStateBase concreteState)
                    concreteState.EnterState(this);
                else
                {
                    if (debugEnabled)
                    {
                        Debug.LogWarning($"{name} tried to call EnterState on non-EnemyCombatStateBase: {next.GetType().Name}");
                    }
                }

                Coroutine behaviorCoroutine = StartCoroutine(WrapWithVisionCheck(next.Execute(this), next));
                yield return behaviorCoroutine;
                if (debugEnabled)
                {
                    Debug.Log($"{name} finished behavior: {lastExecutedState}");
                }

                yield return new WaitForSeconds(0.05f);
            }
            else
            {
                if (TryRetargetPlayer())
                {
                    if (debugEnabled)
                    {
                        Debug.Log($"{name} retargeted successfully.");
                    }
                    continue;
                }
                else if (searchFallbackState != null)
                {
                    if (debugEnabled)
                    {
                        Debug.LogWarning($"{name} failed to retarget. Using fallback search...");
                    }
                    yield return StartCoroutine(searchFallbackState.Execute(this));
                    yield break;
                }
                else
                {
                    if (debugEnabled)
                    {
                        Debug.LogError($"{name} has no options left. Disengaging.");
                    }
                    DisengageCombat();
                    yield break;
                }
            }
        }
    }

    public void EnqueueState(IEnemyCombatState state)
    {
        string stateName = (state as EnemyCombatStateBase)?.GetStateName() ?? state?.GetType().Name ?? "NULL";
        if (state == null)
        {
            if (debugEnabled)
            {
                Debug.LogWarning($"{name} Attempted to enqueue NULL state.");
            }
            return;
        }
        if (stateCooldowns.TryGetValue(stateName, out float cooldownUntil) && Time.time < cooldownUntil)
        {
            if (debugEnabled)
            {
                Debug.LogWarning($"{name} Skipping {stateName} due to cooldown. Available again at {cooldownUntil}");
            }
            return;
        }

        if (bannedStatesUntil.TryGetValue(stateName, out float bannedUntil) && Time.time < bannedUntil)
        {
            if (debugEnabled)
            {
                Debug.LogWarning($"{name} Skipping {stateName} due to temporary ban. Available again at {bannedUntil}");
            }
            return;
        }
        // Cooldown check for RushStateTest
        if (stateName == "RushStateTest" && Time.time < nextAvailableStateTime)
        {
            if (debugEnabled)
            {
                Debug.LogWarning($"{name} Skipping RushStateTest due to cooldown. Available again at {nextAvailableStateTime}");
            }
            return;
        }
        if (!state.CanExecute(this))
        {
            if (stateName == "RushStateTest" && overrideRushRange) 
            {
                if (debugEnabled) 
                {
                    Debug.LogWarning($"{name} EnqueueState bypassing CanExecute for: {stateName} due to overrideRushRange");
                }
            }
            else
            {
                if (debugEnabled)
                {
                    Debug.LogWarning($"{name} Attempted to enqueue state that fails CanExecute: {stateName}");
                }
                return;
            }
        }

        if (debugEnabled)
        {
            Debug.Log($"{name} Enqueued state: {stateName}");
        }
        modularCombatQueue.Enqueue(state);
    }

    public void EnqueueForceState(string stateName)
    {
        var state = GetStateByName(stateName);
        if (state == null)
        {
            if (debugEnabled)
            {
                Debug.LogWarning($"{name} attempted to force-enqueue unknown state: {stateName}");
            }
            return;
        }

        if (debugEnabled)
        {
            Debug.Log($"{name} Force-enqueued state: {stateName}");
        }
        modularCombatQueue.Enqueue(state);
    }

    public void EnqueueWeightedFallback((IEnemyCombatState state, float weight)[] weightedOptions)
    {
        var chosen = WeightedRandomSelector.Choose(weightedOptions);
        if (chosen != null)
        {
            EnqueueState(chosen);
        }
        else
        {
            if (debugEnabled)
            {
                Debug.LogWarning($"{name} EnqueueWeightedFallback failed to select a valid state.");
            }
        }
    }

    public EnemyCombatStateBase GetStateByName(string stateName)
    {
        var state = behaviorProfile.weightedStates
            .Select(ws => ws.stateComponent as EnemyCombatStateBase)
            .FirstOrDefault(state => state != null && state.GetStateName() == stateName);
        if (debugEnabled)
        {
            Debug.Log($"{name} GetStateByName({stateName}) → {(state == null ? "NULL" : "FOUND")}");
        }
        return state;
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

    public void EnqueueRandomBehaviorState()
    {
        if (behaviorProfile == null || behaviorProfile.weightedStates.Count == 0) return;

        bool isFirstState = string.IsNullOrEmpty(lastExecutedState);
        EnemyCombatStateBase previousState = GetStateByName(lastExecutedState);

        // Debug log: listing all candidate states
        foreach (var ws in behaviorProfile.weightedStates)
        {
            if (ws.stateComponent == null)
            {
                if (debugEnabled)
                {
                    Debug.LogWarning($"{name} skipping null stateComponent in behaviorProfile.");
                }
                continue;
            }
        }
        if (debugEnabled)
        {
            Debug.Log($"{name} evaluating {behaviorProfile.weightedStates.Count} behavior states...");
        }

        var validStates = behaviorProfile.weightedStates
            .Where(ws =>
            {
                if (ws.stateComponent == null)
                {
                    if (debugEnabled)
                    {
                        Debug.LogWarning($"{name} skipping null stateComponent in behaviorProfile.");
                    }
                    return false;
                }

                var state = ws.stateComponent as EnemyCombatStateBase;
                if (!state.CanExecute(this))
                {
                    if (debugEnabled)
                    {
                        Debug.LogWarning($"{name} rejected state {state.GetStateName()} — failed CanExecute.");
                    }
                    return false;
                }

                if (!isFirstState && !state.CanRunAfter(previousState))
                {
                    if (debugEnabled)
                    {
                        Debug.LogWarning($"{name} rejected state {state.GetStateName()} — cannot run after {previousState?.GetStateName()}.");
                    }
                    return false;
                }

                if (debugEnabled)
                {
                    Debug.Log($"{name} accepted state {state.GetStateName()} with weight {ws.weight}.");
                }
                return true;
            })
            .ToList();

        if (debugEnabled)
        {
            Debug.Log($"{name} found {validStates.Count} valid states after filtering.");
        }

        if (validStates.Count == 0)
        {
            if (debugEnabled)
            {
                Debug.LogWarning($"{name} has no valid combat states available.");
            }
            return;
        }

        if (isFirstState)
        {
            if (debugEnabled)
            {
                Debug.Log($"{name} is choosing initial combat state (no previous state).");
            }
        }

        float bonusWeightMultiplier = recentlyFailedAttack ? 1.5f : 1f;

        float totalWeight = validStates.Sum(ws =>
        {
            string sname = (ws.stateComponent as EnemyCombatStateBase)?.GetStateName();
            bool isAggressive = sname == "AttackStateTest" || sname == "RushStateTest";
            return isAggressive ? ws.weight * bonusWeightMultiplier : ws.weight;
        });

        float roll = Random.Range(0, totalWeight);
        float cumulative = 0f;

        foreach (var entry in validStates)
        {
            string sname = (entry.stateComponent as EnemyCombatStateBase)?.GetStateName();
            bool isAggressive = sname == "AttackStateTest" || sname == "RushStateTest";
            float effectiveWeight = isAggressive ? entry.weight * bonusWeightMultiplier : entry.weight;
            if (debugEnabled)
            {
                Debug.Log($"{name} rolling {roll:F2} against cumulative {cumulative:F2} for state {sname} (weight: {effectiveWeight:F2})");
            }
            cumulative += effectiveWeight;
            if (roll <= cumulative)
            {
                if (entry.stateComponent is IEnemyCombatState state)
                {
                    EnqueueState(state);
                    recentlyFailedAttack = false; // reset once used
                    return;
                }
            }
        }
    }

    private IEnumerator WrapWithVisionCheck(IEnumerator behavior, IEnemyCombatState state)
    {
        // Save and possibly override combatVisionDistance for RushState
        float originalVisionDistance = combatVisionDistance;
        if (lastExecutedState != null && lastExecutedState.Contains("RushState"))
        {
            combatVisionDistance = 40f; // Temporarily expand vision range during rush
        }

        timeSincePlayerSeen = 0f;

        while (true)
        {
            if (!behavior.MoveNext())
            {
                combatVisionDistance = originalVisionDistance;
                if (debugEnabled)
                {
                    Debug.Log($"{name} finished WrapWithVisionCheck for: {lastExecutedState}");
                }
                yield break;
            }

            if (!PlayerInCombatVision())
            {
                timeSincePlayerSeen += Time.deltaTime;
                if (timeSincePlayerSeen >= combatLoseTimeout)
                {
                    combatVisionDistance = originalVisionDistance;
                    if (debugEnabled)
                    {
                        Debug.LogWarning($"{name} lost player mid-behavior: {lastExecutedState}");
                    }
                    modularCombatQueue.Clear();
                    DisengageCombat();
                    yield break;
                }
            }
            else
            {
                timeSincePlayerSeen = 0f;
                lastKnownPlayerPosition = player.position;
                // If we are in the else branch after losing player, restore vision in case of break
                // (Not strictly necessary here, but if you want before a yield break, you'd add it)
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

    public bool overrideRushRange = false;

    private bool TryRetargetPlayer()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, retargetRadius, playerLayer);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                player = hit.transform;
                overrideRushRange = true;
                lastKnownPlayerPosition = player.position;
                if (debugEnabled)
                {
                    Debug.Log($"{name} re-acquired player via fail-safe retarget.");
                }
                return true;
            }
        }
        return false;
    }

    public Transform GetTarget() => player;
    public NavMeshAgent GetAgent() => agent;
    public EquippedWeaponController GetWeaponController() => weaponController;
    public Vector3 GetLastKnownPlayerPosition() => lastKnownPlayerPosition;
    public Vector3 GetInvestigatePoint() => investigatePoint;

    public void RequestInterruptAndReplan()
    {
        modularCombatQueue.Clear();
        EnqueueRandomBehaviorState();
    }

    public void RegisterStateAbortReason(string reason)
    {
        if (debugEnabled)
        {
            Debug.LogWarning($"{name} aborted combat state: {reason}");
        }
    }


    /// <summary>
    /// Smoothly rotates this enemy toward a target position at a defined rotation speed and with a maximum turn delta per frame.
    /// </summary>
    /// <param name="targetPosition">The world position to rotate toward.</param>
    /// <param name="rotationSpeed">How fast to rotate (degrees per second).</param>
    /// <param name="maxTurnDelta">Maximum degrees to turn in a single frame.</param>
    public void RotateTowardsTarget(Vector3 targetPosition, float rotationSpeed, float maxTurnDelta)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            float angleDelta = Mathf.Min(rotationSpeed * Time.deltaTime * 100f, maxTurnDelta);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, angleDelta);
        }
    }

    public void SetStateCooldown(string stateName, float cooldownTime)
    {
        stateCooldowns[stateName] = Time.time + cooldownTime;
        if (debugEnabled)
        {
            Debug.Log($"{name} cooldown set for {stateName} until {stateCooldowns[stateName]}");
        }
    }

    public void BanStateForSeconds(string stateName, float duration)
    {
        bannedStatesUntil[stateName] = Time.time + duration;
        if (debugEnabled)
        {
            Debug.Log($"{name} temporarily banned {stateName} until {bannedStatesUntil[stateName]}");
        }
    }
    void OnDisable()
    {
        if (health != null)
            health.OnDamaged -= HandleDamaged;
    }
}