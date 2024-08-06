using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System.Threading.Tasks;
using Unity.VisualScripting;

public class DatabaseDataHandler : MonoBehaviour
{

    const string MongoDBConnectionString = "mongodb://localhost:27017"; //Localhost

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /*
    public BsonDocument GetDatabaseData()
    {
        // Create client connection to MongoDB database
        var client = new MongoClient(MongoDBConnectionString);
        var database = client.GetDatabase("SBOMDATA");
        var collection = database.GetCollection<BsonDocument>("SBOMDATA");

        //var filter = Builders<BsonDocument>.Filter.Eq("SPDXID", "SPDXRef-DOCUMENT"); // big example
        var filter = Builders<BsonDocument>.Filter.Eq("dataLicense", "CC0-1"); // small example
        
        var doc = collection.Find(filter).First();


        doc["_id"] = doc["_id"].ToString();
        //Debug.Log(doc.ToString());

        return doc;
    }
    */

    public BsonDocument GetDatabaseDataById(string id)
    {
        var client = new MongoClient(MongoDBConnectionString);
        var database = client.GetDatabase("SBOMDATA");
        var collection = database.GetCollection<BsonDocument>("SBOMDATA");

        var filter = Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(id)); 

        var doc = collection.Find(filter).First();

        doc["_id"] = doc["_id"].ToString();

        return doc;
    }

    public List<string> GetOnlyAllDocumentNames()
    {
        List<string> names = new List<string>();

        var client = new MongoClient(MongoDBConnectionString);
        var database = client.GetDatabase("SBOMDATA");
        var collection = database.GetCollection<BsonDocument>("SBOMDATA");

        var projection = Builders<BsonDocument>.Projection.Include("_id");
        var filter = Builders<BsonDocument>.Filter.Eq("dataLicense", "CC0-1");
        var docs = collection.Find(new BsonDocument()).Project(projection).ToList();

        foreach (var document in docs)
        {
            names.Add(document["_id"].ToString());
        }

        return names;
    }

    public void GetCWEData()
    {
        string searchCWE_ID = "CVE-2022-33915";
        string searchCWE_Name = "Log4j";

        var client = new MongoClient(MongoDBConnectionString);
        var database = client.GetDatabase("SBOMDATA");
        var collection = database.GetCollection<BsonDocument>("CVE");

    }

}
