using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewAttack", menuName = "Combat/AttackAsset")]
public class AttackAsset : ScriptableObject
{
    public string attackName = "New Attack";

    [Tooltip("Each phase in order from start to finish")]
    public List<AttackPhase> phases = new List<AttackPhase>();

    [System.Serializable]
    public struct ComboMapping
    {
        public string inputName; // e.g., "Mouse0", "Mouse3"
        public AttackAsset nextAttack;
    }

    [Tooltip("Mapping of input names to follow-up combo attacks")]
    public List<ComboMapping> comboMap = new List<ComboMapping>();
}