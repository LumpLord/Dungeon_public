// using UnityEngine;
// using UnityEditor;

// public class WeaponPoseEditor : EditorWindow
// {
//     public EquippedWeaponController weaponController;
//     public AttackAsset attackAsset;
//     public int selectedPhaseIndex;

//     private Vector3 basePosition;
//     private Vector3 baseRotation;

//     [MenuItem("Tools/Weapon Pose Editor")]
//     public static void ShowWindow()
//     {
//         GetWindow<WeaponPoseEditor>("Weapon Pose Editor");
//     }

//     void OnGUI()
//     {
//         EditorGUILayout.LabelField("Setup", EditorStyles.boldLabel);
//         weaponController = (EquippedWeaponController)EditorGUILayout.ObjectField("Weapon Controller", weaponController, typeof(EquippedWeaponController), true);
//         attackAsset = (AttackAsset)EditorGUILayout.ObjectField("Attack Asset", attackAsset, typeof(AttackAsset), false);

//         if (attackAsset != null && attackAsset.phases.Count > 0)
//         {
//             string[] phaseNames = attackAsset.phases.ConvertAll(p => p.phaseName).ToArray();
//             selectedPhaseIndex = EditorGUILayout.Popup("Attack Phase", selectedPhaseIndex, phaseNames);
//         }
//         else
//         {
//             EditorGUILayout.HelpBox("Assign an AttackAsset with at least one phase.", MessageType.Info);
//         }

//         if (weaponController == null || weaponController.visualModel == null)
//         {
//             EditorGUILayout.HelpBox("Assign a WeaponController with a valid VisualModel.", MessageType.Warning);
//             return;
//         }

//         EditorGUILayout.Space(10);
//         EditorGUILayout.LabelField("Base Pose Controls", EditorStyles.boldLabel);

//         if (GUILayout.Button("Set Current Pose as Base"))
//         {
//             basePosition = weaponController.visualModel.localPosition;
//             baseRotation = weaponController.visualModel.localEulerAngles;
//             Debug.Log("[PoseEditor] Base Pose Set.");
//         }

//         if (GUILayout.Button("Log Offset from Base"))
//         {
//             Vector3 posOffset = weaponController.visualModel.localPosition - basePosition;
//             Vector3 rotOffset = weaponController.visualModel.localEulerAngles - baseRotation;

//             Debug.Log($"[Offset] Position Offset: {posOffset}");
//             Debug.Log($"[Offset] Rotation Offset: {rotOffset}");
//         }

//         if (GUILayout.Button("Copy Offset to Clipboard"))
//         {
//             Vector3 posOffset = weaponController.visualModel.localPosition - basePosition;
//             Vector3 rotOffset = weaponController.visualModel.localEulerAngles - baseRotation;

//             EditorGUIUtility.systemCopyBuffer = $"Position: new Vector3({posOffset.x:F3}f, {posOffset.y:F3}f, {posOffset.z:F3}f), " +
//                                                 $"Rotation: new Vector3({rotOffset.x:F3}f, {rotOffset.y:F3}f, {rotOffset.z:F3}f)";
//             Debug.Log("[PoseEditor] Offset copied to clipboard.");
//         }

//         EditorGUILayout.Space(10);
//         EditorGUILayout.LabelField("Apply to Phase", EditorStyles.boldLabel);

//         if (attackAsset != null && attackAsset.phases.Count > selectedPhaseIndex)
//         {
//             if (GUILayout.Button("Apply Current Offsets to Selected Phase"))
//             {
//                 Vector3 posOffset = weaponController.visualModel.localPosition - basePosition;
//                 Vector3 rotOffset = weaponController.visualModel.localEulerAngles - baseRotation;

//                 AttackPhase phase = attackAsset.phases[selectedPhaseIndex];
//                 phase.positionOffset = posOffset;
//                 phase.rotationOffset = rotOffset;
//                 EditorUtility.SetDirty(phase);
//                 AssetDatabase.SaveAssets();
//                 Debug.Log($"[PoseEditor] Saved offsets to phase: {phase.phaseName}");
//             }

//             if (GUILayout.Button("Preview Selected Phase Pose"))
//             {
//                 AttackPhase phase = attackAsset.phases[selectedPhaseIndex];
//                 weaponController.visualModel.localPosition = basePosition + phase.positionOffset;
//                 weaponController.visualModel.localEulerAngles = baseRotation + phase.rotationOffset;
//                 Debug.Log($"[PoseEditor] Previewed phase: {phase.phaseName}");
//             }

//             if (GUILayout.Button("Reset Weapon to Base Pose"))
//             {
//                 weaponController.visualModel.localPosition = basePosition;
//                 weaponController.visualModel.localEulerAngles = baseRotation;
//                 Debug.Log("[PoseEditor] Reset to base pose.");
//             }
//         }
//     }
// } 