﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace SBOM_Visualizer_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DBController : ControllerBase
    {
        const string MongoDBConnectionString = "mongodb://localhost:27017"; //Localhost

        [HttpGet("{id}")]
        public IActionResult GetDatabaseDataById(string id)
        {
            var client = new MongoClient(MongoDBConnectionString);
            var database = client.GetDatabase("SBOMDATA");
            var collection = database.GetCollection<BsonDocument>("SBOMDATA");

            var filter = Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(id));

            var doc = collection.Find(filter).First();

            doc["_id"] = doc["_id"].ToString();

            return Ok(doc.ToString());
        }

        [HttpGet]
        public List<string> GetOnlyAllDocumentNames()
        {

            List<string> names = new List<string>();

            var client = new MongoClient(MongoDBConnectionString);
            var database = client.GetDatabase("SBOMDATA");
            var collection = database.GetCollection<BsonDocument>("SBOMDATA");

            var projection = Builders<BsonDocument>.Projection.Include("_id");
            // var filter = Builders<BsonDocument>.Filter.Eq("dataLicense", "CC0-1");
            var docs = collection.Find(new BsonDocument()).Project(projection).ToList();

            foreach (var document in docs)
            {
                names.Add(document["_id"].ToString());
            }

            return names;
        }

        [HttpGet("{searchCVE}/{field}")]
        public List<BsonDocument> GetCVEDataBySubstringAndField(string searchCVE, string field)
        {

            var client = new MongoClient(MongoDBConnectionString);
            var database = client.GetDatabase("SBOMDATA");
            var collection = database.GetCollection<BsonDocument>("CVE");

            // filter using a regex to search for the substring
            var filter = Builders<BsonDocument>.Filter.Regex(field, new BsonRegularExpression(searchCVE, "i"));

            var docs = collection.Find(filter).ToList();

            foreach (var document in docs)
            {
                //Debug.Log(document.ToString());
                document["_id"] = document["_id"].ToString();
            }

            return docs;
        }

    }
}