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
        const string MongoDBConnectionString = "mongodb://localhost:27017"; //Localhost
        const string DBName = "SBOMDATA";
        const string CollectionSBOMName = "SBOMDATA";
        const string CollectionCVEName = "CVE";

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
            // var filter = Builders<BsonDocument>.Filter.Eq("dataLicense", "CC0-1");
            var docs = collection.Find(new BsonDocument()).Project(projection).ToList();

            foreach (var document in docs)
            {
                names.Add(document["_id"].ToString());
            }

            return Ok(names);
        }

        [HttpGet("{searchCVE}/{field}")]
        public IActionResult GetCVEDataBySubstringAndField(string searchCVE, string field)
        {
            List<string> CVEList = new List<string>();

            var client = new MongoClient(MongoDBConnectionString);
            var database = client.GetDatabase(DBName);
            var collection = database.GetCollection<BsonDocument>(CollectionCVEName);

            // filter using a regex to search for the substring
            var filter = Builders<BsonDocument>.Filter.Regex(field, new BsonRegularExpression(searchCVE, "i"));

            var docs = collection.Find(filter).ToList();

            foreach (var document in docs)
            {
                document["_id"] = document["_id"].ToString();

                string shortenedVersion = "{\"CVE Record\": {";
                shortenedVersion += "\"CVE-ID\":" + "\"" + document["cveMetadata"]["cveId"] + "\",";
                shortenedVersion += "\"Product\":" + "\"" + document["containers"]["cna"]["affected"][0]["product"] + "\",";
                shortenedVersion += "\"Vendor\":" + "\"" + document["containers"]["cna"]["affected"][0]["vendor"] + "\",";
                if(document["containers"]["cna"]["descriptions"][0]["value"].ToString() != null)
                {
                    shortenedVersion += "\"Description\":" + "\"" + document["containers"]["cna"]["descriptions"][0]["value"].ToString().Replace("{", "").Replace("}", "") + "\",";
                } 
                else
                {
                    shortenedVersion += "\"Description\":" + "\"" + document["containers"]["cna"]["descriptions"][0]["value"] + "\",";
                }
                shortenedVersion += "\"Problem Type\":" + "\"" + document["containers"]["cna"]["problemTypes"][0]["descriptions"][0]["description"] + "\"";
                shortenedVersion += "}}";

                //CVEList.Add(document.ToString());

                CVEList.Add(shortenedVersion);
                Console.WriteLine(shortenedVersion);
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
                Console.WriteLine(id);
                // filter using a regex to search for the substring
                var filter = Builders<BsonDocument>.Filter.Regex(field, new BsonRegularExpression(id, "i"));

                var docs = collection.Find(filter).ToList();

                foreach (var document in docs)
                {
                    document["_id"] = document["_id"].ToString();

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

                    CVEList.Add(shortenedVersion);
                    Console.WriteLine(shortenedVersion);
                }
            }

            return Ok(CVEList);
        }

        public class StringListWrapper
        {
            public List<string> strings { get; set; }
        }

    }
}
