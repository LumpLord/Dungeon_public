using UnityEngine;
using UnityEngine.UI;

public class HealthBarController : MonoBehaviour
{
    public Slider slider;
    public HealthComponent health;

    private void Update()
    {
        if (health != null && slider != null)
        {
            slider.value = health.currentHealth / health.maxHealth;
        }
    }
}