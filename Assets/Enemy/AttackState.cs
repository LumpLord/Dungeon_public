using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(fileName = "NewAttackState", menuName = "AI/Combat States/Attack")]
public class AttackState : EnemyCombatStateBase
{
    public float minCooldown = 1f;
    public float maxCooldown = 2f;
    public float approachDistance = 1.5f;
    public float minDuration = .5f;

    public override IEnumerator Execute(EnemyCombatController controller)
    {
        var agent = controller.GetAgent();
        var weapon = controller.GetWeaponController();
        var target = controller.GetTarget();

        if (controller == null || agent == null || target == null || !agent.enabled || !agent.isOnNavMesh)
        yield break;

        while (Vector3.Distance(controller.transform.position, target.position) > approachDistance)
        {
            if (!controller.PlayerInCombatVision()) yield break;
            if (!agent.enabled || !agent.isOnNavMesh) yield break;

            agent.SetDestination(target.position);
            controller.FaceTargetSmooth();
            yield return null;
        }

        agent.ResetPath();
        controller.FaceTargetSmooth();

        weapon?.PerformAttack();
        yield return new WaitForSeconds(Random.Range(minCooldown, maxCooldown));

        float elapsed = 0f;
        while (elapsed < minDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    public override bool CanExecute(EnemyCombatController controller)
    {
        float distance = Vector3.Distance(controller.transform.position, controller.GetTarget().position);
        return distance > 1f && distance <= 7f;
    }
}