using UnityEngine;

public class ManualBenchmarkController : MonoBehaviour
{
    [Header("Joint References")]
    [Tooltip("Drag the Left Thigh ArticulationBody here")]
    public ArticulationBody leftHip;
    [Tooltip("Drag the Left Calf ArticulationBody here")]
    public ArticulationBody leftKnee;
    [Tooltip("Drag the Left Ankle ArticulationBody here")]
    public ArticulationBody leftAnkle;

    [Tooltip("Drag the Right Thigh ArticulationBody here")]
    public ArticulationBody rightHip;
    [Tooltip("Drag the Right Calf ArticulationBody here")]
    public ArticulationBody rightKnee;
    [Tooltip("Drag the Right Ankle ArticulationBody here")]
    public ArticulationBody rightAnkle;

    [Header("Gait Parameters")]
    [Tooltip("How fast the legs oscillate")]
    public float walkSpeed = 5f;

    [Tooltip("Maximum angle the hip swings forward/backward")]
    public float hipAmplitude = 30f;

    [Tooltip("Offset the hips to lean the torso forward")]
    public float hipOffset = -10f;

    [Tooltip("Maximum angle the knee bends")]
    public float kneeAmplitude = 45f;

    [Tooltip("Offset to ensure the knee only bends one way")]
    public float kneeOffset = 45f;

    [Tooltip("Maximum angle the ankle bends")]
    public float ankleAmplitude = 20f;
    [Tooltip("Default angle for the ankle to maintain ground contact")]
    public float ankleOffset = 5f;

    void FixedUpdate()
    {
        // Calculate base sine wave using Time.time for continuous oscillation
        float time = Time.time * walkSpeed;

        // Base sine wave (-1 to 1)
        float leftPhase = Mathf.Sin(time);
        float rightPhase = Mathf.Sin(time + Mathf.PI); // Opposite phase for right leg

        // Calculate hip target angles
        // Debug.Log($"Left Hip Target: {leftPhase * hipAmplitude}"); // Check hip target values
        float leftHipTarget = (leftPhase * hipAmplitude) + hipOffset;
        float rightHipTarget = (rightPhase * hipAmplitude) + hipOffset;

        // Calculate knee targets, using Cosine to offset sligtly from the hip movement
        float leftKneeTarget = (Mathf.Cos(time) * kneeAmplitude) + kneeOffset;
        float rightKneeTarget = (Mathf.Cos(time + Mathf.PI) * kneeAmplitude) + kneeOffset;

        // Calculate ankle targets, using inverted Sine to create a natural foot lift and drop
        float leftAnkleTarget = (-leftPhase * ankleAmplitude) + ankleOffset;
        float rightAnkleTarget = (-rightPhase * ankleAmplitude) + ankleOffset;

        // Apply the targets to the ArticulationBodies
        SetJointTarget(leftHip, leftHipTarget);
        SetJointTarget(rightHip, rightHipTarget);
        SetJointTarget(leftKnee, leftKneeTarget);
        SetJointTarget(rightKnee, rightKneeTarget);
        SetJointTarget(leftAnkle, leftAnkleTarget);
        SetJointTarget(rightAnkle, rightAnkleTarget);
    }

    // Heper method to update the X-Drive target of an ArticulationBody
    void SetJointTarget(ArticulationBody joint, float targetAngle)
    {
        if (joint != null)
        {
            // Extract current drive, update target, and reassign
            ArticulationDrive drive = joint.xDrive;
            drive.target = targetAngle;
            joint.xDrive = drive;
        }
    }
}
