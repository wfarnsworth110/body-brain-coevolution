using UnityEngine;

public class MainManager : MonoBehaviour
{
    [Header("Simulation Speed")]
    [Tooltip("Set to 1 for normal viewing, 10-20 for fast training")]
    public float trainingTimeScale = 10.0f;

    void Awake()
    {
        // Ensure 100Hz physics frequency for consistent simulation
        Time.fixedDeltaTime = 0.01f;
        Time.maximumDeltaTime = 0.1f;

        // Accelerate the simulation speed
        Time.timeScale = trainingTimeScale;
    }
}
