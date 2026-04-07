using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

public class GenomeClient : MonoBehaviour
{
    // Point this to FastAPI Docker container
    private string baseUrl = "http://localhost:8000";
    private int lastReceivedGeneration; // Track the generation of the last received genome

    [System.Serializable]
    public class GenomeResponse
    {
        public int individual_id;
        public float[] dna;
        public int generation;
        public bool is_finished;
    }

    [System.Serializable]
    public class FitnessReport
    {
        public int individual_id;
        public float fitness_score;
        // Added for FastAPI validation
        public int generation;
    }

    public void RequestNextGenome()
    {
        StartCoroutine(GetGenomeCoroutine());

    }

    IEnumerator GetGenomeCoroutine()
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(baseUrl + "/get-genome"))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                GenomeResponse response = JsonUtility.FromJson<GenomeResponse>(webRequest.downloadHandler.text);

                // Exit condition if all generations are finished
                if (response.is_finished)
                {
                    Debug.Log("All generations completed! Stopping simulation.");
                    yield break; // Exit the coroutine
                }

                lastReceivedGeneration = response.generation; // Store the generation for fitness reporting
                Debug.Log($"Gen {response.generation} | Received ID {response.individual_id}");

                // TODO Genome Translator logic (script not yet implemented)
                // GenomeTranslator.Instance.ApplyGenome(response.dna);

                // TODO Placeholder: Wait 10 seconds in the simultation
                float mockFitness = Random.Range(0f, 15f); // Mock fitness score for testing
                SendFitness(response.individual_id, lastReceivedGeneration, mockFitness);
            }
        }
    }

    public void SendFitness(int id, int generation, float score)
    {
        StartCoroutine(PostFitnessCoroutine(id, generation, score));
    }

    IEnumerator PostFitnessCoroutine(int id, int generation, float score)
    {
        FitnessReport report = new FitnessReport
        {
            individual_id = id,
            fitness_score = score,
            generation = generation
        };

        string json = JsonUtility.ToJson(report);

        using (UnityWebRequest webRequest = new UnityWebRequest(baseUrl + "/post-fitness", "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                RequestNextGenome(); // Get the next genome after posting fitness
            }
            else
            {
                Debug.LogError($"Error posting fitness: {webRequest.error}");
            }
        }
    }

    void Start()
    {
        RequestNextGenome();
    }
}
