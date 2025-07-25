using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Locks the cursor whenever the game is actively playing and unlocks it
/// whenever the Unity Editor is paused, the application loses focus, or
/// OnApplicationPause is raised (mobile suspend, etc.).
/// </summary>
[DefaultExecutionOrder(-500)]    // run before gameplay scripts
public class CursorLockHelper : MonoBehaviour
{
    private void Start()
    {
        LockCursor(true);
#if UNITY_EDITOR
        // Unlock when the developer clicks the blue pause button in the editor.
        EditorApplication.pauseStateChanged += OnPauseStateChanged;
#endif
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        EditorApplication.pauseStateChanged -= OnPauseStateChanged;
#endif
    }

    // ALT-TAB or OS focus loss → unlock so user can reach other apps
    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
            TryRelock();
        else
            LockCursor(false);
    }

    // Mobile / standalone app pause events
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            LockCursor(false);
        else
            TryRelock();
    }

#if UNITY_EDITOR
    private void OnPauseStateChanged(PauseState state)
    {
        if (state == PauseState.Paused)
            LockCursor(false);
        else if (state == PauseState.Unpaused)
            TryRelock();
    }
#endif

    // ------------------------------------------------------------------------

    private static void TryRelock()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying || EditorApplication.isPaused)
            return;
#endif
        LockCursor(true);
    }

    public static void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible   = !locked;
    }
}