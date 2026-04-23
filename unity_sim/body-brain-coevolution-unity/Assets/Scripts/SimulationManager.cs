using UnityEngine;
using System.Collections;

public class SimulationManager : MonoBehaviour
{
    public static SimulationManager Instance;

    [Header("Trial Settings")]
    public float trialDuration = 10.0f;
    public float heightThreshold = 0.4f; // If the torso falls below this, trial ends
    public Transform torsoTransform;

    private Vector3 startPosition;
    private bool isSimulating = false;
    private int currentIndividualId;
    private int currentGen;

    private void Awake() => Instance = this;

    public void BeginTrial(int id, int gen, float[] dna)
    {
        currentIndividualId = id;
        currentGen = gen;

        // 1. Reset Physics State (Teleport to start, zero out velocities)
        ResetRobot();

        // 2. Morph the robot based on DNA
        GenomeTranslator.Instance.ApplyGenome(dna);

        // 3. Start the timer
        startPosition = torsoTransform.position;
        StartCoroutine(TrialRoutine());
    }

    private IEnumerator TrialRoutine()
    {
        isSimulating = true;
        float elapsed = 0.0f;

        while (elapsed < trialDuration)
        {
            // Exit early if robot falls over
            if (torsoTransform.position.y < heightThreshold)
            {
                Debug.Log("Robot fell! Ending trial early.");
                break;
            }

            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        EndTrial();
    }

    private void EndTrial()
    {
        isSimulating = false;

        // Calculate Fitness: Simple X-distance traveled
        float distanceX = torsoTransform.position.x - startPosition.x;

        // Penalize for drifting off the z-axis (lateral instability)
        float penaltyZ = Mathf.Abs(torsoTransform.position.z - startPosition.z) * 0.5f;

        float finalFitness = Mathf.Max(0, distanceX - penaltyZ);

        Debug.Log($"Trial Done. Fitness: {finalFitness}");

        // Report back to the Python server via GenomeClient
        GetComponent<GenomeClient>().SendFitness(currentIndividualId, currentGen, finalFitness);
    }

    private void ResetRobot()
    {
        if (torsoTransform == null)
        {
            Debug.LogError("SimulationManager.ResetRobot called without a torsoTransform reference.");
            return;
        }

        ArticulationBody rootBody = torsoTransform.GetComponent<ArticulationBody>();
        if (rootBody == null)
        {
            Debug.LogError("SimulationManager.ResetRobot could not find an ArticulationBody on torsoTransform.");
            return;
        }

        ArticulationBody[] bodies = torsoTransform.GetComponentsInChildren<ArticulationBody>();

        // Reset full articulation root transform to the start pose.
        rootBody.TeleportRoot(new Vector3(0, 1.5f, 0), Quaternion.identity);

        foreach (ArticulationBody body in bodies)
        {
            body.jointVelocity = new ArticulationReducedSpace(0f);
            // Unity 6 does not allow setting jointAcceleration directly.
            body.jointForce = new ArticulationReducedSpace(0f);
            body.jointPosition = new ArticulationReducedSpace(0f);
        }
    }

    [ContextMenu("Test Single Dummy Individual")]
    public void TestSingleIndividual()
    {
        float[] dummyDNA = new float[128];
        for(int i = 0; i < 128; i++) dummyDNA[i] = 0.5f; // Middle-of-the-road traits
    
        BeginTrial(999, 1, dummyDNA);
    }

    [ContextMenu("Test Random Individual")]
    public void TestRandomIndividual()
    {
        // Random seed (optional)
        // Random.InitState(System.DateTime.Now.Millisecond);
        // Random.InitState(42);

        float[] randomDNA = new float[128];
        for(int i = 0; i < 128; i++)
        {
            // Random value between 0 and 1
            randomDNA[i] = UnityEngine.Random.value;
        }

        Debug.Log("Testing random individual");

        BeginTrial(888, 1, randomDNA);
    }
}
