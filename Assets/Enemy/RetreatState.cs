using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(fileName = "NewRetreatState", menuName = "AI/Combat States/Retreat")]
public class RetreatState : EnemyCombatStateBase
{
    public float retreatSpeed = 4f;
    public float retreatDistance = 3f;
    public float retreatDuration = 1f;
    public float minDuration = .5f;

    public override IEnumerator Execute(EnemyCombatController controller)
    {
        var agent = controller.GetAgent();
        var target = controller.GetTarget();
        if (controller == null || agent == null || target == null || !agent.enabled || !agent.isOnNavMesh)
        yield break;

        Vector3 direction = (controller.transform.position - target.position).normalized;
        Vector3 destination = controller.transform.position + direction * retreatDistance;

        if (NavMesh.SamplePosition(destination, out NavMeshHit hit, retreatDistance, NavMesh.AllAreas))
        {
            agent.speed = retreatSpeed;
            agent.SetDestination(hit.position);
        }

        float elapsed = 0f;
        while (elapsed < retreatDuration)
        {
            if (!controller.PlayerInCombatVision()) yield break;
            if (!agent.enabled || !agent.isOnNavMesh) yield break;


            elapsed += Time.deltaTime;
            controller.FaceTargetSmooth();
            yield return null;
        }

        agent.ResetPath();

        while (elapsed < minDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    public override bool CanExecute(EnemyCombatController controller)
    {
        float distance = Vector3.Distance(controller.transform.position, controller.GetTarget().position);
        return distance <= 4f;
    }
} 