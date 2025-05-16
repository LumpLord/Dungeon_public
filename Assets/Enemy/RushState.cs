using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(fileName = "NewRushState", menuName = "AI/Combat States/Rush")]
public class RushState : EnemyCombatStateBase
{
    public float rushSpeed = 6f;
    public float rushAttackRange = 2f;
    public float minDuration = .5f;

    public override IEnumerator Execute(EnemyCombatController controller)
    {
        var agent = controller.GetAgent();
        var target = controller.GetTarget();
        var weapon = controller.GetWeaponController();

        if (controller == null || agent == null || target == null || !agent.enabled || !agent.isOnNavMesh)
        yield break;

        agent.speed = rushSpeed;

        while (Vector3.Distance(controller.transform.position, target.position) > rushAttackRange)
        {
            if (!controller.PlayerInCombatVision()) yield break;
            if (!agent.enabled || !agent.isOnNavMesh) yield break;


            agent.SetDestination(target.position);
            controller.FaceTargetSmooth();
            yield return null;
        }

        agent.ResetPath();
        controller.FaceTargetSmooth();
        yield return new WaitForSeconds(0.1f);

        weapon?.PerformAttack();
        yield return new WaitForSeconds(0.5f);

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
        return distance >= 7f && distance <= 10f;
    }
}