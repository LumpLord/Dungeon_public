using UnityEngine;
using UnityEngine.AI;
// no additional using required if DamageType is in global namespace

public class HealthComponent : MonoBehaviour, IDamageable
{
    public bool useRagdoll = false;
    public Rigidbody[] ragdollBodies; // Assign via inspector

    public float maxHealth = 100f;
    public float currentHealth;

    // --------------------------------------------------------------------
    // Event to notify when this character takes damage
    public delegate void DamagedHandler(float amount, DamageType type, GameObject source, Vector3 hitPoint);
    public event DamagedHandler OnDamaged;

    private bool isDead = false;
    public bool IsDead() => isDead;

    public bool IsAlive => !isDead;

    /// <summary>
    /// Fired exactly once when Die() is executed.
    /// </summary>
    public event System.Action<HealthComponent> OnDeath;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;  // Already dead

        currentHealth -= amount;

        // Notify listeners (source unknown in this overload)
        OnDamaged?.Invoke(amount, default(DamageType), null, transform.position);

        var roamer = GetComponent<EnemyRoamer>();
        if (roamer != null)
        {
            roamer.ReceiveDamage(amount);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // --------------------------------------------------------------------
    // IDamageable implementation (calls the existing numeric method)
    public void TakeDamage(float amount, DamageType type, GameObject source)
    {
        if (isDead) return;  // Already dead

        currentHealth -= amount;

        // Notify listeners with context
        OnDamaged?.Invoke(amount, type, source, transform.position);

        var roamer = GetComponent<EnemyRoamer>();
        if (roamer != null)
        {
            roamer.ReceiveDamage(amount);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Overload that includes hitPoint for future use (e.g., investigate behaviour).
    /// Currently forwards to the main 3‑parameter version.
    /// </summary>
    public void TakeDamage(float amount, DamageType type, GameObject source, Vector3 hitPoint)
    {
        // Invoke directly to avoid double‑decrement of health
        if (isDead) return;

        currentHealth -= amount;
        OnDamaged?.Invoke(amount, type, source, hitPoint);

        var roamer = GetComponent<EnemyRoamer>();
        if (roamer != null)
        {
            roamer.ReceiveDamage(amount);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        var agent = GetComponent<NavMeshAgent>();
        if (agent != null) agent.enabled = false;

        var roamer = GetComponent<EnemyRoamer>();
        if (roamer != null) roamer.enabled = false;

        if (useRagdoll)
        {
            EnableRagdoll();
        }
        else
        {
            var animator = GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetTrigger("Die");
            }
        }

        // Broadcast death once
        OnDeath?.Invoke(this);
        Destroy(gameObject, 5f);
    }

    void EnableRagdoll()
    {
        foreach (var body in ragdollBodies)
        {
            body.isKinematic = false;
            body.useGravity = true;
        }

        var mainCollider = GetComponent<Collider>();
        if (mainCollider != null) mainCollider.enabled = false;
    }
}