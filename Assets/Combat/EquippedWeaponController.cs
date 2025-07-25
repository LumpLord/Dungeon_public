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
    [SerializeField] private AttackAsset heavyAttackAsset;

    private Coroutine attackCoroutine;
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    private bool isComboing = false;
    private GameObject lastHitObject;
    private AttackAsset queuedAttack = null;
    private bool comboReadyToFire = false;

    private AnimationCurve activeDamageCurve;
    private float currentPhaseProgress = 0f;

    // Stores the weapon's default opener attack so we can reliably reset after combos
    private AttackAsset initialAttack;

    public float GetCurrentDamageMultiplier()
    {
        return activeDamageCurve != null ? activeDamageCurve.Evaluate(currentPhaseProgress) : 1f;
    }

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (equippedWeapon == null) equippedWeapon = GetComponent<EquippedWeapon>();

        // Capture the starting attack asset for reset logic and debugging
        initialAttack = currentAttack;
        Debug.Log($"[EWC] Awake() – initialAttack set to '{initialAttack?.name}'");

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
        // No longer used; kept for compatibility if called elsewhere
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
        Debug.Log($"[EWC] PerformAttack() called – currentAttack='{currentAttack?.name}', queuedAttack='{queuedAttack?.name}'");

        if (attackCoroutine != null)
            StopCoroutine(attackCoroutine);

        Debug.Log($"[EWC] Starting PlayAttackRoutine with asset '{currentAttack?.name}'");

        AttackAsset attackToPlay = currentAttack;

        // Combo index override is no longer used

        attackCoroutine = StartCoroutine(PlayAttackRoutine(attackToPlay));
    }

    private IEnumerator PlayAttackRoutine(AttackAsset attackAsset)
    {
        AttackAsset activeAttack = attackAsset;
        Debug.Log($"[EWC] >>> Enter PlayAttackRoutine – activeAttack='{activeAttack?.name}'");

        if (attackAsset == null || attackAsset.phases == null || visualModel == null)
            yield break;

        isComboing = true;
        lastHitObject = null;
        queuedAttack = null;
        comboReadyToFire = false;

        foreach (var phase in attackAsset.phases)
        {
            comboReadyToFire = false;

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

                if (comboReadyToFire && queuedAttack == null && currentAttack.comboMap != null)
                {
                    foreach (var mapping in currentAttack.comboMap)
                    {
                        // Debug.Log($"Checking combo mapping: inputName='{mapping.inputName}'");
                        if (Input.GetButtonDown(mapping.inputName))
                        {
                            queuedAttack = mapping.nextAttack;
                            break;
                        }
                    }
                }

                yield return null;
            }

            if (phase.enableDamageDuringPhase)
            {
                DisableDamage();
                activeDamageCurve = null;
            }

            comboReadyToFire = false;

            if (phase.endCurrentAttackOnCombo && queuedAttack != null)
            {
                Debug.Log("[Combo] Ending current attack early to start combo");
                break;
            }
        }

        isComboing = false;
        attackCoroutine = null;

        if (queuedAttack != null)
        {
            Debug.Log($"[EWC] Queued combo detected – switching currentAttack to '{queuedAttack.name}'");
            Debug.Log("[Combo] Triggering queued attack: " + queuedAttack.name);
            currentAttack = queuedAttack;
            queuedAttack = null;
            PerformAttack();
        }
        else
        {
            Debug.Log("[EWC] No queued combo – resetting currentAttack to default opener");
            currentAttack = initialAttack;
            queuedAttack = null;
        }
    }


    public bool ComboWasQueued() => queuedAttack != null;

    /// <summary>
    /// Clears any pending combo so that the next PerformAttack starts fresh.
    /// </summary>
    public void ClearQueuedCombo()
    {
        queuedAttack = null;
        currentAttack = initialAttack;   // reset to default starter
        Debug.Log($"[EWC] ClearQueuedCombo() – combo cleared, currentAttack reset to '{initialAttack?.name}'");
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
    public void PerformHeavyAttack()
    {
        if (heavyAttackAsset == null) return;

        if (attackCoroutine != null)
            StopCoroutine(attackCoroutine);

        attackCoroutine = StartCoroutine(PlayAttackRoutine(heavyAttackAsset));
    }

    public bool HasAttackAtIndex(int index)
    {
        // This can be modified if index-based attack lookup is implemented
        return currentAttack != null;
    }
}