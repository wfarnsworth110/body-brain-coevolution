using UnityEngine;

public class AgentLocomotion : MonoBehaviour
{
    [Header("Motor Control")]
    public float maxTargetAngle = 45.0f;

    void FixedUpdate()
    {
        if (GenomeTranslator.Instance == null || GenomeTranslator.Instance.brain == null)
        {
            return;
        }

        ArticulationBody[] joints =
        {
            GenomeTranslator.Instance.leftThigh,
            GenomeTranslator.Instance.rightThigh,
            GenomeTranslator.Instance.leftCalf,
            GenomeTranslator.Instance.rightCalf,
            GenomeTranslator.Instance.leftFoot,
            GenomeTranslator.Instance.rightFoot
        };

        float[] inputs = new float[6];
        for (int i = 0; i < joints.Length; i++)
        {
            ArticulationBody joint = joints[i];
            if (joint == null)
            {
                inputs[i] = 0f;
                continue;
            }

            ArticulationDrive drive = joint.xDrive;
            float lower = drive.lowerLimit;
            float upper = drive.upperLimit;
            float current = joint.jointPosition[0];

            if (Mathf.Abs(upper - lower) < Mathf.Epsilon)
            {
                inputs[i] = 0f;
            }
            else
            {
                float t = Mathf.InverseLerp(lower, upper, current);
                inputs[i] = Mathf.Lerp(-1f, 1f, t);
            }
        }

        float[] outputs = GenomeTranslator.Instance.brain.Think(inputs);

        for (int i = 0; i < joints.Length; i++)
        {
            ArticulationBody joint = joints[i];
            if (joint == null || i >= outputs.Length)
            {
                continue;
            }

            float target = outputs[i] * maxTargetAngle;
            ArticulationDrive drive = joint.xDrive;
            drive.target = target;
            joint.xDrive = drive;
        }
    }
}
