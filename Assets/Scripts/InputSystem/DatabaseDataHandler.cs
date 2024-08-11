using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MongoDB.Bson;
using System.Threading.Tasks;
using Unity.VisualScripting;
using System.Net.Http;
using Newtonsoft.Json;
using UnityEngine.Networking;

public class DatabaseDataHandler : MonoBehaviour
{

    const string BackendConnectionString = "http://192.168.178.88:7204";

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public IEnumerator GetDatabaseDataById(string id, System.Action<string, string> callback)
    {
        /*
        string responseData = "";
        // Create an instance of HttpClient
        using (HttpClient client = new HttpClient())
        {
            client.BaseAddress = new Uri(BackendConnectionString);

            string endpoint = "/api/DB/" + id + "/";

            HttpResponseMessage response = await client.GetAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                responseData = await response.Content.ReadAsStringAsync();
                Debug.Log("Response received: " + responseData);
            }
            else
            {
                Debug.Log("Request failed with status code: " + response.StatusCode);
            }
            client.Dispose();
        }
        return responseData;
        */

        using (UnityWebRequest webRequest = UnityWebRequest.Get(BackendConnectionString + "/api/DB/" + id + "/"))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.Log(": Error: " + webRequest.error);
                callback(null, null);
            }
            else
            {
                Debug.Log(": Received: " + webRequest.downloadHandler.text);
                callback(id, webRequest.downloadHandler.text);
            }
        }

    }


    public IEnumerator GetOnlyAllDocumentNames(System.Action<List<string>> callback)
    {
        /*
        List<string> responseData = new List<string>();

        using (HttpClient client = new HttpClient())
        {
            client.BaseAddress = new Uri(BackendConnectionString);

            string endpoint = "/api/DB/";

            HttpResponseMessage response = await client.GetAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                string responseString = await response.Content.ReadAsStringAsync();
                responseData  = JsonConvert.DeserializeObject<List<string>>(responseString);
                Debug.Log("Response received: " + responseData);
            }
            else
            {
                Debug.Log("Request failed with status code: " + response.StatusCode);
            }
            client.Dispose();
        }

        return responseData;
        */

        using (UnityWebRequest webRequest = UnityWebRequest.Get(BackendConnectionString + "/api/DB/"))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.Log(": Error: " + webRequest.error);
                callback(null);
            }
            else
            {
                Debug.Log(": Received: " + webRequest.downloadHandler.text);
                List<string> responseData = JsonConvert.DeserializeObject<List<string>>(webRequest.downloadHandler.text);
                callback(responseData);
            }
        }
    }


    public IEnumerator GetCVEDataBySubstringAndField(string searchCWE, string field, System.Action<List<string>> callback)
    {
        /*
        List<string> responseData = new List<string>();

        using (HttpClient client = new HttpClient())
        {
            client.BaseAddress = new Uri(BackendConnectionString);

            string endpoint = "/api/DB/" + searchCWE + "/" + field + "/";

            HttpResponseMessage response = await client.GetAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                string responseString = await response.Content.ReadAsStringAsync();
                responseData = JsonConvert.DeserializeObject<List<string>>(responseString);

                Debug.Log("Response received: " + responseData);
            }
            else
            {
                Debug.Log("Request failed with status code: " + response.StatusCode);
            }
            client.Dispose();
        }

        return responseData;
        */

        using (UnityWebRequest webRequest = UnityWebRequest.Get(BackendConnectionString + "/api/DB/" + searchCWE + "/" + field + "/"))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.Log(": Error: " + webRequest.error);
                callback(null);
            }
            else
            {
                Debug.Log(": Received: " + webRequest.downloadHandler.text);
                List<string> responseData = JsonConvert.DeserializeObject<List<string>>(webRequest.downloadHandler.text);
                callback(responseData);
            }
        }
    }


}
