using UnityEngine;
using System.Collections;

public class PlayerCombatController : MonoBehaviour
{
    private Rigidbody rb;
    [Header("References")]
    public EquippedWeaponController weaponController;

    [Header("Attack Settings")]
    public float attackCooldown = 0.8f;
    private float attackTimer = 0f;
    private bool isAttacking = false;

    private int currentComboIndex = 0;

    private void Update()
    {
        attackTimer += Time.deltaTime;

        if (Input.GetMouseButtonDown(0))
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

        if (Input.GetKeyDown(KeyCode.Mouse5))
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
            attackTimer = 0f;
            StartCoroutine(ComboAttackRoutine());
        }
    }

    private IEnumerator ComboAttackRoutine()
    {
        isAttacking = true;

        weaponController.PerformAttack(currentComboIndex);

        yield return new WaitForSeconds(attackCooldown);

        if (weaponController.ComboWasQueued() && weaponController.HasAttackAtIndex(currentComboIndex + 1))
        {
            currentComboIndex++;
            isAttacking = false;
            yield break;
        }

        currentComboIndex = 0;
        weaponController.ReturnToGuardPose();
        isAttacking = false;
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
        Debug.Log("[Combat] Heavy Attack Attempted (Mouse5)");
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