using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class GenomeClientTests
{
    [Test]
    public void GenomeResponse_ParsesIncomingJsonCorrectly()
    {
        // Mimic the exact JSON string Python sends
        string mockJson = "{\"individual_id\": 42, \"dna\": [0.5, -0.2, 0.9, 1.0]}";

        // Use Unity's JsonUtility to parse the string into C# object
        GenomeClient.GenomeResponse response = JsonUtility.FromJson<GenomeClient.GenomeResponse>(mockJson);

        // Verify the data was mapped to the variables correctly
        Assert.IsNotNull(response, "Response should not be null after parsing.");
        Assert.AreEqual(42, response.individual_id, "Individual ID did not parse correctly.");
        
        Assert.IsNotNull(response.dna, "DNA array should not be null.");
        Assert.AreEqual(4, response.dna.Length, "DNA array length is incorrect.");
        Assert.AreEqual(0.5f, response.dna[0], "First DNA value is incorrect.");
        Assert.AreEqual(-0.2f, response.dna[1], "Second DNA value is incorrect.");
    }

    [Test]
    public void FitnessReport_SerializesOutgoingJsonCorrectly()
    {
        // Create a mock fitness report
        GenomeClient.FitnessReport mockReport = new GenomeClient.FitnessReport 
        { 
            individual_id = 7, 
            fitness_score = 99.5f 
        };

        // Convert the C# object back into a JSON string
        string jsonOutput = JsonUtility.ToJson(mockReport);

        // Check if the resulting string contains the required FastAPI keys
        // Note: We remove spaces to avoid failing over minor formatting differences
        string compressedJson = jsonOutput.Replace(" ", "");
        
        StringAssert.Contains("\"individual_id\":7", compressedJson, "JSON missing correct individual_id.");
        StringAssert.Contains("\"fitness_score\":99.5", compressedJson, "JSON missing correct fitness_score.");
    }
}
