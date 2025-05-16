using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(fileName = "NewStalkState", menuName = "AI/Combat States/Stalk")]
public class StalkState : EnemyCombatStateBase
{
    public float circleRadius = 3.5f;
    public float stalkMoveSpeed = 2f;
    public float minStalkTime = 2f;
    public float maxStalkTime = 4f;
    public float orbitSpeed = 1f;
    public float minDuration = .5f;

    public override IEnumerator Execute(EnemyCombatController controller)
    {
        var agent = controller.GetAgent();
        var target = controller.GetTarget();
        if (controller == null || agent == null || target == null || !agent.enabled || !agent.isOnNavMesh)
        yield break;

        float duration = Random.Range(minStalkTime, maxStalkTime);
        float timer = 0f;

        bool clockwise = Random.value > 0.5f;
        float direction = clockwise ? 1f : -1f;

        Vector3 toEnemy = controller.transform.position - target.position;
        float angle = Mathf.Atan2(toEnemy.z, toEnemy.x);

        agent.speed = stalkMoveSpeed;

        while (timer < duration)
        {
            if (!controller.PlayerInCombatVision()) yield break;
            if (!agent.enabled || !agent.isOnNavMesh) yield break;

            timer += Time.deltaTime;
            angle += direction * orbitSpeed * Time.deltaTime;

            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * circleRadius;
            Vector3 desiredPosition = target.position + offset;

            if (NavMesh.SamplePosition(desiredPosition, out NavMeshHit hit, 1f, NavMesh.AllAreas))
                agent.SetDestination(hit.position);

            controller.FaceTargetSmooth();
            yield return null;
        }

        agent.ResetPath();

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
        return distance >= 7f && distance <= 12f;
    }
}