using UnityEngine;

/// <summary>
/// Example usage of the ParticleManager system.
/// This script demonstrates how to programmatically control particle reloading.
/// You don't need this script for the UI to work - it's just for reference.
/// </summary>
public class ParticleManagerExample : MonoBehaviour
{
    // Example: Reload particles when a specific key is pressed
    void Update()
    {
        // Example 1: Simple reload with current count
        if (Input.GetKeyDown(KeyCode.F5))
        {
            if (ParticleManager.Instance != null)
            {
                ParticleManager.Instance.ReloadParticles();
                Debug.Log("Reloaded particles with current count");
            }
        }

        // Example 2: Reload with a specific count
        if (Input.GetKeyDown(KeyCode.F6))
        {
            if (ParticleManager.Instance != null)
            {
                ParticleManager.Instance.ReloadParticles(1000);
                Debug.Log("Reloaded particles with 1000 particles");
            }
        }

        // Example 3: Just update the count without reloading
        if (Input.GetKeyDown(KeyCode.F7))
        {
            if (ParticleManager.Instance != null)
            {
                ParticleManager.Instance.SetParticleCount(500);
                Debug.Log("Set particle count to 500 (will be used on next reload)");
            }
        }

        // Example 4: Toggle reload on play
        if (Input.GetKeyDown(KeyCode.F8))
        {
            if (ParticleManager.Instance != null)
            {
                ParticleManager.Instance.ReloadOnPlay = !ParticleManager.Instance.ReloadOnPlay;
                Debug.Log($"Reload on play: {ParticleManager.Instance.ReloadOnPlay}");
            }
        }

        // Example 5: Get current settings
        if (Input.GetKeyDown(KeyCode.F9))
        {
            if (ParticleManager.Instance != null)
            {
                Debug.Log($"Min: {ParticleManager.Instance.MinParticleCount}");
                Debug.Log($"Max: {ParticleManager.Instance.MaxParticleCount}");
                Debug.Log($"Current: {ParticleManager.Instance.CurrentParticleCount}");
                Debug.Log($"Step: {ParticleManager.Instance.ParticleCountStep}");
                Debug.Log($"Reload on Play: {ParticleManager.Instance.ReloadOnPlay}");
            }
        }
    }
}
