using UnityEngine;
using System;

public class TinyHealthComponent : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
        Debug.Log($"[SAFE AWAKE] {gameObject.name} now has {currentHealth}/{maxHealth} HP.");
    }
}