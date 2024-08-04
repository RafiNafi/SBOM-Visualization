using JetBrains.Annotations;
using MongoDB.Bson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using static UnityEngine.Rendering.DebugUI;


public class InputReader : MonoBehaviour
{
    public GameObject BallPrefab;

    public List<DataObject> dataObjects = new List<DataObject>();
    public Dictionary<int, int> level_occurrences = new Dictionary<int, int>();
    public LineDrawer ld;
    public Dictionary<string, UnityEngine.Color> colors = new Dictionary<string, UnityEngine.Color>();

    public DatabaseDataHandler dbHandler;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void CreateGraph(BsonDocument sbomElement)
    {
        Initilization();
        ReadFileAndCreateObjects(sbomElement);
        FuseSameNodes();
        PositionDataBalls();
        ColorDataBalls();
    }

    public void Initilization()
    {
        foreach (var obj in dataObjects)
        {
            Destroy(obj.DataBall);

            foreach(var parent_line in obj.relationship_line_parent)
            {
                Destroy(parent_line);
            }
        }

        dataObjects.Clear();
        level_occurrences.Clear();
        colors.Clear();
    }

    public void ReadFileAndCreateObjects(BsonDocument sbomElement)
    {
        //string jsonTest = "{\r\n  \"name\": \"32098/github.com/CortezFrazierJr/my_recipe_book-418bed3e334f2c1b2d4de973e069037dc2faf60e\",\r\n  \"documentNamespace\": \"https://s3.us-east-1.amazonaws.com/blob.fossa.io/FOSSA_BOMS/custom%2B32098%2Fgithub.com%2FCortezFrazierJr%2Fmy_recipe_book%24418bed3e334f2c1b2d4de973e069037dc2faf60e\",\r\n  \"dataLicense\": \"CC0-1.0\",\r\n  \"packages\": [\r\n    {\r\n      \"SPDXID\": \"SPDXRef-custom-32098-github.com-CortezFrazierJr-my-recipe-book-418bed3e334f2c1b2d4de973e069037dc2faf60e\",\r\n      \"name\": \"https://github.com/CortezFrazierJr/my_recipe_book.git\",\r\n      \"versionInfo\": \"418bed3e334f2c1b2d4de973e069037dc2faf60e\",\r\n      \"filesAnalyzed\": true,\r\n      \"downloadLocation\": \"NOASSERTION\",\r\n      \"originator\": \"Organization: Custom (provided build)\",\r\n      \"supplier\": \"Organization: Uchiha Cortez\",\r\n      \"packageFileName\": \"custom+32098/github.com/CortezFrazierJr/my_recipe_book$418bed3e334f2c1b2d4de973e069037dc2faf60e\",\r\n      \"summary\": \"Project uploaded via Provided Builds from fossa-cli\",\r\n      \"licenseDeclared\": \"LicenseRef-ISC-26342263\",\r\n      \"copyrightText\": \"NONE\",\r\n      \"homepage\": \"NOASSERTION\",\r\n      \"licenseConcluded\": \"NOASSERTION\",\r\n      \"checksums\": [\r\n        {\r\n          \"algorithm\": \"MD5\",\r\n          \"checksumValue\": \"10cab2cd0690ffb91baeba0b42a3453b\"\r\n        },\r\n        {\r\n          \"algorithm\": \"SHA1\",\r\n          \"checksumValue\": \"c147bad1f95516ab7bee6ba2023020d7123c2fac\"\r\n        },\r\n        {\r\n          \"algorithm\": \"SHA256\",\r\n          \"checksumValue\": \"ee1300ac533cebc2d070ce3765685d5f7fca2a5a78ca15068323f68ed63d4abf\"\r\n        }\r\n      ],\r\n      \"externalRefs\": []\r\n    }]}";
        //string jsonTest = System.IO.File.ReadAllText("C:\\Users\\Rafi\\Desktop\\Studium Semester\\Master\\Semester 2\\Projektarbeit\\sampleSPDX.json");
        //string jsonTest = System.IO.File.ReadAllText("C:\\Users\\Rafi\\Desktop\\Studium Semester\\Master\\Semester 2\\Projektarbeit\\bom.json");

        dynamic jsonObj = JObject.Parse(sbomElement.ToString());

        var dict = JsonConvert.DeserializeObject<Dictionary<string, JToken>>(sbomElement.ToString());

        //Main Root Point
        Debug.Log("ERSTELLUNG START");
        DataObject main_root = CreateDataObjectWithBall(0, "ROOT", "", null);
        ProcessLevelOccurence(0);
        dataObjects.Add(main_root);

        RecursiveRead(dict,"ROOT", main_root);

    }

