using UnityEngine;

public class PlayerPickupController : MonoBehaviour
{
    [Header("Pickup Settings")]
    public WeaponManager weaponManager;
    public float pickupRadius = 2f;
    public LayerMask weaponLayer;

    [Header("UI Prompt")]
    public PickupPromptUI pickupPromptUI;

    private void Update()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, pickupRadius, weaponLayer);

        WeaponPickup nearest = null;
        float closestDist = float.MaxValue;

        foreach (Collider hit in hits)
        {
            WeaponPickup wp = hit.GetComponent<WeaponPickup>();
            if (wp != null)
            {
                float dist = Vector3.Distance(transform.position, wp.transform.position);
                if (dist < closestDist)
                {
                    nearest = wp;
                    closestDist = dist;
                }
            }
        }

        // Update UI prompt
        if (pickupPromptUI != null)
        {
            if (nearest != null)
            {
                pickupPromptUI.player = nearest.transform;
                pickupPromptUI.SetVisible(true);
            }
            else
            {
                pickupPromptUI.SetVisible(false);
            }
        }

        // Handle pickup input
        if (nearest != null && Input.GetKeyDown(KeyCode.E))
        {
            weaponManager.EquipWeaponInstance(nearest.gameObject);
        }
    }
}