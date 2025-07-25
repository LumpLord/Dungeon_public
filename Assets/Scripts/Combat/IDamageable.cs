using UnityEngine;

/// <summary>
/// Implement on any GameObject that can receive damage.
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// Apply damage and provide minimal context.
    /// </summary>
    /// <param name="amount">Raw damage before resistances.</param>
    /// <param name="type">DamageType (e.g., Magic, Slash).</param>
    /// <param name="source">Projectile, weapon, or attacker GameObject.</param>
    void TakeDamage(float amount, DamageType type, GameObject source);

    /// <summary>
    /// Apply damage and include hit point for behaviours that need spatial context (e.g., investigate).
    /// </summary>
    /// <param name="amount">Raw damage before resistances.</param>
    /// <param name="type">DamageType (e.g., Magic, Slash).</param>
    /// <param name="source">Projectile, weapon, or attacker GameObject.</param>
    /// <param name="hitPoint">World-space point where the damage occurred.</param>
    void TakeDamage(float amount, DamageType type, GameObject source, Vector3 hitPoint);
}
