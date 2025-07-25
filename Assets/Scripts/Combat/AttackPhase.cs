using UnityEngine;

[CreateAssetMenu(fileName = "NewAttackPhase", menuName = "Combat/AttackPhases")]
public class AttackPhase : ScriptableObject
{
    [Header("Damage Over Time (0 = start, 1 = end)")]
    public AnimationCurve damageCurve = AnimationCurve.Constant(0, 1, 1f);
    public string phaseName = "Phase";
    
    public float duration = 0.4f;

    [Header("Offset From Base Pose")]
    public Vector3 positionOffset;
    public Vector3 rotationOffset;

    [Header("Damage Settings")]
    public bool enableDamageDuringPhase = false;

    [Header("Optional Interpolation")]
    public AnimationCurve interpolationCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("Combo Settings")]
    public bool allowComboQueue = false;           // Should this phase accept combo inputs?
    public bool endCurrentAttackOnCombo = false;   // If true, end attack early when combo input is queued
}