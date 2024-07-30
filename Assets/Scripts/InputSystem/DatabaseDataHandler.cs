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
        //GetDatabaseData();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public BsonDocument GetDatabaseData()
    {
        // Create client connection to our MongoDB database
        var client = new MongoClient(MongoDBConnectionString);

        var database = client.GetDatabase("SBOMDATA");
        var collection = database.GetCollection<BsonDocument>("SBOMDATA");

        var filter = Builders<BsonDocument>.Filter.Eq("SPDXID", "SPDXRef-DOCUMENT");
        var doc = collection.Find(filter).First();


        doc["_id"] = doc["_id"].ToString();
        //Debug.Log(doc.ToString());

        return doc;
    }
}
