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
    public float rushStopDistance = 1.5f;

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
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.updatePosition = false;
            agent.updateRotation = false;
        }
    }

    public override IEnumerator Execute(EnemyCombatController controller)
    {
        Debug.Log($"{controller.name} is executing {this.GetType().Name}");
        Debug.Log($"{controller.name} RushState: checking Rigidbody and NavMeshAgent state...");

        var target = controller.GetTarget();
        var rb = controller.GetComponent<Rigidbody>();
        var agent = controller.GetAgent();

        if (rb == null)
        {
            Debug.LogWarning($"{controller.name} RushState aborted: Rigidbody is null");
            isRushing = false;
            yield return new WaitForSeconds(minTimeBeforeNextState);
            yield break;
        }
        else if (rb.isKinematic)
        {
            Debug.LogWarning($"{controller.name} RushState aborted: Rigidbody is kinematic");
            isRushing = false;
            yield return new WaitForSeconds(minTimeBeforeNextState);
            yield break;
        }
        else if (agent == null)
        {
            Debug.LogWarning($"{controller.name} RushState aborted: NavMeshAgent is null");
            isRushing = false;
            yield return new WaitForSeconds(minTimeBeforeNextState);
            yield break;
        }
        else if (!agent.enabled)
        {
            Debug.LogWarning($"{controller.name} RushState aborted: NavMeshAgent is disabled.");
            isRushing = false;
            yield return new WaitForSeconds(minTimeBeforeNextState);
            yield break;
        }
        else if (!agent.isOnNavMesh)
        {
            Debug.LogWarning($"{controller.name} RushState aborted: NavMeshAgent not on NavMesh.");
            isRushing = false;
            yield return new WaitForSeconds(minTimeBeforeNextState);
            yield break;
        }
        else if (controller == null || controller.gameObject == null || !controller.gameObject.activeInHierarchy)
        {
            Debug.LogWarning($"{controller.name} RushState aborted: Controller GameObject is inactive or null");
            isRushing = false;
            yield return new WaitForSeconds(minTimeBeforeNextState);
            yield break;
        }

        while (Time.time - startTime < maxRushDuration)
        {
            if (target == null || controller == null) break;

            controller.FaceTargetSmooth();

            Vector3 currentDirection = (target.position - controller.transform.position).normalized;
            currentDirection.y = 0;

            Vector3 newVelocity = currentDirection * rushSpeed;
            if ((rb.linearVelocity - newVelocity).sqrMagnitude > 0.01f)
            {
                Debug.Log($"{controller.name} RushState velocity set to: {newVelocity}");
            }
            rb.linearVelocity = newVelocity;

            float distanceToTarget = Vector3.Distance(controller.transform.position, target.position);
            if (distanceToTarget <= rushStopDistance)
            {
                Debug.Log($"{controller.name} RushState hit range reached. Initiating attack.");
                rb.linearVelocity = Vector3.zero;
                controller.FaceTargetSmooth();

                var weaponController = controller.GetComponentInChildren<EquippedWeaponController>();
                if (weaponController != null)
                {
                    Debug.Log($"{controller.name} RushState triggering weapon attack.");
                    weaponController.PerformAttack();
                }
                else
                {
                    Debug.LogWarning($"{controller.name} RushState could not find EquippedWeaponController.");
                }

                break;
            }

            yield return null;
        }

        Debug.Log($"{controller.name} RushState movement complete, cleaning up.");
        rb.linearVelocity = Vector3.zero;
        isRushing = false;

        if (agent != null && agent.isOnNavMesh)
            agent.isStopped = false;
    }

    public override void ExitState(EnemyCombatController controller)
    {
        Debug.Log($"{controller.name} is exiting {this.GetType().Name}");

        isRushing = false;
        var rb = controller.GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity = Vector3.zero;

        var agent = controller.GetAgent();
        if (agent != null && agent.isOnNavMesh)
        {
            agent.updatePosition = true;
            agent.updateRotation = true;
            agent.isStopped = false;
        }
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