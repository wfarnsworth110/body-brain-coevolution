using UnityEngine;

public class NeuralController : MonoBehaviour
{
    public bool isInitialized = false;
    
    [Header("Network Dimensions")]
    private readonly int inputNodes = 6;
    private readonly int hiddenNodes = 4;
    private readonly int outputNodes = 6;

    [Header("Weight Ranges")]
    private readonly Vector2 weightRange = new Vector2(-2.0f, 2.0f);
    private readonly Vector2 biasRange = new Vector2(-2.0f, 2.0f);

    // Matrices
    private float[,] weightsInputHidden;
    private float[] biasesHidden;
    private float[,] weightsHiddenOutput;
    private float[] biasesOutput;

    // Node Activations
    private float[] hiddenLayer;
    private float[] outputLayer;

    public void InitializeNetwork(float[] dna, int startIndex)
    {
        weightsInputHidden = new float[inputNodes, hiddenNodes];
        biasesHidden = new float[hiddenNodes];
        weightsHiddenOutput = new float[hiddenNodes, outputNodes];
        biasesOutput = new float[outputNodes];
        
        hiddenLayer = new float[hiddenNodes];
        outputLayer = new float[outputNodes];

        int ptr = startIndex;

        // 1. Map Input to Hidden Weights (24 floats)
        for (int i = 0; i < inputNodes; i++)
        {
            for (int h = 0; h < hiddenNodes; h++)
            {
                weightsInputHidden[i, h] = Mathf.Lerp(weightRange.x, weightRange.y, dna[ptr++]);
            }
        }

        // 2. Map Hidden Layer Biases (4 floats)
        for (int h = 0; h < hiddenNodes; h++)
        {
            biasesHidden[h] = Mathf.Lerp(biasRange.x, biasRange.y, dna[ptr++]);
        }

        // 3. Map Hidden to Output Weights (24 floats)
        for (int h = 0; h < hiddenNodes; h++)
        {
            for (int o = 0; o < outputNodes; o++)
            {
                weightsHiddenOutput[h, o] = Mathf.Lerp(weightRange.x, weightRange.y, dna[ptr++]);
            }
        }

        // 4. Map Output Layer Biases (6 floats)
        for (int o = 0; o < outputNodes; o++)
        {
            biasesOutput[o] = Mathf.Lerp(biasRange.x, biasRange.y, dna[ptr++]);
        }

        isInitialized = true;

        // Note: 58 of 64 floats used; remaining 6 for global parameters or junk DNA.
    }

    public float[] Think(float[] inputs)
    {
        if (inputs.Length != inputNodes)
        {
            Debug.LogError($"Expected {inputNodes} inputs, but got {inputs.Length}.");
            return new float[outputNodes];
        }

        // Forward pass: Input to Hidden
        for (int h = 0; h < hiddenNodes; h++)
        {
            float sum = biasesHidden[h];
            for (int i = 0; i < inputNodes; i++)
            {
                sum += inputs[i] * weightsInputHidden[i, h];
            }
            hiddenLayer[h] = (float)System.Math.Tanh(sum); // Activation function
        }

        // Forward pass: Hidden to Output
        for (int o = 0; o < outputNodes; o++)
        {
            float sum = biasesOutput[o];
            for (int h = 0; h < hiddenNodes; h++)
            {
                sum += hiddenLayer[h] * weightsHiddenOutput[h, o];
            }
            outputLayer[o] = (float)System.Math.Tanh(sum); // Activation function
        }

        return outputLayer;
    }

    // Fixed Update loop to call the existing functionality
    void FixedUpdate()
    {
        
    }
}
