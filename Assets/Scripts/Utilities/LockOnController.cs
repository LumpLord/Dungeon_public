using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Handles hard lock‑on logic: press <Q> to toggle lock on the nearest enemy
/// within radius, and scroll‑wheel cycles left / right while locked.
/// Other systems (camera, UI) listen to OnLockChanged to adjust visuals.
/// </summary>
public class LockOnController : MonoBehaviour
{
    [Header("Lock Settings")]
    [SerializeField] private KeyCode lockKey = KeyCode.Q;
    [SerializeField] private float lockRadius = 25f;
    [SerializeField] private LayerMask enemyMask = ~0; // default: everything

    public Transform CurrentTarget { get; private set; }
    public static Transform CurrentTargetStatic { get; private set; }
    public static Transform Player { get; private set; }

    /// <summary>Raised whenever the current lock target changes (null = unlock).</summary>
    public static event Action<Transform> OnLockChanged;

    private HealthComponent currentTargetHealth;

    // ---------------------------------------------------------------------

    private void Start()
    {
        // Cache the player's transform for camera‑alignment helpers
        Player = transform;
        CurrentTargetStatic = null;
    }
    
    void Update()
    {
        // Toggle hard‑lock
        if (Input.GetKeyDown(lockKey))
            ToggleLock();

        // Cycle while locked
        float scroll = Input.mouseScrollDelta.y;
        if (CurrentTarget && Mathf.Abs(scroll) > 0.01f)
            CycleTarget(clockwise: scroll > 0);

        // Auto‑clear if target destroyed / too far
        if (CurrentTarget && !IsTargetValid(CurrentTarget))
            ClearLock();
    }

    // ---------------------------------------------------------------------
    private void ToggleLock()
    {
        if (CurrentTarget)
        {
            ClearLock();
        }
        else
        {
            Transform target = FindNearestTarget();
            if (target == null) return;

            CurrentTarget = target;
            CurrentTargetStatic = target;

            currentTargetHealth = target.GetComponent<HealthComponent>();
            if (currentTargetHealth)
                currentTargetHealth.OnDeath += HandleTargetDeath;

            OnLockChanged?.Invoke(CurrentTarget);
        }
    }

    private void ClearLock()
    {
        if (currentTargetHealth)
            currentTargetHealth.OnDeath -= HandleTargetDeath;

        CurrentTarget = null;
        CurrentTargetStatic = null;
        currentTargetHealth = null;
        OnLockChanged?.Invoke(null);
    }

    // ---------------------------------------------------------------------
    private Transform FindNearestTarget()
    {
        Collider[] cols = Physics.OverlapSphere(
            transform.position, lockRadius, enemyMask,
            QueryTriggerInteraction.Collide);

        Transform best = null;
        float bestSqr = float.MaxValue;

        foreach (var col in cols)
        {
            var hc = col.GetComponent<HealthComponent>();
            if (hc == null || !hc.IsAlive) continue;

            float sq = (col.transform.position - transform.position).sqrMagnitude;
            if (sq < bestSqr)
            {
                best = col.transform;
                bestSqr = sq;
            }
        }
        return best;
    }

    private void CycleTarget(bool clockwise)
    {
        Collider[] cols = Physics.OverlapSphere(
            transform.position, lockRadius, enemyMask,
            QueryTriggerInteraction.Collide);

        List<Transform> list = cols.Select(c => c.transform).ToList();
        list = list.Where(t =>
        {
            var hc = t.GetComponent<HealthComponent>();
            return hc != null && hc.IsAlive;
        }).ToList();
        if (list.Count < 2) return;

        // Order by signed angle around player (clockwise list)
        list = list.OrderBy(t => Vector3.SignedAngle(
                    transform.forward,
                    t.position - transform.position,
                    Vector3.up)).ToList();

        int idx = list.IndexOf(CurrentTarget);
        idx = (idx + (clockwise ? 1 : -1) + list.Count) % list.Count;
        CurrentTarget = list[idx];
        CurrentTargetStatic = CurrentTarget;
        OnLockChanged?.Invoke(CurrentTarget);
    }

    private bool IsTargetValid(Transform t)
    {
        if (!t) return false;
        var hc = t.GetComponent<HealthComponent>();
        if (hc == null || !hc.IsAlive) return false;
        float sqr = (t.position - transform.position).sqrMagnitude;
        return sqr <= lockRadius * lockRadius;
    }

    private void HandleTargetDeath(HealthComponent _)
    {
        ClearLock();
    }
}