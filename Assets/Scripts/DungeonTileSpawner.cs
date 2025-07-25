using UnityEngine;
using Unity.AI.Navigation;
using UnityEngine.AI;

public class DungeonTileSpawner : MonoBehaviour
{
    [Header("Tile Settings")]
    public GameObject tilePrefab;
    public int width = 10;
    public int height = 10;
    public float tileSpacing = 10.0f;

    [Header("Randomization")]
    [Range(0f, 1f)]
    public float tileSpawnChance = 0.9f;

    [Header("NavMesh Baking")]
    public NavMeshSurface navMeshSurface;

    [Header("Enemy Settings")]
    public GameObject enemyPrefab;
    public int enemyCount = 5;

    [Header("Patrol Points")]
    public bool spawnPatrolPoints = true;

    private void Start()
    {
        GenerateTiles();
        BakeNavMesh();
        if (spawnPatrolPoints)
            SpawnPatrolPoints();
        SpawnEnemies();
    }

    void GenerateTiles()
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                if (Random.value <= tileSpawnChance)
                {
                    Vector3 position = new Vector3(x * tileSpacing, 0f, z * tileSpacing);
                    Instantiate(tilePrefab, position, Quaternion.identity, transform);
                }
            }
        }
    }

    void BakeNavMesh()
    {
        if (navMeshSurface != null)
        {
            navMeshSurface.BuildNavMesh();
        }
        else
        {
            Debug.LogWarning("NavMeshSurface not assigned in DungeonTileSpawner.");
        }
    }

    void SpawnPatrolPoints()
    {
        var patrolManager = FindFirstObjectByType<PatrolPointSpawnerManager>();

        if (patrolManager != null)
        {
            patrolManager.GeneratePatrolRoutes();
            Debug.Log("[DungeonTileSpawner] Patrol Points spawned after floor generation!");
        }
        else
        {
            Debug.LogWarning("[DungeonTileSpawner] No PatrolPointSpawnerManager found in scene.");
        }
    }

    void SpawnEnemies()
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning("Enemy prefab not assigned in DungeonTileSpawner.");
            return;
        }

        var patrolManager = FindFirstObjectByType<PatrolPointSpawnerManager>();

        for (int i = 0; i < enemyCount; i++)
        {
            Vector3 randomPos = new Vector3(
                Random.Range(0, width) * tileSpacing,
                0,
                Random.Range(0, height) * tileSpacing
            );

            if (NavMesh.SamplePosition(randomPos, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                GameObject enemyObj = Instantiate(enemyPrefab, hit.position, Quaternion.identity);

                var roamer = enemyObj.GetComponent<EnemyRoamer>();
                if (roamer != null && patrolManager != null && patrolManager.patrolRoutes.Count > 0)
                {
                    var randomRoute = patrolManager.patrolRoutes[Random.Range(0, patrolManager.patrolRoutes.Count)];
                    roamer.patrolRoute = randomRoute;
                }
            }
        }
    }
}