    public DataObject CreateDataObjectWithBall(int level, string key, string value, DataObject parent)
    {
        GameObject dataPoint = Instantiate(BallPrefab, new Vector3(1, 1, 1), Quaternion.identity);
        TextMeshPro text = dataPoint.GetComponentInChildren<TextMeshPro>();

        if(value != "")
        {
            text.text = key + ":" + value;
        }
        else
        {
            text.text = key;
        }

        dataPoint.GetComponentInChildren<Renderer>().material.color = new UnityEngine.Color(0, 0, 1, 1.0f);

        if(parent != null)
        {
            ProcessLevelOccurence(parent.level + 1);
            parent.nr_children++;
            //Debug.Log(parent.level + 1);
        }

        return new DataObject(dataPoint,level,key,value,parent);
    }

    //Creates DataObjects and corresponding 3D Balls recursively through JSON 
    public void RecursiveRead(Dictionary<string, JToken> dict1, string prev, DataObject parent)
    {
        
        foreach (var kv in dict1)
        {

            if (kv.Value.ToString().Contains("{") && kv.Value.ToString().Contains("}"))
            {
                string Value = kv.Value.ToString();

                if (kv.Value.ToString()[0] is '[' && kv.Value.ToString()[kv.Value.ToString().Length - 1] is ']')
                {
                    int counter = 0;
                    Debug.Log(prev + "-[REL]->" + kv.Key);

                    DataObject new_parent = CreateDataObjectWithBall(parent.level + 1,kv.Key,"",parent);
                    dataObjects.Add(new_parent);

                    //Sub Points
                    foreach (var sub in kv.Value.Children().ToList())
                    {
                        counter++;

                        string sub_node = kv.Key.ToString().Substring(0, kv.Key.Length - 1);
                        Debug.Log(kv.Key + "-[REL]->" + sub_node + counter);

                        DataObject new_sub_parent = CreateDataObjectWithBall(new_parent.level + 1, sub_node + counter, "", new_parent);
                        dataObjects.Add(new_sub_parent);

                        var dictSub = JsonConvert.DeserializeObject<Dictionary<string, JToken>>(sub.ToString());
                        RecursiveRead(dictSub, sub_node + counter, new_sub_parent);
                    }
                }
                else
                {
                    //Sub Point
                    Debug.Log(prev + "-[REL]->" + kv.Key);

                    DataObject new_parent = CreateDataObjectWithBall(parent.level + 1, kv.Key, "", parent);
                    dataObjects.Add(new_parent);

                    var dict2 = JsonConvert.DeserializeObject<Dictionary<string, JToken>>(Value);
                    RecursiveRead(dict2, kv.Key, new_parent);
                }

            }
            else if (kv.Value.ToString().Contains("[") && kv.Value.ToString().Contains("]"))
            {
                //Check if convertible to array or if '[' or ']' character just in string value
                if(kv.Value.IsConvertibleTo<JArray>(true))
                {
                    //Array End Point
                    Debug.Log(prev + "-[REL]->" + kv.Key);

                    DataObject new_parent = CreateDataObjectWithBall(parent.level + 1, kv.Key, "", parent);
                    dataObjects.Add(new_parent);

                    JArray arr = JArray.Parse(kv.Value.ToString());

                    foreach (var arr_val in arr)
                    {
                        Debug.Log(kv.Key + "-[REL]->" + arr_val);

                        DataObject new_end_point = CreateDataObjectWithBall(parent.level + 1, arr_val.ToString(), "", parent);
                        dataObjects.Add(new_end_point);
                    }
                } else
                {
                    //End Point
                    Debug.Log(prev + "-[REL]->" + kv.Key + ":" + kv.Value);

                    DataObject new_end_point = CreateDataObjectWithBall(parent.level + 1, kv.Key, kv.Value.ToString(), parent);
                    dataObjects.Add(new_end_point);
                }

            }
            else
            {
                //End Point
                Debug.Log(prev + "-[REL]->" + kv.Key + ":" + kv.Value);

                DataObject new_end_point = CreateDataObjectWithBall(parent.level + 1, kv.Key, kv.Value.ToString(), parent);
                dataObjects.Add(new_end_point);
            }
        }

    }

    public void FuseSameNodes()
    {

        Dictionary<(string,string), List<DataObject>> nodeOccurrences = new Dictionary<(string, string), List<DataObject>>();

        foreach (DataObject node in dataObjects)
        {

            if(nodeOccurrences.ContainsKey((node.key, node.value)))
            {
                nodeOccurrences[(node.key, node.value)].Add(node);
            }
            else
            {
                List<DataObject> list = new List<DataObject>();
                list.Add(node);
                nodeOccurrences.Add((node.key, node.value), list);
            }

        }

        foreach (var item in nodeOccurrences) 
        {
            if (item.Value.Count > 1) 
            {
                DataObject fusedNode = item.Value[0];
                item.Value.RemoveAt(0);

                foreach(DataObject obj in item.Value)
                {
                    //Check children of node
                    foreach(DataObject child in dataObjects)
                    {
                        if (child.parent.Contains(obj) && !child.parent.Contains(fusedNode))
                        {
                            child.parent.Add(fusedNode);
                            child.parent.Remove(obj);
                        }
                    }

                    //Check parents of node
                    obj.parent.ForEach(p => { 
                        if(!fusedNode.parent.Contains(p))
                        {
                            fusedNode.parent.Add(p);
                        } 
                    });
                }

                item.Value.ForEach(obj => {
                    level_occurrences[obj.level]--;
                    Destroy(obj.DataBall);
                    obj.parent.Clear();
                    dataObjects.Remove(obj);
                    });
                
            }

        }
        nodeOccurrences.Clear();

    }

