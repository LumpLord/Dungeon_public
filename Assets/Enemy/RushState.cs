using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[CreateAssetMenu(menuName = "Enemy/Combat States/Rush State")]
public class RushState : EnemyCombatStateBase
{
    public float rushSpeed = 12f;
    public float maxRushDuration = 1.2f;
    public float minStartDistance = 2f;
    public float maxStartDistance = 15f;
    //[SerializeField] private float rushStoppingDistance = 1.2f;
    public float minTimeBeforeNextState = 0.75f;

    private float startTime;
    private float endTime;
    private bool isRushing;

    public override void EnterState(EnemyCombatController controller)
    {
        Debug.Log($"{controller.name} is entering {this.GetType().Name} with maxRushDuration={maxRushDuration}");

        startTime = Time.time;
        endTime = startTime + maxRushDuration + minTimeBeforeNextState;
        isRushing = true;

        var agent = controller.GetAgent();
        if (agent != null)
            agent.isStopped = true;
    }

    public override IEnumerator Execute(EnemyCombatController controller)
    {
        Debug.Log($"{controller.name} is executing {this.GetType().Name}");
        Debug.Log($"{controller.name} RushState: checking Rigidbody and NavMeshAgent state...");

        var target = controller.GetTarget();
        var rb = controller.GetComponent<Rigidbody>();
        var agent = controller.GetAgent();

        if (rb == null || rb.isKinematic)
        {
            Debug.LogWarning($"{controller.name} RushState exiting early due to invalid component references or conditions.");
            Debug.LogWarning($"{controller.name} has kinematic Rigidbody during RushState. Using NavMesh fallback.");
            isRushing = false;
            yield return new WaitForSeconds(minTimeBeforeNextState);
            yield break;
        }

        if (agent == null || !agent.enabled || !agent.isOnNavMesh || target == null || controller == null || controller.gameObject == null || !controller.gameObject.activeInHierarchy)
        {
            Debug.LogWarning($"{controller.name} RushState exiting early due to invalid component references or conditions.");
            isRushing = false;
            yield break;
        }

        controller.FaceTargetSmooth();

        while (Time.time - startTime < maxRushDuration)
        {
            if (target == null || controller == null) break;

            Vector3 currentDirection = (target.position - controller.transform.position).normalized;
            currentDirection.y = 0;

            rb.linearVelocity = currentDirection * rushSpeed;
            controller.FaceTargetSmooth();

            // Optional: insert check to exit early if within striking range
            // if (Vector3.Distance(controller.transform.position, target.position) < hitDistance)
            //     break;

            yield return null;
        }

        Debug.Log($"{controller.name} RushState movement complete, cleaning up.");
        rb.linearVelocity = Vector3.zero;
        isRushing = false;

        if (agent != null)
            agent.isStopped = false;
    }

    public override void ExitState(EnemyCombatController controller)
    {
        Debug.Log($"{controller.name} is exiting {this.GetType().Name}");

        isRushing = false;
        var rb = controller.GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity = Vector3.zero;

        var agent = controller.GetAgent();
        if (agent != null) agent.isStopped = false;
    }

    public override bool CanExit(EnemyCombatController controller)
    {
        Debug.Log($"{controller.name} CanExit RushState? isRushing={isRushing} time={Time.time} vs endTime={endTime}");
        if (!isRushing) return true;
        return Time.time >= endTime;
    }

    public override bool CanQueueNextState(EnemyCombatController controller)
    {
        Debug.Log($"{controller.name} CanQueueNext RushState? isRushing={isRushing} time={Time.time} vs endTime={endTime}");
        return !isRushing && Time.time >= endTime;
    }

    public override bool CanExecute(EnemyCombatController controller)
    {
        var target = controller.GetTarget();
        if (target == null) return false;

        float distance = Vector3.Distance(controller.transform.position, target.position);
        return distance >= minStartDistance && distance <= maxStartDistance;
    }
}