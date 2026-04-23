using System;
using Unity.Plastic.Newtonsoft.Json;
using UnityEngine;

public class GenomeTranslator : MonoBehaviour
{
    /*
        NOTE: Keep leftover genes as junk DNA for the duration of the experiment.

        Genome: 128 floats between 0 and 1
        First 64: Physical morphology
            56 floats for each of the 7 segments:
                3 floats for size (x,y,z)
                1 float for mass
                2 floats for joint limits (min, max)
                1 float for joint stiffness
                1 float for joint damping
            8 floats for:
                Global gravity scale
                Ground friction
                Torso center of mass offset x-axis
                Torso center of mass offset y-axis
                Stance width
                Joint friction
                Global scale
                Joint force limit
        Second 64: Neural network
            58 floats for:
                24 input to hidden weights
                24 hidden to output weights
                4 hidden layer biases
                6 output layer biases
            6 floats for:
                Activation function slope
                Input smoothing
                Output smoothing
                Internal clock frequency
                Internal clock amplitude
                Global weight scale
    */

    public static GenomeTranslator Instance;

    [Header("Rig Segments (Assign in Inspector)")]
    public ArticulationBody torso;
    public ArticulationBody leftThigh, rightThigh;
    public ArticulationBody leftCalf, rightCalf;
    public ArticulationBody leftFoot, rightFoot;

    [Header("Morphology Scaling Ranges")]
    private readonly Vector2 scaleRange = new Vector2(0.5f, 2.0f);
    private readonly Vector2 massRange = new Vector2(0.5f, 10.0f);
    private readonly Vector2 limitRange = new Vector2(-90f, 90f);
    private readonly Vector2 stiffnessRange = new Vector2(0f, 10000f);
    private readonly Vector2 dampingRange = new Vector2(0f, 1000f);

    [Header("The Brain")]
    public NeuralController brain;

    [Header("Global Modifiers")]
    public float globalMotorMultiplier = 1.0f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void ApplyGenome(float[] dna)
    {
        if (dna.Length != 128)
        {
            Debug.LogError("DNA sequence must be exactly 128 floats.");
            return;
        }

        // Map the 7 segments (8 floats each = 56 floats)
        ApplySegmentTraits(torso, dna, 0);
        ApplySegmentTraits(leftThigh, dna, 8);
        ApplySegmentTraits(rightThigh, dna, 16);
        ApplySegmentTraits(leftCalf, dna, 24);
        ApplySegmentTraits(rightCalf, dna, 32);
        ApplySegmentTraits(leftFoot, dna, 40);
        ApplySegmentTraits(rightFoot, dna, 48);

        // Apply Global Morphology (56-63)
        // 56 - Global Motor Multiplier
        // 57-63 - Either junk DNA or additional global parameters (not implemented yet)
        globalMotorMultiplier = Mathf.Lerp(0.5f, 3.0f, dna[56]);

        // Map Neural Network Weights (64-127)
        if (brain != null)
        {
            brain.InitializeNetwork(dna, 64);
        }
        else
        {
            Debug.LogWarning("NeuralController reference not set in GenomeTranslator.");
        }
    }

    private void ApplySegmentTraits(ArticulationBody segment, float[] dna, int startIndex)
    {
        // 1. Scale (x, y, z) -> index 0-2
        float scaleX = Mathf.Lerp(scaleRange.x, scaleRange.y, dna[startIndex]);
        float scaleY = Mathf.Lerp(scaleRange.x, scaleRange.y, dna[startIndex + 1]);
        float scaleZ = Mathf.Lerp(scaleRange.x, scaleRange.y, dna[startIndex + 2]);
        segment.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);

        // 2. Mass -> index 3
        segment.mass = Mathf.Lerp(massRange.x, massRange.y, dna[startIndex + 3]);

        // Note: Torso doesn't have an X-Drive (it's the root),
        // so we skip limits/motors for it
        if (segment.isRoot) return;

        // 3. Joint Limits (Min, Max) -> Indices 4, 5
        float minLimit = Mathf.Lerp(limitRange.x, limitRange.y, dna[startIndex + 4]);
        float maxLimit = Mathf.Lerp(limitRange.x, limitRange.y, dna[startIndex + 5]);

        // Ensure min is always less than max to prevent physics errors
        if (minLimit > maxLimit)
        {
            float temp = minLimit;
            minLimit = maxLimit;
            maxLimit = temp;
        }

        ArticulationDrive xDrive = segment.xDrive;
        xDrive.lowerLimit = minLimit;
        xDrive.upperLimit = maxLimit;

        // 4. Actuator Strength (Stiffness, Damping) -> Indices 6, 7
        xDrive.stiffness = Mathf.Lerp(stiffnessRange.x, stiffnessRange.y, dna[startIndex + 6]);
        xDrive.damping = Mathf.Lerp(dampingRange.x, dampingRange.y, dna[startIndex + 7]);

        segment.xDrive = xDrive;
    }
}