    public void ProcessLevelOccurence(int num)
    {
        // If the number is already in the dictionary, increment its count
        if (level_occurrences.ContainsKey(num))
        {
            level_occurrences[num]++;
        }
        // Otherwise, add it to the dictionary with count 1
        else
        {
            level_occurrences[num] = 1;
        }
    }

    public void PositionDataBalls()
    {

        PositionAsRadialTidyTree();


    }

    public void PositionAsRadialTidyTree()
    {
        //Radius depending on level and number of 3d balls
        int previous_nr_balls = 0;

        foreach (int key in level_occurrences.Keys)
        {
            Debug.Log(key + "-:-" + level_occurrences[key]);

            for (var i = 0; i < dataObjects.Count; i++)
            {
                if (key == dataObjects[i].level)
                {
                    int ballCount = level_occurrences[key] + previous_nr_balls;
                    float angle = (i * Mathf.PI * 2f) / ballCount;
                    Vector3 v = new Vector3(Mathf.Cos(angle) * ((ballCount) + 1.5f * key), 6 + key * 2, Mathf.Sin(angle) * ((ballCount) + 1.5f * key * 1));
                    dataObjects[i].DataBall.transform.position = v;
                    //Debug.Log(dataObjects[i].nr_children);
                }
            }
            previous_nr_balls += level_occurrences[key];
        }

        DrawLinesBetweenDataBalls();
    }

    public void PositionAsForceDirectedGraph()
    {

    }


    public void PositionAsSphere()
    {

        //Position by category and level ocurrence for every layer

        Dictionary<string, List<DataObject>> nodeOccurrences = new Dictionary<string, List<DataObject>>();

        foreach (DataObject dobj  in dataObjects)
        {

            if (nodeOccurrences.ContainsKey(dobj.key))
            {
                nodeOccurrences[dobj.key].Add(dobj);
            }
            else
            {
                List<DataObject> list = new List<DataObject>();
                list.Add(dobj);
                nodeOccurrences.Add(dobj.key, list);
            }
        }

        int layer_level = 0;
        int alternate = 3;

        foreach(int key in level_occurrences.Keys)
        {
            foreach(var obj in nodeOccurrences)
            {

                if (key == obj.Value[0].level)
                {
                    layer_level++;

                    for(var i = 0; i < obj.Value.Count; i++)
                    {
                        if(obj.Value.Count < 2)
                        {
                            int ballCount = obj.Value.Count;
                            float angle = (i * Mathf.PI * 2f) / ballCount;
                            Vector3 v = new Vector3(Mathf.Cos(angle) * ((ballCount)) + alternate, 6 + layer_level * 2, Mathf.Sin(angle) * ((ballCount)));
                            obj.Value[i].DataBall.transform.position = v;
                            alternate = alternate * (-1);
                        }
                        else
                        {
                            int ballCount = obj.Value.Count;
                            float angle = (i * Mathf.PI * 2f) / ballCount;
                            Vector3 v = new Vector3(Mathf.Cos(angle) * ((ballCount)), 6 + layer_level * 2, Mathf.Sin(angle) * ((ballCount)));
                            obj.Value[i].DataBall.transform.position = v;
                        }
                        
                    }

                }
            }
        }

        DrawLinesBetweenDataBalls();
    }

    //Vorgehen: Zuerst benötigten platz für jedes layer berechnen => danach die Bälle positionieren

    public void DrawLinesBetweenDataBalls()
    {

        foreach (DataObject point in dataObjects)
        {
            point.relationship_line_parent.ForEach(line => {
                Destroy(line);
            });

            if (point.parent.Count > 0)
            {

                foreach (DataObject p in point.parent)
                {
                    ld = new LineDrawer(0.04f);
                    List<Vector3> pointlist = new List<Vector3>();
                    pointlist.Add(point.DataBall.transform.position);
                    pointlist.Add(p.DataBall.transform.position);
                    GameObject line = ld.CreateLine(pointlist);

                    point.relationship_line_parent.Add(line);
                }
            }
        }

    }

    public void ColorDataBalls()
    {
        foreach (DataObject ball in dataObjects)
        {

            if (colors.ContainsKey(ball.key))
            {
                colors.TryGetValue(ball.key, out var color);

                ball.DataBall.GetComponentInChildren<Renderer>().material.color = color;
            } 
            else
            {
                UnityEngine.Color c = UnityEngine.Random.ColorHSV();

                ball.DataBall.GetComponentInChildren<Renderer>().material.color = c;
                colors.Add(ball.key, c);
            }

        }
    }
}
