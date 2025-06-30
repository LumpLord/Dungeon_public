using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// TODO: Investigate and fix post-pursuit vertical 'bob' where the enemy briefly sinks into the ground
// Hypothesis: This may be caused by timing or sync issues between NavMeshAgent.nextPosition,
// controller.transform.position, and physics re-engagement after Warp/Lerp

[CreateAssetMenu(menuName = "AI/Combat States/Pursuit States")]
public class PursuitState : EnemyCombatStateBase
{
    public float maxPursuitTime = 3f; 
    public float pursuitSpeed = 9f;
    public float exitDistance = 8f;

    public override IEnumerator Execute(EnemyCombatController controller)
    {
        float timer = 0f;

        var agent = controller.GetAgent();

        if (agent != null)
        {
            agent.enabled = true;
            agent.height = 2f;
            agent.baseOffset = 0.9f;
            NavMeshHit hit;
            if (!NavMesh.SamplePosition(controller.transform.position, out hit, 2.0f, NavMesh.AllAreas))
            {
                Debug.LogWarning($"{controller.name} could not find nearby NavMesh. Aborting Pursuit.");
                yield break;
            }

            Vector3 startPosition = controller.transform.position;
            Vector3 navMeshPosition = hit.position;
            // float elapsed = 0f;
            // float lerpDuration = 0.2f;

            // while (elapsed < lerpDuration)
            // {
            //     controller.transform.position = Vector3.Lerp(startPosition, navMeshPosition, elapsed / lerpDuration);
            //     elapsed += Time.deltaTime;
            //     yield return null;
            // }
            // controller.transform.position = navMeshPosition;

            Debug.Log($"{controller.name} warped to navmesh position. Start ΔY: {navMeshPosition.y - startPosition.y}");
            agent.Warp(navMeshPosition);
            controller.transform.position = agent.nextPosition;
            agent.updatePosition = true;
            agent.updateRotation = true;

            agent.autoBraking = false;
            agent.speed = pursuitSpeed;
            agent.isStopped = false;
        }

        Vector3 lastPosition = controller.transform.position;
        float stuckTimer = 0f;

        while (timer < maxPursuitTime)
        {
            timer += Time.deltaTime;

            Transform target = controller.GetTarget(); // dynamically refetch
            if (target == null) break;

            Vector3 destination = target.position;
            agent.SetDestination(destination);

            float distanceToPlayer = Vector3.Distance(controller.transform.position, destination);
            bool pathIsReachable = agent.pathStatus == NavMeshPathStatus.PathComplete;

            if (distanceToPlayer <= exitDistance && pathIsReachable)
            {
                break;
            }

            // Stuck detection (not moving enough)
            float movedDistance = Vector3.Distance(controller.transform.position, lastPosition);
            if (movedDistance < 0.05f)
            {
                stuckTimer += Time.deltaTime;
                if (stuckTimer > 0.75f)
                {
                    Debug.LogWarning($"{controller.name} is stuck. Exiting Pursuit.");
                    break;
                }
            }
            else
            {
                stuckTimer = 0f;
            }

            lastPosition = controller.transform.position;

            yield return null;
        }

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
            controller.transform.position = agent.nextPosition;
        }

        controller.EnqueueRandomBehaviorState(); // Choose next behavior from weights
        yield return new WaitForSeconds(0.1f); // buffer before next state
    }

    public override string GetStateName() => "PursuitState";
}
