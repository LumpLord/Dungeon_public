using UnityEngine;

[CreateAssetMenu(menuName = "Player/Movement Stats")]
public class PlayerMovementStats : ScriptableObject
{
    [Header("Movement Speeds")]
    public float moveSpeed = 5f;
    public float sprintMultiplier = 2f;
    public float aimSpeedMultiplier = 0.5f;

    [Header("Dashing")]
    public float dashDistance = 4.5f;      // How far to dash
    public float dashTime = 0.2f;          // How long the dash lasts
    public float dashCooldown = 0.75f;     // Cooldown between dashes
}