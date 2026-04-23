using UnityEngine;
using System.Collections;
using System;

public class SimulationManager : MonoBehaviour
{
    public static SimulationManager Instance;

    // Trial parameters
    [Header("Trial Settings")]
    public float trialDuration = 10.0f;
    public float heightThreshold = 0.4f; // If the torso falls below this, trial ends
    public Transform torsoTransform;

    // Fitness function information
    [Header("Fitness Weights")]
    public float w1_torque = 0.0001f;
    public float w2_zDrift = 0.0005f;

    private float cumulativeTorque = 0f;
    private float cumulativeZDrift = 0f;
    private ArticulationBody[] allBodies;

    // State tracking
    private Vector3 startPosition;
    private bool isSimulating = false;
    private int currentIndividualId;
    private int currentGen;

    private void Awake() => Instance = this;

    public void BeginTrial(int id, int gen, float[] dna)
    {
        // Use isSimulating to prevent overlapping trials
        if (isSimulating)
        {
            Debug.LogWarning("Trial is already in progress. Ignoring.");
            return;
        }

        currentIndividualId = id;
        currentGen = gen;

        // 1. Reset Physics State (Teleport to start, zero out velocities)
        ResetRobot();

        // 2. Morph the robot based on DNA
        GenomeTranslator.Instance.ApplyGenome(dna);

        // 2.5 Initialize accumulators and cache bodies for torque calculation
        cumulativeTorque = 0f;
        cumulativeZDrift = 0f;
        allBodies = torsoTransform.GetComponentsInChildren<ArticulationBody>();

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

            // Accumulate torque and z-drift for fitness calculation
            float currentZDrift = Mathf.Abs(torsoTransform.position.z - startPosition.z);
            cumulativeZDrift += (w2_zDrift * currentZDrift);

            float currentTorque = 0f;
            foreach (ArticulationBody body in allBodies)
            {
                if (!body.isRoot && body.dofCount > 0)
                {
                    currentTorque += Mathf.Abs(body.jointVelocity[0]);
                }
            }
            cumulativeTorque += (w1_torque * currentTorque);

            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        EndTrial(elapsed);
    }

    private void EndTrial(float timeElapsed)
    {
        isSimulating = false;

        float distanceX = torsoTransform.position.x - startPosition.x;

        // Prevent division by zero
        float validTime = Mathf.Max(0.1f, timeElapsed);

        /*
            Fitness function:
            F = (distanceX / timeElapsed) - (w1 * cumulativeTorque) - (w2 * cumulativeZDrift)
            - distanceX / timeElapsed rewards forward movement speed.
            - w1 * cumulativeTorque penalizes excessive torque usage.
            - w2 * cumulativeZDrift penalizes drifting away from the center line.
        */
        float finalFitness = (distanceX / validTime) - cumulativeTorque - cumulativeZDrift;

        // Ensure fitness is not negative
        finalFitness = Mathf.Max(0f, finalFitness);

        Debug.Log($"Gen {currentGen} Ind {currentIndividualId} | Fit: {finalFitness:F3} | Dist: {distanceX:F2} | Torque Pen: {cumulativeTorque:F2} | Z-Pen: {cumulativeZDrift:F2}");
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

    // Load HOF JSON file directly into Unity
    [System.Serializable]
    public class EliteData { public int rank; public float fitness; public float[] dna; }
    [System.Serializable]
    public class EliteList { public EliteData[] elites; }

    [Header("Playback")]
    public TextAsset eliteJsonFile; // Drag exported JSON file here in the inspector
    public int eliteRankToPlay = 1; // Change to 2, 3, etc. to play different elites

    [ContextMenu("Play Elite from JSON")]
    public void PlayElite() {
        if (eliteJsonFile == null) return;

        // Wrap the JSON array in an object for Unity's JsonUtility
        string wrappedJson = "{\"elites\":" + eliteJsonFile.text + "}";
        EliteList eliteList = JsonUtility.FromJson<EliteList>(wrappedJson);

        foreach (var elite in eliteList.elites)
        {
            if (elite.rank == eliteRankToPlay)
            {
                Debug.Log($"Playing Elite Rank {elite.rank} with Fitness {elite.fitness}");
                BeginTrial(999, 999, elite.dna);
                return;
            }
        }
    }
}
