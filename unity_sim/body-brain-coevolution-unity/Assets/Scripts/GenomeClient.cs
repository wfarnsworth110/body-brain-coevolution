using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

public class GenomeClient : MonoBehaviour
{
    // Point this to FastAPI Docker container
    private string baseUrl = "http://localhost:8000";

    [System.Serializable]
    public class GenomeResponse
    {
        public int individual_id;
        public float[] dna;
    }

    [System.Serializable]
    public class FitnessReport
    {
        public int individual_id;
        public float fitness_score;
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
                Debug.Log($"Received ID {response.individual_id}. DNA Length: {response.dna.Length}");

                // TODO: Here we would tell the 'CreatureSpawner' to build the agent
                SendFitness(response.individual_id, 10.5f); // Example placeholder call
            }
        }
    }

    public void SendFitness(int id, float score)
    {
        StartCoroutine(PostFitnessCoroutine(id, score));
    }

    IEnumerator PostFitnessCoroutine(int id, float score)
    {
        FitnessReport report = new FitnessReport { individual_id = id, fitness_score = score };
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
                Debug.Log("Fitness posted successfully!");
                RequestNextGenome(); // Get the next genome after posting fitness
            }
        }
    }

    void Start()
    {
        // Kick off loop upon starting the game
        RequestNextGenome();
    }
}
