using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class WeaponPoseEditor : EditorWindow
{
    [Header("Assignments")]
    public EquippedWeaponController weaponController;
    public Transform visualModel;
    public Transform referencePoint;
    public AttackAsset attackAsset;

    private int selectedPhaseIndex = 0;
    private Vector3 basePosition;
    private Vector3 baseRotation;
    private string[] phaseNames = new string[0];

    [MenuItem("Tools/Weapon Pose Editor")]
    public static void ShowWindow()
    {
        GetWindow<WeaponPoseEditor>("Weapon Pose Editor");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Weapon Pose Editor", EditorStyles.boldLabel);
        weaponController = (EquippedWeaponController)EditorGUILayout.ObjectField("Weapon Controller", weaponController, typeof(EquippedWeaponController), true);
        visualModel = (Transform)EditorGUILayout.ObjectField("Visual Model", visualModel, typeof(Transform), true);
        referencePoint = (Transform)EditorGUILayout.ObjectField("Reference Point (WeaponRoot)", referencePoint, typeof(Transform), true);
        attackAsset = (AttackAsset)EditorGUILayout.ObjectField("Attack Asset", attackAsset, typeof(AttackAsset), false);

        EditorGUILayout.Space();

        if (attackAsset != null && attackAsset.phases.Count > 0)
        {
            phaseNames = attackAsset.phases.ConvertAll(p => p.phaseName).ToArray();
            selectedPhaseIndex = EditorGUILayout.Popup("Selected Phase", selectedPhaseIndex, phaseNames);

            EditorGUILayout.Space();

            if (GUILayout.Button("Set Current Pose as Base"))
            {
                if (visualModel != null)
                {
                    basePosition = visualModel.localPosition;
                    baseRotation = visualModel.localEulerAngles;
                    Debug.Log("[WeaponPoseEditor] Base pose set.");
                }
            }

            if (GUILayout.Button("Preview Phase Offset"))
            {
                ApplyPhaseOffsetPreview();
            }

            if (GUILayout.Button("Reset to Base Pose"))
            {
                if (visualModel != null)
                {
                    visualModel.localPosition = basePosition;
                    visualModel.localEulerAngles = baseRotation;
                    Debug.Log("[WeaponPoseEditor] Pose reset to base.");
                }
            }

            if (GUILayout.Button("Apply Current Offset to Phase"))
            {
                ApplyCurrentOffsetToPhase();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Assign an AttackAsset with at least one AttackPhase.", MessageType.Info);
        }
    }

    private void ApplyPhaseOffsetPreview()
    {
        if (visualModel == null || referencePoint == null || attackAsset == null || attackAsset.phases.Count == 0)
            return;

        var phase = attackAsset.phases[selectedPhaseIndex];
        visualModel.localPosition = basePosition + phase.positionOffset;
        visualModel.localEulerAngles = baseRotation + phase.rotationOffset;
        Debug.Log($"[WeaponPoseEditor] Previewed phase '{phase.phaseName}'");
    }

    private void ApplyCurrentOffsetToPhase()
    {
        if (visualModel == null || referencePoint == null || attackAsset == null || attackAsset.phases.Count == 0)
            return;

        Vector3 posOffset = visualModel.localPosition - basePosition;
        Vector3 rotOffset = visualModel.localEulerAngles - baseRotation;

        var phase = attackAsset.phases[selectedPhaseIndex];
        phase.positionOffset = posOffset;
        phase.rotationOffset = rotOffset;

        EditorUtility.SetDirty(phase);
        AssetDatabase.SaveAssets();
        Debug.Log($"[WeaponPoseEditor] Updated phase '{phase.phaseName}' with current offset.");
    }
}