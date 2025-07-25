using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class WeaponManager : MonoBehaviour
{
    [Header("Socket Map")]
    public Transform RightHandSocket;
    public Transform LeftHandSocket;
    public Transform TwoHandSocket;

    [Header("Default Equip")]
    public GameObject defaultWeaponPrefab;

    [Header("Runtime")]
    public EquippedWeaponController currentWeapon;

    private void Start()
    {
        if (defaultWeaponPrefab != null && currentWeapon == null)
        {
            GameObject runtimeWeapon = Instantiate(defaultWeaponPrefab);
            EquipWeaponInstance(runtimeWeapon);
        }
    }

    
    public void EquipWeaponInstance(GameObject weaponObject)
    {
#if UNITY_EDITOR
        if (currentWeapon != null && !PrefabUtility.IsPartOfPrefabAsset(currentWeapon.gameObject))
#else
        if (currentWeapon != null)
#endif
        {
            Vector3 tossDirection = -transform.forward + Vector3.up * 0.3f;
            currentWeapon.Drop(tossDirection.normalized * 8f);
            currentWeapon = null;
        }

        currentWeapon = weaponObject.GetComponent<EquippedWeaponController>();
        EquippedWeapon equipped = weaponObject.GetComponent<EquippedWeapon>();

        if (currentWeapon == null || equipped == null)
        {
            Debug.LogWarning("Weapon is missing required components.");
            return;
        }

        Transform targetSocket = GetSocketForGripType(equipped.weaponData.gripType) ?? RightHandSocket;
        currentWeapon.Equip(targetSocket);

        WeaponPickup pickup = weaponObject.GetComponent<WeaponPickup>();
        if (pickup != null)
        {
            Destroy(pickup);
        }

        weaponObject.layer = LayerMask.NameToLayer("Default");
    }

    private Transform GetSocketForGripType(WeaponGripType gripType)
    {
        return gripType switch
        {
            WeaponGripType.RightHand => RightHandSocket,
            WeaponGripType.LeftHand => LeftHandSocket,
            WeaponGripType.TwoHand => TwoHandSocket,
            _ => RightHandSocket
        };
    }

    public void ThrowWeapon(float force = 10f)
    {
        if (currentWeapon != null)
        {
            Vector3 throwDir = transform.forward + Vector3.up * 0.3f;
            currentWeapon.Drop(throwDir.normalized * force);
            currentWeapon = null;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            ThrowWeapon();
        }
    }
}