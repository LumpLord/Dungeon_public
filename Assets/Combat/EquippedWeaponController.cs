using UnityEngine;
using System.Collections;

public class EquippedWeaponController : MonoBehaviour
{
    [Header("References")]
    public EquippedWeapon equippedWeapon;
    public Collider weaponCollider;
    public Rigidbody rb;
    public Transform visualModel;

    [Header("Attack Asset")]
    public AttackAsset currentAttack;

    private Coroutine attackCoroutine;
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    private bool isComboing = false;
    private GameObject lastHitObject;
    private bool comboQueued = false;
    private bool comboReadyToFire = false;

    private AnimationCurve activeDamageCurve;
    private float currentPhaseProgress = 0f;

    public float GetCurrentDamageMultiplier()
    {
        return activeDamageCurve != null ? activeDamageCurve.Evaluate(currentPhaseProgress) : 1f;
    }

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (equippedWeapon == null) equippedWeapon = GetComponent<EquippedWeapon>();

        if (visualModel != null)
        {
            initialPosition = visualModel.localPosition;
            initialRotation = visualModel.localRotation;
        }

        if (weaponCollider != null)
            weaponCollider.enabled = false;
    }

    public bool CanAttack()
    {
        return !isComboing && currentAttack != null;
    }

    public void QueueComboInput()
    {
        comboQueued = true;
    }

    public void Equip(Transform weaponSocket)
    {
        transform.SetParent(weaponSocket);

        transform.localPosition = equippedWeapon?.GetGripPositionOffset() ?? Vector3.zero;
        transform.localRotation = Quaternion.Euler(equippedWeapon?.GetGripRotationOffset() ?? Vector3.zero);
        transform.localScale = Vector3.one;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        if (weaponCollider != null)
            weaponCollider.enabled = false;
    }

    public void Drop(Vector3 force)
    {
        transform.SetParent(null);

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.AddForce(force, ForceMode.Impulse);
        }

        if (weaponCollider != null)
            weaponCollider.enabled = true;

        if (GetComponent<WeaponPickup>() == null)
            gameObject.AddComponent<WeaponPickup>();

        gameObject.layer = LayerMask.NameToLayer("Pickup");
    }

    public void PerformAttack(int comboIndexOverride = -1)
    {
        if (attackCoroutine != null)
            StopCoroutine(attackCoroutine);

        AttackAsset attackToPlay = currentAttack;

        if (comboIndexOverride > 0 &&
            currentAttack.comboAttacks != null &&
            comboIndexOverride - 1 < currentAttack.comboAttacks.Count)
        {
            attackToPlay = currentAttack.comboAttacks[comboIndexOverride - 1];
        }

        attackCoroutine = StartCoroutine(PlayAttackRoutine(attackToPlay));
    }

    private IEnumerator PlayAttackRoutine(AttackAsset attackAsset)
    {
        
        if (attackAsset == null || attackAsset.phases == null || visualModel == null)
            yield break;

        isComboing = true;
        lastHitObject = null;
        comboQueued = false;
        comboReadyToFire = false;

        foreach (var phase in attackAsset.phases)
        {
            float elapsed = 0f;
            Vector3 startPos = visualModel.localPosition;
            Quaternion startRot = visualModel.localRotation;

            Vector3 endPos = initialPosition + phase.positionOffset;
            Quaternion endRot = Quaternion.Euler(initialRotation.eulerAngles + phase.rotationOffset);

            if (phase.enableDamageDuringPhase)
            {
                EnableDamage();
                activeDamageCurve = phase.damageCurve;
            }

            if (phase.allowComboQueue)
                comboReadyToFire = true;

            while (elapsed < phase.duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / phase.duration);
                float curveT = phase.interpolationCurve.Evaluate(t);

                currentPhaseProgress = t;

                visualModel.localPosition = Vector3.Lerp(startPos, endPos, curveT);
                visualModel.localRotation = Quaternion.Slerp(startRot, endRot, curveT);

                if (comboReadyToFire && Input.GetMouseButtonDown(0))
                {
                    QueueComboInput();
                }

                yield return null;
            }
            
            if (phase.enableDamageDuringPhase)
            {
                DisableDamage();
                activeDamageCurve = null;
            }

            comboReadyToFire = false;

            
            if (phase.endCurrentAttackOnCombo && comboQueued)
            {
                Debug.Log("[Combo] Ending current attack early to start combo");
                break;
            }
        }

        isComboing = false;
        attackCoroutine = null;

        // Automatically trigger next combo if queued
        if (comboQueued && currentAttack != null)
        {
            int nextIndex = currentAttack.comboAttacks.IndexOf(attackAsset) + 1;

            if (nextIndex >= 0 && nextIndex < currentAttack.comboAttacks.Count)
            {
                Debug.Log("[Combo] Auto-triggering next combo attack...");
                PerformAttack(nextIndex + 1); // +1 for offset (index 0 = base)
            }
        }
    }


    public bool ComboWasQueued() => comboQueued;

    public bool HasAttackAtIndex(int index)
    {
        return currentAttack != null &&
               currentAttack.comboAttacks != null &&
               index > 0 &&
               index - 1 < currentAttack.comboAttacks.Count;
    }

    public void ReturnToGuardPose()
    {
        if (visualModel != null)
        {
            visualModel.localPosition = initialPosition;
            visualModel.localRotation = initialRotation;
        }
    }

    public bool CurrentAttackHitObject() => lastHitObject != null;

    public void RegisterHit(GameObject hit)
    {
        lastHitObject = hit;
    }

    public void EnableDamage()
    {
        if (weaponCollider != null)
        {
            weaponCollider.enabled = true;
            // Debug.Log("[Weapon] Damage collider enabled");
        }
    }

    public void DisableDamage()
    {
        if (weaponCollider != null)
        {
            weaponCollider.enabled = false;
            // Debug.Log("[Weapon] Damage collider disabled");
        }
    }
}