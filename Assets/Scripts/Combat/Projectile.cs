using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float baseDamage = 25f;
    [SerializeField] private DamageType dmgType = DamageType.Fire;
    [SerializeField] private float maxRange = 60f;
    [SerializeField] private GameObject hitVfx;
    /// <summary>
    /// Reference to the GameObject that fired / owns this projectile.
    /// Set by the launcher so damage events can identify the attacker.
    /// </summary>
    [HideInInspector] public GameObject shooter;
    Vector3 spawnPos;

    void Start() => spawnPos = transform.position;

    void Update()
    {
        if ((transform.position - spawnPos).sqrMagnitude >
            maxRange * maxRange)
            Destroy(gameObject);
    }

    void OnCollisionEnter(Collision col)
    {
        if (col.collider.TryGetComponent<IDamageable>(out var d))
        {
            var src = shooter != null ? shooter : gameObject;
            Vector3 hitPoint = col.GetContact(0).point;
            d.TakeDamage(baseDamage, dmgType, src, hitPoint);
        }

        if (hitVfx) Instantiate(hitVfx, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}
