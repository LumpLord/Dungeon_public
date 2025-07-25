using System.Collections;      // for IEnumerator
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Combat state executed when an enemy is damaged but
/// cannot yet rush/attack because the player is too far or out of LOS.
/// Enemy runs to the impact point, waits a few seconds, then exits.
/// </summary>
[CreateAssetMenu(menuName = "Combat States/Investigate State")]
public class InvestigateState : EnemyCombatStateBase
{
    [Header("Investigate‑tuning")]
    [Tooltip("Movement speed while investigating.")]
    public float moveSpeed     = 4f;

    [Tooltip("Distance at which we consider we have reached the investigate point.")]
    public float arrivalRadius = 1.5f;

    [Tooltip("Seconds to wait / search once we arrive.")]
    public float searchDuration = 3f;

    private float _timer;

    public override bool CanExecute(EnemyCombatController controller) => true;

    public override void EnterState(EnemyCombatController controller)
    {
        base.EnterState(controller);

        _timer = 0f;

        NavMeshAgent agent = controller.GetAgent();
        agent.speed = moveSpeed;
        agent.SetDestination(controller.GetInvestigatePoint());
    }

    public override IEnumerator Execute(EnemyCombatController controller)
    {
        NavMeshAgent agent = controller.GetAgent();

        while (true)
        {
            // arrived?
            if (!agent.pathPending && agent.remainingDistance <= arrivalRadius)
                _timer += Time.deltaTime;

            // regain sight of player?
            if (controller.PlayerInCombatVision())
            {
                ExitState(controller);
                yield break;  // end state early
            }

            // finished searching
            if (_timer >= searchDuration)
            {
                ExitState(controller);
                yield break;
            }

            yield return null; // wait a frame
        }
    }

    public override void ExitState(EnemyCombatController controller)
    {
        base.ExitState(controller);
    }
}