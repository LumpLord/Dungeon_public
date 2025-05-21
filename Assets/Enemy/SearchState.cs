using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(fileName = "NewSearchState", menuName = "AI/Combat States/Search")]
public class SearchState : EnemyCombatStateBase
{
    [Header("Search Parameters")]
    public float rotateDuration = 3f;
    public float relocateRadius = 3f;
    public float relocateDuration = 1.5f;

    public override void EnterState(EnemyCombatController controller)
    {
        Debug.Log($"{controller.name} is entering {this.GetType().Name}");
    }

    public override IEnumerator Execute(EnemyCombatController controller)
    {
        Debug.Log($"{controller.name} is executing {this.GetType().Name}");
        NavMeshAgent agent = controller.GetAgent();
        Transform target = controller.GetTarget();

        // Phase 1: Rotate in place
        float rotateTimer = 0f;
        while (rotateTimer < rotateDuration)
        {
            controller.transform.Rotate(Vector3.up, 120f * Time.deltaTime);
            rotateTimer += Time.deltaTime;

            if (controller.PlayerInCombatVision())
            {
                Debug.Log($"{controller.name} reacquired player during rotate scan.");
                controller.EngageCombat();
                yield break;
            }

            yield return null;
        }

        // Phase 2: Relocate to a nearby point
        Vector3 randomOffset = Random.insideUnitSphere * relocateRadius;
        randomOffset.y = 0;
        Vector3 targetPos = controller.GetLastKnownPlayerPosition() + randomOffset;

        if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, relocateRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }

        float relocateTimer = 0f;
        while (relocateTimer < relocateDuration)
        {
            relocateTimer += Time.deltaTime;

            if (controller.PlayerInCombatVision())
            {
                Debug.Log($"{controller.name} reacquired player during relocate.");
                controller.EngageCombat();
                yield break;
            }

            yield return null;
        }

        // Phase 3: Still no player found
        if (!controller.PlayerInCombatVision())
        {
            Debug.Log($"{controller.name} failed to locate player. Disengaging.");
            controller.DisengageCombat();
        }

        yield return new WaitForSeconds(1f);
    }

    public override void ExitState(EnemyCombatController controller)
    {
        Debug.Log($"{controller.name} is exiting {this.GetType().Name}");
    }
}