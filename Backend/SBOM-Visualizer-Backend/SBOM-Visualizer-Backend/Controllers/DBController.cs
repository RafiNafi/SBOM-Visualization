using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Web;

namespace SBOM_Visualizer_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DBController : ControllerBase
    {
        const string MongoDBConnectionString = "mongodb://localhost:27017";
        const string DBName = "SBOMDATA";
        const string CollectionSBOMName = "SBOMDATA";
        const string CollectionCVEName = "CVE";


        [HttpGet("SBOM/{value}/{field}")]
        public IActionResult GetSBOMDataBySubstringAndField(string value, string field)
        {

            var client = new MongoClient(MongoDBConnectionString);
            var database = client.GetDatabase(DBName);
            var collection = database.GetCollection<BsonDocument>(CollectionCVEName);

            var filter = Builders<BsonDocument>.Filter.Regex(field, new BsonRegularExpression(value, "i"));

            var doc = collection.Find(filter).First();

            doc["_id"] = doc["_id"].ToString();

            return Ok(doc.ToString());
        }

        [HttpGet("{id}")]
        public IActionResult GetDatabaseDataById(string id)
        {
            var client = new MongoClient(MongoDBConnectionString);
            var database = client.GetDatabase(DBName);
            var collection = database.GetCollection<BsonDocument>(CollectionSBOMName);

            var filter = Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(id));

            var doc = collection.Find(filter).First();

            doc["_id"] = doc["_id"].ToString();

            return Ok(doc.ToString());
        }

        [HttpGet]
        public IActionResult GetOnlyAllDocumentNames()
        {

            List<string> names = new List<string>();

            var client = new MongoClient(MongoDBConnectionString);
            var database = client.GetDatabase(DBName);
            var collection = database.GetCollection<BsonDocument>(CollectionSBOMName);

            var projection = Builders<BsonDocument>.Projection.Include("_id");
            var docs = collection.Find(new BsonDocument()).Project(projection).ToList();

            foreach (var document in docs)
            {
                names.Add(document["_id"].ToString());
            }

            return Ok(names);
        }

        [HttpGet("{value}/{field}")]
        public IActionResult GetCVEDataBySubstringAndField(string value, string field)
        {
            List<string> CVEList = new List<string>();

            var client = new MongoClient(MongoDBConnectionString);
            var database = client.GetDatabase(DBName);
            var collection = database.GetCollection<BsonDocument>(CollectionCVEName);

            // a regex to search for substring
            var filter = Builders<BsonDocument>.Filter.Regex(field, new BsonRegularExpression(value, "i"));

            var docs = collection.Find(filter).ToList();

            foreach (var document in docs)
            {
                document["_id"] = document["_id"].ToString();

                string shortenedVersion = ShortenInformation(document);
                CVEList.Add(shortenedVersion);

                //CVEList.Add(document.ToString());  // To get all CVE Information uncomment this and comment two lines above
            }
            return Ok(CVEList);
        }


        [HttpPost("{field}")]
        public IActionResult GetAllCVEDataBySubstringAndField([FromBody] StringListWrapper stringsWrapper, string field)
        {

            if (stringsWrapper == null || stringsWrapper.strings == null)
            {
                return BadRequest("Invalid data.");
            }

            List<string> CVEList = new List<string>();

            var client = new MongoClient(MongoDBConnectionString);
            var database = client.GetDatabase(DBName);
            var collection = database.GetCollection<BsonDocument>(CollectionCVEName);

            foreach(string id in stringsWrapper.strings)
            {
                var filter = Builders<BsonDocument>.Filter.Regex(field, new BsonRegularExpression(id, "i"));

                var docs = collection.Find(filter).ToList();

                foreach (var document in docs)
                {
                    document["_id"] = document["_id"].ToString();

                    string shortenedVersion = ShortenInformation(document);

                    CVEList.Add(shortenedVersion);

                    //Console.WriteLine(shortenedVersion);
                }
            }

            return Ok(CVEList);
        }

        [NonAction]
        public string ShortenInformation(BsonDocument document)
        {
            string shortenedVersion = "{\"CVE Record\": {";
            shortenedVersion += "\"CVE-ID\":" + "\"" + document["cveMetadata"]["cveId"] + "\",";
            shortenedVersion += "\"Product\":" + "\"" + document["containers"]["cna"]["affected"][0]["product"] + "\",";
            shortenedVersion += "\"Vendor\":" + "\"" + document["containers"]["cna"]["affected"][0]["vendor"] + "\",";

            if (document["containers"]["cna"]["descriptions"][0]["value"].ToString() != null)
            {
                shortenedVersion += "\"Description\":" + "\"" +
                    document["containers"]["cna"]["descriptions"][0]["value"].ToString().Replace("{", "(").Replace("}", ")").Replace("\"", "\\\"") + "\",";
            }
            else
            {
                shortenedVersion += "\"Description\":" + "\"" + document["containers"]["cna"]["descriptions"][0]["value"] + "\",";
            }

            shortenedVersion += "\"Problem Type\":" + "\"" + document["containers"]["cna"]["problemTypes"][0]["descriptions"][0]["description"] + "\"";
            shortenedVersion += "}}";

            return shortenedVersion;
        }

        public class StringListWrapper
        {
            public List<string> strings { get; set; }
        }

    }
}
