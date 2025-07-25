using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PatrolPointSpawnerManager : MonoBehaviour
{
    [Header("Patrol Settings")]
    public int numberOfRoutes = 3;
    public int pointsPerRoute = 4;
    public float safeZoneRadius = 15f;
    public float minDistanceBetweenPoints = 10f;
    public GameObject patrolPointPrefab; // Optional: simple marker

    [Header("References")]
    public Transform playerSpawn;

    [Header("Spawn Area Settings")]
    public Vector3 patrolAreaCenter = Vector3.zero;
    public float patrolAreaRadius = 50f;

    [Header("Generated Routes")]
    public List<List<Transform>> patrolRoutes = new List<List<Transform>>();

    private void Start()
    {
        //GeneratePatrolRoutes(); // HACK! Causing problem with floor generation (need floor to finish before this). This necessitates calling manually. Will need to add in more complex solution later
    }

    public void GeneratePatrolRoutes()
    {
        if (playerSpawn == null)
        {
            Debug.LogError("[PatrolSpawner] Player spawn reference is missing!");
            return;
        }

        for (int r = 0; r < numberOfRoutes; r++)
        {
            List<Transform> routePoints = new List<Transform>();

            int attempts = 0;

            while (routePoints.Count < pointsPerRoute && attempts < 500)
            {
                attempts++;

                Vector3 randomPoint = RandomNavMeshLocation(patrolAreaRadius);
                float distanceToPlayer = Vector3.Distance(randomPoint, playerSpawn.position);

                if (distanceToPlayer < safeZoneRadius)
                    continue;

                bool tooCloseToOtherPoints = false;

                foreach (var otherRoute in patrolRoutes)
                {
                    foreach (var p in otherRoute)
                    {
                        if (Vector3.Distance(randomPoint, p.position) < minDistanceBetweenPoints)
                        {
                            tooCloseToOtherPoints = true;
                            break;
                        }
                    }
                    if (tooCloseToOtherPoints) break;
                }

                if (!tooCloseToOtherPoints)
                {
                    GameObject pointObj = patrolPointPrefab != null
                        ? Instantiate(patrolPointPrefab, randomPoint, Quaternion.identity)
                        : new GameObject("PatrolPoint");

                    pointObj.transform.position = randomPoint;
                    pointObj.transform.parent = this.transform;

                    routePoints.Add(pointObj.transform);

                    Debug.Log($"[PatrolSpawner] Spawned patrol point at {randomPoint}");
                }
            }

            if (routePoints.Count > 0)
            {
                patrolRoutes.Add(routePoints);
            }
        }

        Debug.Log($"[PatrolSpawner] Generated {patrolRoutes.Count} patrol routes total.");
    }

    private Vector3 RandomNavMeshLocation(float radius)
    {
        Vector3 randomDirection = Random.insideUnitSphere * radius;
        randomDirection.y = 0f; // Keep horizontal

        randomDirection += patrolAreaCenter; // Use configurable spawn center

        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, radius, NavMesh.AllAreas))
        {
            return hit.position;
        }
        else
        {
            return patrolAreaCenter;
        }
    }
}