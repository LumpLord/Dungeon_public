using UnityEngine;

public enum DamageType { Physical, Fire, Ice, Poison, Magic }

public enum WeaponGripType
{
    RightHand,
    LeftHand,
    TwoHand,
    Back,
    SideRight,
    SideLeft
}

[CreateAssetMenu(menuName = "Weapons/Weapon Data")]
public class WeaponData : ScriptableObject
{
    public string weaponName;
    public WeaponGripType gripType = WeaponGripType.RightHand;
    public float baseDamage = 10f;
    public DamageType damageType = DamageType.Physical;

    // New Offset Fields
    public Vector3 gripPositionOffset = Vector3.zero;
    public Vector3 gripRotationOffset = Vector3.zero;
}
