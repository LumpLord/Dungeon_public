using UnityEngine;

public class DamageSource : MonoBehaviour
{
    [Header("Damage Settings")]
    public float baseDamage = 25f;

    private EquippedWeaponController weaponController;

    private void Awake()
    {
        weaponController = GetComponentInParent<EquippedWeaponController>();

        if (weaponController == null)
        {
            Debug.LogWarning($"[DamageSource] No EquippedWeaponController found in parent of {name}");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer != LayerMask.NameToLayer("Enemy")) return;

        if (other.TryGetComponent(out HealthComponent health))
        {
            float finalDamage = baseDamage;

            if (weaponController != null)
            {
                finalDamage *= weaponController.GetCurrentDamageMultiplier();
                weaponController.RegisterHit(other.gameObject);
            }

            health.TakeDamage(finalDamage);
        }
    }
}