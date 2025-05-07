using UnityEngine;
using UnityEngine.UI;

public class PickupPromptUI : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public Transform player;
    public float verticalOffset = 2.0f;

    void LateUpdate()
    {
        if (player == null) return;

        // Follow and face camera
        transform.position = player.position + Vector3.up * verticalOffset;
        transform.forward = Camera.main.transform.forward;
    }

    public void SetVisible(bool visible)
    {
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.blocksRaycasts = visible;
    }
}