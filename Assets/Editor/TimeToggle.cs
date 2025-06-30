using UnityEngine;

public class TimeScaleDebugger : MonoBehaviour
{
    public KeyCode toggleKey = KeyCode.T;
    public float slowTimeScale = 0.25f;
    public float normalTimeScale = 1f;

    private bool isSlowed = false;

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            isSlowed = !isSlowed;
            Time.timeScale = isSlowed ? slowTimeScale : normalTimeScale;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            Debug.Log($"Time scale set to: {Time.timeScale}");
        }
    }
}