using UnityEngine;
using System.Collections;

public class PlayerCombatController : MonoBehaviour
{
    private Rigidbody rb;
    [Header("References")]
    public EquippedWeaponController weaponController;

    // Assign the ProjectileLauncher component (wand muzzle) in Inspector
    [Header("Ranged")]
    [SerializeField] private ProjectileLauncher projectileLauncher;

    [Header("Attack Settings")]
    public float attackCooldown = 0.8f;
    private float attackTimer = 0f;
    private bool isAttacking = false;

    private int currentComboIndex = 0;
    private bool isAiming = false;

    private void Update()
    {
        // Update aim state
        isAiming = Input.GetMouseButton(1);   // RMB hold to aim

        // Fire projectile while aiming
        if (isAiming && Input.GetMouseButtonDown(0) && projectileLauncher != null)
        {
            projectileLauncher.Fire();
            return;    // skip melee attack handling this frame
        }

        attackTimer += Time.deltaTime;

        if (!isAiming && Input.GetMouseButtonDown(0))
        {
            if (isAttacking)
            {
                weaponController?.QueueComboInput();
            }
            else
            {
                TryPrimaryAttack();
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            TryContextAction();
        }

        if (Input.GetKeyDown(KeyCode.Mouse4))
        {
            TryParry();
        }

        if (Input.GetKeyDown(KeyCode.Mouse3))
        {
            TryHeavyAttack();
        }
    }

    private void TryPrimaryAttack()
    {
        if (weaponController == null || !weaponController.CanAttack())
            return;

        if (attackTimer >= attackCooldown && !isAttacking)
        {
            // ensure starting fresh
            weaponController.ClearQueuedCombo();
            currentComboIndex = 0;

            attackTimer = 0f;
            StartCoroutine(ComboAttackRoutine());
        }
    }

    private IEnumerator ComboAttackRoutine()
    {
        isAttacking = true;

        // reset any stale combo input at the start
        weaponController.ClearQueuedCombo();

        // clamp combo index to available attacks
        if (!weaponController.HasAttackAtIndex(currentComboIndex))
        {
            currentComboIndex = 0;
        }

        Debug.Log($"[Combo DEBUG] Performing attack index={currentComboIndex}");
        weaponController.PerformAttack(currentComboIndex);

        yield return new WaitForSeconds(attackCooldown);

        Debug.Log($"[Combo DEBUG] queued before branch = {weaponController.ComboWasQueued()}");
        if (weaponController.ComboWasQueued() && weaponController.HasAttackAtIndex(currentComboIndex + 1))
        {
            weaponController.ClearQueuedCombo();
            Debug.Log($"[Combo DEBUG] queued after clear = {weaponController.ComboWasQueued()}");
            currentComboIndex++;
            isAttacking = false;
            Debug.Log($"[Combo DEBUG] isAttacking set to false at comboIndex={currentComboIndex}");
            yield break;
        }

        weaponController.ClearQueuedCombo();
        Debug.Log($"[Combo DEBUG] queued after clear = {weaponController.ComboWasQueued()}");
        currentComboIndex = 0;
        weaponController.ReturnToGuardPose();
        isAttacking = false;
        Debug.Log($"[Combo DEBUG] isAttacking set to false at comboIndex={currentComboIndex}");
    }

    private void TryContextAction()
    {
        Debug.Log("[Combat] Context Action Triggered (Block / Aim depending on weapon type)");
    }

    private void TryParry()
    {
        Debug.Log("[Combat] Parry Attempted (Mouse4)");
    }

    private void TryHeavyAttack()
    {
        Debug.Log("[Combat] Heavy Attack Attempted (Mouse3)");
        if (weaponController == null || !weaponController.CanAttack())
            return;

        weaponController.PerformHeavyAttack();
        weaponController.ClearQueuedCombo();
        currentComboIndex = 0;
    }
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }
    }
}