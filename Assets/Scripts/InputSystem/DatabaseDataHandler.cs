using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MongoDB.Bson;
using System.Threading.Tasks;
using Unity.VisualScripting;
using System.Net.Http;
using Newtonsoft.Json;


public class DatabaseDataHandler : MonoBehaviour
{

    //const string MongoDBConnectionString = "mongodb://localhost:27017"; //Localhost
    const string BackendConnectionString = "http://192.168.178.88:7204";

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public async Task<string> GetDatabaseDataById(string id)
    {

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
    }


    public async Task<List<string>> GetOnlyAllDocumentNames()
    {
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
    }


    public async Task<List<string>> GetCVEDataBySubstringAndField(string searchCWE, string field)
    {
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
    }


}
