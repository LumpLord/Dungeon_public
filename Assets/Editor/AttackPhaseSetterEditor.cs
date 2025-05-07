using UnityEngine;
using UnityEditor;
using System.IO;

public class AttackPhaseSetterEditor : EditorWindow
{
    private Transform referenceTransform;
    private string attackPhaseName = "NewAttackPhase";

    [MenuItem("Tools/Attack Phase Setter")]
    public static void ShowWindow()
    {
        GetWindow<AttackPhaseSetterEditor>("Attack Phase Setter");
    }

    private void OnGUI()
    {
        GUILayout.Label("Assign Offsets to AttackPhase", EditorStyles.boldLabel);
        referenceTransform = (Transform)EditorGUILayout.ObjectField("Reference Transform", referenceTransform, typeof(Transform), true);
        attackPhaseName = EditorGUILayout.TextField("AttackPhase File Name", attackPhaseName);

        if (GUILayout.Button("Apply Offsets to AttackPhase"))
        {
            ApplyOffsets();
        }
    }

    private void ApplyOffsets()
    {
        if (referenceTransform == null)
        {
            Debug.LogWarning("Reference Transform not assigned.");
            return;
        }

        string path = $"Assets/Combat/AttackPhases/{attackPhaseName}.asset";
        var attackPhase = AssetDatabase.LoadAssetAtPath<AttackPhase>(path);

        if (attackPhase == null)
        {
            Debug.LogError($"AttackPhase not found at path: {path}");
            return;
        }

        // Apply local offsets
        attackPhase.positionOffset = referenceTransform.localPosition;
        attackPhase.rotationOffset = referenceTransform.localEulerAngles;

        EditorUtility.SetDirty(attackPhase);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"AttackPhase '{attackPhaseName}' updated successfully.");
    }
}