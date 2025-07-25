using UnityEngine;
using UnityEditor;

/// <summary>
/// WeaponPoseEditorAuto Usage Guide:
/// 
/// 1. Enter Play Mode with your weapon-equipped character selected.
/// 2. Open the editor via Tools > Weapon Pose Editor (Auto Assign).
/// 3. The tool attempts to auto-assign references from the selected character.
///    - "Weapon Controller" should point to the EquippedWeaponController.
///    - "Visual Model" should be the weapon mesh object to animate.
///    - "Reference Point" should be the WeaponRoot, which is animated during attacks.
/// 4. Assign the correct AttackAsset to edit its phases.
/// 5. Select a phase from the dropdown list.
/// 
/// Step-by-step Workflow:
/// a) With play mode active and the weapon in its rest/default pose,
///    click "Set Current Pose as Base".
/// b) Move and rotate the WeaponRoot in the scene view to define this phase’s pose.
/// c) Click "Apply Current Offset to Phase" to store the difference from base.
/// d) You can "Preview Phase Offset" to view changes and "Reset to Base Pose" to revert.
///
/// Notes:
/// - Do NOT move or rotate the Visual Model directly—only the WeaponRoot.
/// - Ensure all attack phases are properly named to appear in the dropdown.
/// </summary>
public class WeaponPoseEditorAuto : EditorWindow
{
    public EquippedWeaponController weaponController;
    public Transform visualModel;
    public Transform referencePoint;
    public AttackAsset attackAsset;

    private int selectedPhaseIndex = 0;
    private Vector3 basePosition;
    private Vector3 baseRotation;
    private string[] phaseNames = new string[0];

    [MenuItem("Tools/Weapon Pose Editor (Auto Assign)")]
    public static void ShowWindow()
    {
        GetWindow<WeaponPoseEditorAuto>("Weapon Pose Editor (Auto)");
    }

    private void OnEnable()
    {
        // Attempt auto assignment if in context
        if (Selection.activeGameObject != null)
        {
            weaponController = Selection.activeGameObject.GetComponentInChildren<EquippedWeaponController>();
            if (weaponController != null)
            {
                referencePoint = weaponController.transform;
                visualModel = weaponController.visualModel;
            }
        }
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
                    Debug.Log("[WeaponPoseEditorAuto] Base pose set.");
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
                    Debug.Log("[WeaponPoseEditorAuto] Pose reset to base.");
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
        Debug.Log($"[WeaponPoseEditorAuto] Previewed phase '{phase.phaseName}'");
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
        Debug.Log($"[WeaponPoseEditorAuto] Updated phase '{phase.phaseName}' with current offset.");
    }
}