using UnityEngine;
using UnityEngine.AI;

public class EnemyAIController : MonoBehaviour
{
    public float roamRadius = 5f;
    public float roamDelay = 2f;

    private NavMeshAgent agent;
    private float roamTimer;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        roamTimer = roamDelay;
    }

    void Update()
    {
        roamTimer += Time.deltaTime;

        if (agent.isOnNavMesh && roamTimer >= roamDelay && agent.remainingDistance <= agent.stoppingDistance)
        {
            Vector3 randomDirection = Random.insideUnitSphere * roamRadius;
            randomDirection += transform.position;

            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, roamRadius, NavMesh.AllAreas))
            {
                // Debug.Log($"{gameObject.name} roaming to {hit.position}");
                agent.SetDestination(hit.position);
            }
            else
            {
                Debug.LogWarning($"{gameObject.name} failed to find a valid roam target.");
            }

            roamTimer = 0f;
        }
    }
}