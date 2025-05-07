using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewAttack", menuName = "Combat/AttackAsset")]
public class AttackAsset : ScriptableObject
{
    public string attackName = "New Attack";

    [Tooltip("Each phase in order from start to finish")]
    public List<AttackPhase> phases = new List<AttackPhase>();

    [Tooltip("Combo attacks that can follow this one")]
    public List<AttackAsset> comboAttacks = new List<AttackAsset>();
}