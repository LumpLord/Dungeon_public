using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(fileName = "NewAttackState", menuName = "AI/Combat States/Attack")]
public class AttackState : EnemyCombatStateBase
{
    [Header("Execution Range")]
    public float minAllowedDistance = 1f;
    public float maxAllowedDistance = 7f;

    [Header("Approach Speed")]
    public float minApproachSpeed = 2f;
    public float maxApproachSpeed = 4f;

    public float minCooldown = 1f;
    public float maxCooldown = 2f;
    public float desiredAttackRange = 1.5f;
    public float minDuration = .5f;

    public override void EnterState(EnemyCombatController controller)
    {
        Debug.Log($"{controller.name} is entering {this.GetType().Name}");
    }

    public override IEnumerator Execute(EnemyCombatController controller)
    {
        Debug.Log($"{controller.name} is executing {this.GetType().Name}");

        var agent = controller.GetAgent();
        agent.speed = Random.Range(minApproachSpeed, maxApproachSpeed);
        var weapon = controller.GetWeaponController();
        var target = controller.GetTarget();

        if (agent == null || !agent.enabled || !agent.isOnNavMesh || target == null || controller == null || controller.gameObject == null || !controller.gameObject.activeInHierarchy)
            yield break;

        float elapsed = 0f;
        float waitBeforeAttack = Random.Range(minCooldown, maxCooldown);
        float startTime = Time.time;

        // Approach target
        while (Vector3.Distance(controller.transform.position, target.position) > desiredAttackRange)
        {
            if (!controller.PlayerInCombatVision()) yield break;

            if (agent.enabled)
                agent.SetDestination(target.position);

            yield return null;
        }

        if (agent.enabled)
            agent.ResetPath();

        controller.FaceTargetLocked();

        // Wait before attack if needed
        while (Time.time < startTime + waitBeforeAttack)
        {
            yield return null;
        }

        weapon?.PerformAttack();

        while (elapsed < minDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    public override void ExitState(EnemyCombatController controller)
    {
        Debug.Log($"{controller.name} is exiting {this.GetType().Name}");
    }

    public override bool CanExecute(EnemyCombatController controller)
    {
        if (controller == null || controller.GetTarget() == null || controller.gameObject == null || !controller.gameObject.activeInHierarchy)
            return false;

        float distance = Vector3.Distance(controller.transform.position, controller.GetTarget().position);
        return distance >= minAllowedDistance && distance <= maxAllowedDistance;
    }
}
