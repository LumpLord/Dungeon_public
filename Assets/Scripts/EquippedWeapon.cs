using UnityEngine;

public class EquippedWeapon : MonoBehaviour
{
    public WeaponData weaponData;

    public float GetDamage()
    {
        return weaponData != null ? weaponData.baseDamage : 0f;
    }

    public DamageType GetDamageType()
    {
        return weaponData != null ? weaponData.damageType : DamageType.Physical;
    }

    // New: Provide access to grip offsets
    public Vector3 GetGripPositionOffset()
    {
        return weaponData != null ? weaponData.gripPositionOffset : Vector3.zero;
    }

    public Vector3 GetGripRotationOffset()
    {
        return weaponData != null ? weaponData.gripRotationOffset : Vector3.zero;
    }
}