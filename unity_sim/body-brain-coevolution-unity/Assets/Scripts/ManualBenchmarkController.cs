using UnityEngine;

public class ManualBenchmarkController : MonoBehaviour
{
    [Header("Joint References")]
    public ArticulationBody leftHip;
    public ArticulationBody leftKnee;
    public ArticulationBody leftAnkle;
    public ArticulationBody rightHip;
    public ArticulationBody rightKnee;
    public ArticulationBody rightAnkle;

    [Header("Gait Parameters")]
    public float walkSpeed = 5f;
    public float hipAmplitude = 20f;
    public float hipOffset = 10f;
    public float kneeAmplitude = 40f;
    public float kneeOffset = 45f;
    public float ankleAmplitude = 20f;
    public float ankleOffset = 5f;

    [Header("Trial Settings")]
    public float trialDuration = 10.0f;
    public Transform torsoTransform; // Assign ATLAS root here

    private float timer = 0f;
    private bool isTrialActive = false;
    private Vector3 startPosition;

    void OnEnable()
    {
        // Start trial automatically when this script is enabled
        startPosition = torsoTransform.position;
        timer = 0f;
        isTrialActive = true;
        Debug.Log("Manual Benchmark Trial Started.");
    }

    void FixedUpdate()
    {
        if (!isTrialActive) return;

        timer += Time.fixedDeltaTime;

        if (timer >= trialDuration)
        {
            EndTrial();
            return;
        }
        
        // Calculate base sine wave using Time.time for continuous oscillation
        float time = Time.time * walkSpeed;

        float leftPhase = Mathf.Sin(time);
        float rightPhase = Mathf.Sin(time + Mathf.PI); // Opposite phase for right leg

        float leftHipTarget = (leftPhase * hipAmplitude) + hipOffset;
        float rightHipTarget = (rightPhase * hipAmplitude) + hipOffset;
        float leftKneeTarget = (Mathf.Cos(time) * kneeAmplitude) + kneeOffset;
        float rightKneeTarget = (Mathf.Cos(time + Mathf.PI) * kneeAmplitude) + kneeOffset;
        float leftAnkleTarget = (-leftPhase * ankleAmplitude) + ankleOffset;
        float rightAnkleTarget = (-rightPhase * ankleAmplitude) + ankleOffset;
        
        SetJointTarget(leftHip, leftHipTarget);
        SetJointTarget(rightHip, rightHipTarget);
        SetJointTarget(leftKnee, leftKneeTarget);
        SetJointTarget(rightKnee, rightKneeTarget);
        SetJointTarget(leftAnkle, leftAnkleTarget);
        SetJointTarget(rightAnkle, rightAnkleTarget);
    }

    void SetJointTarget(ArticulationBody joint, float targetAngle)
    {
        if (joint != null)
        {
            ArticulationDrive drive = joint.xDrive;
            drive.target = targetAngle;
            joint.xDrive = drive;
        }
    }

    void EndTrial()
    {
        isTrialActive = false;
        float distanceX = torsoTransform.position.x - startPosition.x;
        Debug.Log($"Manual Trial Complete. Total X-Distance: {distanceX:F3} meters.");
    }
}
