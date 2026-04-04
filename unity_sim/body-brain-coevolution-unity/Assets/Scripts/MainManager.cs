using UnityEngine;

public class MainManager : MonoBehaviour
{
    void Awake()
    {
        // Ensure 100Hz physics frequency for consistent simulation
        Time.fixedDeltaTime = 0.01f; // 1/100 seconds

        // Cap physics lag to 100ms to prevent instability
        Time.maximumDeltaTime = 0.1f; // 100ms
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
