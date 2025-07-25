// Basic script for baking NavMeshSurface. May come in handy for autobaking new areas player moves to or procedural generation. Will require more work in either case
using UnityEngine;
using UnityEngine.AI; // Required for NavMeshSurface
using Unity.AI.Navigation; //Required to find NavMesh

[RequireComponent(typeof(NavMeshSurface))]
public class NavMeshAutoBaker : MonoBehaviour
{
    void Start()
    {
        // Automatically bake NavMesh at runtime (optional)
        GetComponent<NavMeshSurface>().BuildNavMesh();
    }

    // Optional: Editor-only bake button
#if UNITY_EDITOR
    [ContextMenu("Bake NavMesh Now")]
    private void BakeNavMeshNow()
    {
        GetComponent<NavMeshSurface>().BuildNavMesh();
    }
#endif
}