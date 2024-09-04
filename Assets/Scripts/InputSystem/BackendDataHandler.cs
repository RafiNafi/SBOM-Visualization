using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using Unity.VisualScripting;
using System.Net.Http;
using Newtonsoft.Json;
using UnityEngine.Networking;
using System.Net;

public class BackendDataHandler : MonoBehaviour
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

    public IEnumerator GetAllCVEDataBySubstringAndField(string json, string field, System.Action<List<string>> callback)
    {

        UnityWebRequest request = new UnityWebRequest(BackendConnectionString + "/api/DB/" + field + "/", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log(": Received: " + request.downloadHandler.text);
            List<string> responseData = JsonConvert.DeserializeObject<List<string>>(request.downloadHandler.text);
            callback(responseData);
        }
        else
        {
            Debug.Log(": Error: " + request.error);
            callback(null);
        }
    }


    public IEnumerator GetDatabaseCompareDataByIds(string id1, string id2, string json, string field, System.Action<string, string, List<string>> callback)
    {

        UnityWebRequest request = new UnityWebRequest(BackendConnectionString + "/api/DB/SBOM/Compare/" + field + "/", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            //Debug.Log(": Received: " + request.downloadHandler.text);
            List<string> responseData = JsonConvert.DeserializeObject<List<string>>(request.downloadHandler.text);
            callback(id1, id2, responseData);
        }
        else
        {
            Debug.Log(": Error: " + request.error);
            callback(null, null, null);
        }
    }
}
