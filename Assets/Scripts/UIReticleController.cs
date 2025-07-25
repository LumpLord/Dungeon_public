using UnityEngine;

public class UIReticleController : MonoBehaviour
{
    public GameObject reticle;
    public PlayerController player;

    void Update()
    {
        if (reticle != null && player != null)
        {
            bool aiming = player.isAiming;
            reticle.SetActive(aiming);
            //Debug.Log("Reticle Active: " + aiming + " | Object setActive: " + reticle.activeSelf);
        }
    }
}