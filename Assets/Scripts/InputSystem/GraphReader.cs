using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Policy;
using TMPro;
using Unity.VisualScripting;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityEngine.XR.Interaction.Toolkit;
using static Unity.Burst.Intrinsics.X86.Sse4_2;
using static UnityEditor.Experimental.AssetDatabaseExperimental.AssetDatabaseCounters;


public class GraphReader
{
    public GameObject BallPrefab;
    public GameObject BoundaryBox;
    public GameObject BoundaryBoxCategories;

    public string dbid;
    public string sbomName;
    public int highest_relationship_count = 0;

    public List<DataObject> dataObjects = new List<DataObject>();
    public Dictionary<int, int> level_occurrences = new Dictionary<int, int>();
    public LineDrawer ld;
    public static Dictionary<string, UnityEngine.Color> colors = new Dictionary<string, UnityEngine.Color>();
    public List<GameObject> categoryBalls = new List<GameObject>();

    public Dictionary<string, int> categoryNumbers = new Dictionary<string, int>();

    public Vector3 offset = new Vector3(0, 0, 0);
    public Vector3 offsetCategories = new Vector3(0, 0, 0);
    public Vector3 graphMin = Vector3.zero;
    public Vector3 graphMax = Vector3.zero;

    public bool isCVE = false;
    public bool isComparisonGraph = false;
    public int lineCounter = 0;
    public int allCategories = 0;
    public string currentGraphType = "";
    public bool glowEnabled = false;

    public List<GameObject> sbomLabels = new List<GameObject>();
    public GameObject categoryPlane;
    public GameObject numberSbomLabel;
    public GameObject numberCategoriesLabel;

    public GameObject verticeMesh;
    public GameObject glowLegend;

    public void CreateGraph(string sbomElement, string graphType, bool showDuplicateNodes)
    {
        Initialization();
        ReadFileAndCreateObjects(sbomElement);
        CountCategoryNumbers();

        if(!showDuplicateNodes)
        {
            FuseSameNodes();
        }

        CountRelationshipsAndDetermineMax();
        PositionDataBalls(graphType);
        ColorDataBalls();
        CreateCategories();
        AddSbomLabel();
        CVEOptimizations();
    }

    public void Initialization()
    {
        foreach (var obj in dataObjects)
        {
            MonoBehaviour.Destroy(obj.DataBall);

            foreach(var parent_line in obj.relationship_line_parent)
            {
                MonoBehaviour.Destroy(parent_line);
            }
        }

        foreach (var categoryBall in categoryBalls)
        {
            MonoBehaviour.Destroy(categoryBall);
        }

        if (BoundaryBox != null)
        {
            MonoBehaviour.Destroy(BoundaryBox);
        }
        if (BoundaryBoxCategories != null)
        {
            MonoBehaviour.Destroy(BoundaryBoxCategories);
        }

        foreach (var label in sbomLabels)
        {
            if (label != null)
            {
                MonoBehaviour.Destroy(label);
            }
        }

        if (categoryPlane != null)
        {
            MonoBehaviour.Destroy(categoryPlane);
        }

        if (numberSbomLabel != null)
        {
            MonoBehaviour.Destroy(numberSbomLabel);
        }

        if (numberCategoriesLabel != null)
        {
            MonoBehaviour.Destroy(numberCategoriesLabel);
        }
        
        if(verticeMesh != null)
        {
            MonoBehaviour.Destroy(verticeMesh);
        }

        if (glowLegend != null)
        {
            MonoBehaviour.Destroy(glowLegend);
        }

        dataObjects.Clear();
        level_occurrences.Clear();
        //colors.Clear();
        categoryBalls.Clear();
        categoryNumbers.Clear();
        lineCounter = 0;
    }

    public void ReadFileAndCreateObjects(string sbomElement)
    {
        //Debug.Log(sbomElement.ToString());
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
        lineCounter++;

        GameObject dataPoint = MonoBehaviour.Instantiate(BallPrefab, new Vector3(1, 1, 1), Quaternion.identity);
        TextMeshPro text = dataPoint.GetComponentInChildren<TextMeshPro>();

        if (value != "")
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

        }

        DataObject newObj = new DataObject(dataPoint, level, key, value, parent, lineCounter);

        if (parent != null)
        {
            parent.children.Add(newObj);
        }

        return newObj;
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

                        string sub_node = kv.Key.ToString().Substring(0, kv.Key.Length) + "-";
                        Debug.Log(kv.Key + "-[REL]->" + sub_node + counter);

                        DataObject new_sub_parent = CreateDataObjectWithBall(new_parent.level + 1, sub_node + counter, "", new_parent);
                        new_sub_parent.suffix = counter.ToString();
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

                    //for invalid characters conbtained
                    try
                    {
                        var dict2 = JsonConvert.DeserializeObject<Dictionary<string, JToken>>(Value);
                        RecursiveRead(dict2, kv.Key, new_parent);
                    }
                    catch(Exception e)
                    {
                        Debug.Log(Value);
                        //Debug.Log(e);
                        var dict2 = JsonConvert.DeserializeObject<Dictionary<string, JToken>>("{"+"\"Contained Invalid Character\":\"\""+"}");
                        RecursiveRead(dict2, kv.Key, new_parent);
                    }
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

    public void CountCategoryNumbers()
    {
        foreach (DataObject obj in dataObjects) 
        { 
            if(categoryNumbers.ContainsKey(obj.key) || categoryNumbers.ContainsKey(obj.key.Substring(0, obj.key.Length - obj.suffix.Length)))
            {
                categoryNumbers[obj.key.Substring(0, obj.key.Length - obj.suffix.Length)]++;
            }
            else
            {
                categoryNumbers.Add(obj.key.Substring(0, obj.key.Length - obj.suffix.Length), 1);
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
                        if (child.parent.Contains(obj))
                        {
                            if(!child.parent.Contains(fusedNode))
                            {
                                child.parent.Add(fusedNode);
                            }
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
                    MonoBehaviour.Destroy(obj.DataBall);
                    obj.parent.Clear();
                    obj.relationship_line_parent.ForEach(line => { MonoBehaviour.Destroy(line); });
                    obj.relationship_line_parent.Clear();
                    dataObjects.Remove(obj);
                });
                
            }

        }
        nodeOccurrences.Clear();
    }

    public void ProcessLevelOccurence(int num)
    {
        if (level_occurrences.ContainsKey(num))
        {
            level_occurrences[num]++;
        }
        else
        {
            level_occurrences[num] = 1;
        }
    }

    public void PositionDataBalls(string type)
    {
        currentGraphType = type;

        switch (type)
        {
            case "Radial Tidy Tree":
                PositionAsRadialTidyTree();
                break;

            case "Stacking Radial Tree":
                PositionAsStackingRadialTidyTreeNoOverlap();
                break;

            case "Force-directed Graph":
                PositionAsForceDirectedGraph();
                break;

            case "Sphere":
                PositionOnSphere();
                break;

            case "Category And Level":
                PositionByCategoryAndLevel();
                break;
        }

        DrawLinesBetweenDataBalls();

        MakeGraphBoundaries();

        CreateGlowCubeLegend();

        //MeshCombiner();
    }

    public void PositionAsRadialTidyTree()
    {

        float ballDiameter = 1f;
        float previousRadius = ballDiameter / 2f;
        int incHeight = 0;

        foreach (int key in level_occurrences.Keys)
        {
            int numberOfBalls = level_occurrences[key];
            int counter = 0;
            float radius = 1f;

            if((2 * Mathf.Sin(Mathf.PI / numberOfBalls)) > 0)
            {
               radius = previousRadius + (ballDiameter / (2 * Mathf.Sin(Mathf.PI / numberOfBalls)));
            }

            for (int i = 0; i < dataObjects.Count; i++)
            {
                if (key == dataObjects[i].level)
                {
                    float angle = counter * Mathf.PI * 2 / numberOfBalls;
                    float x = Mathf.Cos(angle) * radius;
                    float z = Mathf.Sin(angle) * radius;

                    Vector3 position = new Vector3(x, 0 + incHeight, z); 

                    dataObjects[i].DataBall.transform.position = position;
                    counter++;
                }

            }
            incHeight += 2;
            previousRadius = radius + ballDiameter;
        }
    }

    public void PositionAsStackingRadialTidyTree()
    {
        float ballDiameter = 1.1f;
        float previousRadius = ballDiameter / 2f;
        int incHeight = 0;

        foreach (int key in level_occurrences.Keys)
        {
            int numberOfBalls = level_occurrences[key];
            int counter = 0;
            float radius = 1f;
            int balls = numberOfBalls;

            if (numberOfBalls > 50) 
            {
                balls = 50;

                if ((2 * Mathf.Sin(Mathf.PI / balls)) > 0)
                {
                    radius = previousRadius + (ballDiameter / (2 * Mathf.Sin(Mathf.PI / balls)));
                }
            } 
            else
            {
                if ((2 * Mathf.Sin(Mathf.PI / numberOfBalls)) > 0)
                {
                    radius = previousRadius + (ballDiameter / (2 * Mathf.Sin(Mathf.PI / numberOfBalls)));
                }
            }

            for (int i = 0; i < dataObjects.Count; i++)
            {
                if (key == dataObjects[i].level)
                {
                    float angle = counter * Mathf.PI * 2 / balls;
                    float x = Mathf.Cos(angle) * radius;
                    float z = Mathf.Sin(angle) * radius;

                    Vector3 position = new Vector3(x, 0 + incHeight + (counter/49)*2, z);

                    dataObjects[i].DataBall.transform.position = position;
                    counter++;
                }

            }
            incHeight += 2;
            previousRadius = radius + ballDiameter;
        }
    }

    
    public void PositionAsStackingRadialTidyTreeNoOverlap()
    {
        
        float ballDiameter = 1f;
        float previousRadius = ballDiameter / 2f;
        int incHeight = 0;
        float maximumBallsPerLevel = 60;

        foreach (int key in level_occurrences.Keys)
        {
            List<Vector3> positionsV = new List<Vector3>();
            Dictionary<DataObject, List<DataObject>> parentChildrenPairs = new Dictionary<DataObject, List<DataObject>>();

            int numberOfLayers = Mathf.CeilToInt(level_occurrences[key] / maximumBallsPerLevel);
            int numberOfBalls = level_occurrences[key];
            float radius = 1f;
            int numberOfChildrenBalls = 1;

            if (level_occurrences.ContainsKey(key+1))
            {
                numberOfChildrenBalls = level_occurrences[key + 1];
            }
            
            if ((2 * Mathf.Sin(Mathf.PI / numberOfBalls)) > 0)
            {
                radius = previousRadius + (ballDiameter / (2 * Mathf.Sin(Mathf.PI / numberOfBalls)));

                if (numberOfBalls > maximumBallsPerLevel)
                {
                    radius = previousRadius + (ballDiameter / (2 * Mathf.Sin(Mathf.PI / maximumBallsPerLevel)));
                }
            }

            int ballCount = Mathf.FloorToInt(2 * Mathf.PI * radius / ballDiameter);
            float angleIncrement = 360f / ballCount;


            for(int h=0;h<numberOfLayers;h++)
            {
                //Position Pre-Save
                for (int i = 0; i < ballCount; i++)
                {
                    //float angle = i * 2 * Mathf.PI / numberOfBalls;

                    float angleInDegrees = i * angleIncrement;
                    float angle = angleInDegrees * Mathf.Deg2Rad;

                    float x = Mathf.Cos(angle) * radius;
                    float z = Mathf.Sin(angle) * radius;

                    Vector3 position = new Vector3(x, 0 - (incHeight + h*2), z);

                    positionsV.Add(position);
                }
            }

            int numberPositions = positionsV.Count;
            //int NodesWithoutChild = 0;
            //List<DataObject> nodesChecked = new List<DataObject>();

            //Count Parents with Children
            for (int i = 0; i < dataObjects.Count; i++)
            {
                if (key == dataObjects[i].level)
                { 
                    if(dataObjects[i].parent.Count == 0)
                    {
                        parentChildrenPairs.Add(dataObjects[i], new List<DataObject> { dataObjects[i] });
                        break;
                    }

                    if (parentChildrenPairs.ContainsKey(dataObjects[i].parent[0]))
                    {
                        parentChildrenPairs[dataObjects[i].parent[0]].Add(dataObjects[i]);
                    }
                    else
                    {
                        parentChildrenPairs.Add(dataObjects[i].parent[0], new List<DataObject> { dataObjects[i] });
                    }

                    /*
                    if (dataObjects[i].nr_children == 0)
                    {
                        NodesWithoutChild++;
                        nodesChecked.Add(dataObjects[i]);
                    }
                    */

                }
            }

            /*
            for (int i = 0; i < dataObjects.Count; i++)
            {
                if (key == dataObjects[i].level && !nodesChecked.Contains(dataObjects[i]))
                {
                    float ballsFraction = (float)dataObjects[i].nr_children / ((float)(numberOfChildrenBalls));
                    int numberClaimedPositions = (int)(ballsFraction * (numberPositions - NodesWithoutChild));
                        
                    if(numberClaimedPositions == 0)
                    {
                        NodesWithoutChild++;
                    }
                }
            }
            */

            var sortedparentChildrenDict = parentChildrenPairs.OrderByDescending(kvp => kvp.Value.Count).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            foreach (var keyValuePair in sortedparentChildrenDict) 
            {
                var parentChildsPair = keyValuePair.Value.OrderByDescending(kvp => kvp.nr_children).ToList();

                foreach (DataObject dobj in parentChildsPair)
                {
                    
                    Vector3 closestPosition = Vector3.zero;
                    float shortestDistance = Mathf.Infinity;

                    foreach (Vector3 position in positionsV)
                    {
                        if (dobj.parent.Count == 0)
                        {
                            //ROOT node
                            closestPosition = position;
                        }
                        else if (dobj.parent.Count == 1)
                        {
                            //one parent
                            float distance = Vector3.Distance(dobj.parent[0].DataBall.transform.position, position);

                            if (distance < shortestDistance)
                            {
                                shortestDistance = distance;
                                closestPosition = position;
                            }
                        }
                        else
                        {
                            //mutliple parents
                            float distance = 0;

                            foreach (var parent in dobj.parent)
                            {
                                distance += Vector3.Distance(parent.DataBall.transform.position, position);
                            }

                            if (distance / dobj.parent.Count < shortestDistance)
                            {
                                shortestDistance = distance / dobj.parent.Count;
                                closestPosition = position;
                            }
                        }
                    }

                    dobj.DataBall.transform.position = closestPosition;



                    //TODO: Fix missing positions (maybe numberClaimedPositions too high)
                    float ballsFraction = (float)dobj.nr_children / ((float)(numberOfChildrenBalls));
                    int numberClaimedPositions = (int)(ballsFraction * (numberPositions - numberOfBalls));

                    if (numberClaimedPositions == 0)
                    {
                        numberClaimedPositions = 1;
                    }
                    /*
                    Debug.Log(dobj.key);
                    Debug.Log("POSITIONS:" + numberPositions);
                    Debug.Log("LAYER:" + key);
                    Debug.Log("Fraction: " + ballsFraction);
                    Debug.Log("numberPositions - NodesWithoutChild: " + (numberPositions - NodesWithoutChild));
                    Debug.Log("numberClaimedPositions: " + numberClaimedPositions);
                    Debug.Log("numberPositions: " + numberPositions);
                    */

                    positionsV = positionsV.OrderBy(pos => Vector3.Distance(pos, closestPosition)).ToList();

                    positionsV.RemoveRange(0, Math.Min(numberClaimedPositions, positionsV.Count));
                }

            }

            incHeight += 2;
            previousRadius = radius + ballDiameter;
        }
    }


    public void PositionAsForceDirectedGraph()
    {

        float attractionForce = 0.8f;
        float repulsionForce = 1f; //higher for more repulsion between nodes
        float damping = 0.9f;

        foreach (DataObject dobj in dataObjects) 
        {
            dobj.DataBall.transform.position = UnityEngine.Random.insideUnitSphere * 10; //higher value for bigger spawn radius
        }

        foreach (DataObject node in dataObjects)
        {
            Vector3 force = Vector3.zero;

            foreach (DataObject other in dataObjects)
            {
                if (node != other && !other.parent.Contains(node) && !node.parent.Contains(other))
                {
                    Vector3 direction = node.DataBall.transform.position - other.DataBall.transform.position;
                    float distance = direction.magnitude;

                    if (distance == 0)
                    {
                        distance = 1;
                    }

                    force += direction.normalized * repulsionForce / (distance * distance);
                }

                if(node != other && other.parent.Contains(node) || node.parent.Contains(other))
                {

                    Vector3 direction = other.DataBall.transform.position - node.DataBall.transform.position;
                    float distance = direction.magnitude;
                    force += direction.normalized * attractionForce * distance;
                }

            }

            node.velocity = (node.velocity + force) * damping;   
            
            /*
            //added force for children
            foreach(DataObject other in dataObjects) 
            {
                if(other.parent.Contains(node))
                {
                    other.velocity = (node.velocity + force * damping);
                }
            }
            */
        }

        AddVelocityToNodes();
    }

    public void AddVelocityToNodes()
    {
        foreach (DataObject node in dataObjects)
        {
            node.DataBall.transform.position += node.velocity;
            node.DataBall.transform.position = Vector3.ClampMagnitude(node.DataBall.transform.position, 100f + Mathf.Sqrt(dataObjects.Count));
        }
    }

    public void PositionByCategoryAndLevel()
    {

        //Position by category and level ocurrence for every layer
        Dictionary<string, List<DataObject>> nodeOccurrences = new Dictionary<string, List<DataObject>>();

        foreach (DataObject dobj  in dataObjects)
        {

            if (nodeOccurrences.ContainsKey(dobj.key) || nodeOccurrences.ContainsKey(dobj.key.Substring(0, dobj.key.Length - dobj.suffix.Length)))
            {
                nodeOccurrences[dobj.key.Substring(0, dobj.key.Length - dobj.suffix.Length)].Add(dobj);
            }
            else
            {
                List<DataObject> list = new List<DataObject>();
                list.Add(dobj);
                nodeOccurrences.Add(dobj.key.Substring(0, dobj.key.Length - dobj.suffix.Length), list);
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
                    layer_level += 2;

                    for(var i = 0; i < obj.Value.Count; i++)
                    {
                        float radius = 1;

                        if (Mathf.Sin(Mathf.PI / obj.Value.Count) > 0)
                        {
                            radius = 0.75f / Mathf.Sin(Mathf.PI / obj.Value.Count);
                        }

                        if (obj.Value.Count < 2)
                        {
                            int ballCount = obj.Value.Count;
                            float angle = (i * Mathf.PI * 2f) / ballCount;
                            Vector3 v = new Vector3(Mathf.Cos(angle) * radius + alternate, 0 + layer_level, Mathf.Sin(angle) * radius);
                            obj.Value[i].DataBall.transform.position = v;

                            alternate = alternate * (-1);
                        }
                        else
                        {
                            int ballCount = obj.Value.Count;
                            float angle = (i * Mathf.PI * 2f) / ballCount;
                            Vector3 v = new Vector3(Mathf.Cos(angle) * radius, 0 + layer_level, Mathf.Sin(angle) * radius);
                            obj.Value[i].DataBall.transform.position = v;
                        }
                        
                    }

                }
            }
        }
    }

    public void PositionOnSphere()
    {
        float phi = Mathf.PI * (3 - Mathf.Sqrt(5));

        float sphereRadius = 2.0f * 0.5f * Mathf.Sqrt(dataObjects.Count); // spacing * ball radius * number of balls

        for (int i = 0; i < dataObjects.Count; i++)
        {
            float y = 1 - (i / (float)(dataObjects.Count - 1)) * 2; 
            float radiusAtY = Mathf.Sqrt(1 - y * y); 

            float theta = phi * i; 

            float x = Mathf.Cos(theta) * radiusAtY;
            float z = Mathf.Sin(theta) * radiusAtY;

            Vector3 position = (new Vector3(x, y, z) * sphereRadius);


            dataObjects[i].DataBall.transform.position = position;
        }
    }

    public void AdjustEntireGraphPosition(Vector3 position)
    {
        foreach (var obj in dataObjects)
        {
            // move data balls
            obj.DataBall.transform.position += position;

            // move lines
            foreach (var line in obj.relationship_line_parent)
            {
                LineRenderer lineR = line.GetComponent<LineRenderer>();
                int posCount = lineR.positionCount;
                Vector3[] pos = new Vector3[posCount];
                lineR.GetPositions(pos);

                for (int i = 0; i < posCount; i++)
                {
                    pos[i] += position;
                }
                lineR.SetPositions(pos);
            }
        }

        //Move boundary box pos
        MoveBoundaryBox(BoundaryBox, position);

        //Move category boundary Box pos
        MoveBoundaryBox(BoundaryBoxCategories, position);


        //Set Label Pos 1
        sbomLabels[0].transform.position = new Vector3(BoundaryBox.GetComponent<Renderer>().bounds.min.x + 0.5f,
            BoundaryBox.GetComponent<Renderer>().bounds.min.y, (BoundaryBox.GetComponent<Renderer>().bounds.max.z + BoundaryBox.GetComponent<Renderer>().bounds.min.z) / 2);

        AdjustTextFontSize(sbomLabels[0], Mathf.Abs(BoundaryBox.GetComponent<Renderer>().bounds.max.z - BoundaryBox.GetComponent<Renderer>().bounds.min.z));

        
        //Set Label Pos 2
        sbomLabels[1].transform.position = new Vector3((BoundaryBox.GetComponent<Renderer>().bounds.max.x + BoundaryBox.GetComponent<Renderer>().bounds.min.x) / 2,
            BoundaryBox.GetComponent<Renderer>().bounds.min.y, BoundaryBox.GetComponent<Renderer>().bounds.min.z + 0.5f);

        AdjustTextFontSize(sbomLabels[1], Mathf.Abs(BoundaryBox.GetComponent<Renderer>().bounds.max.x - BoundaryBox.GetComponent<Renderer>().bounds.min.x));


        //Set Label Pos 3
        sbomLabels[2].transform.position = new Vector3(BoundaryBox.GetComponent<Renderer>().bounds.max.x - 0.5f,
            BoundaryBox.GetComponent<Renderer>().bounds.min.y, (BoundaryBox.GetComponent<Renderer>().bounds.max.z + BoundaryBox.GetComponent<Renderer>().bounds.min.z) / 2);

        AdjustTextFontSize(sbomLabels[2], Mathf.Abs(BoundaryBox.GetComponent<Renderer>().bounds.max.z - BoundaryBox.GetComponent<Renderer>().bounds.min.z));


        //Set Label Pos 4
        sbomLabels[3].transform.position = new Vector3((BoundaryBox.GetComponent<Renderer>().bounds.max.x + BoundaryBox.GetComponent<Renderer>().bounds.min.x) / 2,
            BoundaryBox.GetComponent<Renderer>().bounds.min.y, BoundaryBox.GetComponent<Renderer>().bounds.max.z - 0.5f);

        AdjustTextFontSize(sbomLabels[3], Mathf.Abs(BoundaryBox.GetComponent<Renderer>().bounds.max.x - BoundaryBox.GetComponent<Renderer>().bounds.min.x));


        if (isCVE)
        {
            //Set Number sbom and category labels for CVE
            AdjustNumberTextFontSize(numberSbomLabel, Math.Max(6, Math.Min(14, sbomLabels[0].GetComponent<TextMeshPro>().fontSize / 2)));

            numberSbomLabel.transform.position = new Vector3(BoundaryBox.GetComponent<Renderer>().bounds.max.x - 0.5f,
                BoundaryBox.GetComponent<Renderer>().bounds.max.y - 1.1f, BoundaryBox.GetComponent<Renderer>().bounds.min.z + numberSbomLabel.GetComponent<TextMeshPro>().preferredWidth);

            AdjustNumberTextFontSize(numberCategoriesLabel, Math.Max(6, Math.Min(14, sbomLabels[0].GetComponent<TextMeshPro>().fontSize / 2)));

            numberCategoriesLabel.transform.position = new Vector3(BoundaryBoxCategories.GetComponent<Renderer>().bounds.max.x - 0.5f,
                BoundaryBoxCategories.GetComponent<Renderer>().bounds.min.y + 1.1f, BoundaryBoxCategories.GetComponent<Renderer>().bounds.min.z + numberSbomLabel.GetComponent<TextMeshPro>().preferredWidth);
        }
        else
        {
            //Set Number sbom and category labels 
            numberSbomLabel.transform.position = new Vector3(BoundaryBox.GetComponent<Renderer>().bounds.min.x + 0.5f,
                BoundaryBox.GetComponent<Renderer>().bounds.max.y - 1.1f, BoundaryBox.GetComponent<Renderer>().bounds.max.z);

            AdjustNumberTextFontSize(numberSbomLabel, Math.Max(6, Math.Min(14, sbomLabels[0].GetComponent<TextMeshPro>().fontSize / 2)));

            numberCategoriesLabel.transform.position = new Vector3(BoundaryBoxCategories.GetComponent<Renderer>().bounds.min.x + 0.5f,
                BoundaryBoxCategories.GetComponent<Renderer>().bounds.min.y + 1.1f, BoundaryBoxCategories.GetComponent<Renderer>().bounds.max.z);

            AdjustNumberTextFontSize(numberCategoriesLabel, Math.Max(6, Math.Min(14, sbomLabels[0].GetComponent<TextMeshPro>().fontSize / 2)));
        }

        //Move Plane
        MovePlanePosition(position);

        //verticeMesh.transform.position += position;

        CreateGlowCubeLegend();
    }

    public void MoveBoundaryBox(GameObject box, Vector3 position)
    {
        LineRenderer cubeRenderer = box.GetComponent<LineRenderer>();

        int positionCount = cubeRenderer.positionCount;
        Vector3[] positions = new Vector3[positionCount];
        cubeRenderer.GetPositions(positions);

        for (int i = 0; i < positionCount; i++)
        {
            positions[i] += position;
        }

        cubeRenderer.SetPositions(positions);
    }

    public void MovePlanePosition(Vector3 position) 
    {
        categoryPlane.transform.position += position;
    }


    public void DrawLinesBetweenDataBalls()
    {
        ld = new LineDrawer(0.04f);
        foreach (DataObject point in dataObjects)
        {
            point.relationship_line_parent.ForEach(line => {
                MonoBehaviour.Destroy(line);
            });
            point.relationship_line_parent.Clear();

            if (point.parent.Count > 0)
            {

                foreach (DataObject p in point.parent)
                {
                    List<Vector3> pointlist = new List<Vector3>();
                    pointlist.Add(point.DataBall.transform.position);
                    pointlist.Add(p.DataBall.transform.position);
                    GameObject line = ld.CreateLine(pointlist, isCVE);

                    point.relationship_line_parent.Add(line);
                }
            }
        }

    }

    public bool HasChildrens(DataObject parent)
    {
        foreach (DataObject point in dataObjects)
        {
            if(point.parent.Contains(parent))
            {
                return true;
            }
        }

        return false;
    }

    public void ColorDataBalls()
    {

        foreach (DataObject ball in dataObjects)
        {

            if (colors.ContainsKey(ball.key) || colors.ContainsKey(ball.key.Substring(0, ball.key.Length - ball.suffix.Length)))
            {
                colors.TryGetValue(ball.key.Substring(0, ball.key.Length - ball.suffix.Length), out var color);
                ball.DataBall.GetComponentInChildren<Renderer>().sharedMaterial.color = color;
            } 
            else
            {

                //UnityEngine.Color c = UnityEngine.Random.ColorHSV();
                UnityEngine.Color c = UnityEngine.Color.HSVToRGB(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0.1f, 1f), UnityEngine.Random.Range(0.2f, 1f));

                // loop to lower chances for duplicate color
                for (int i = 0; i < 10; i++)
                {

                    if (!colors.Values.Contains(c))
                    {
                        break;
                    }

                    c = UnityEngine.Color.HSVToRGB(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0.1f, 1f), UnityEngine.Random.Range(0.2f, 1f));
                }
                ball.DataBall.GetComponentInChildren<Renderer>().material.color = c;
                colors.Add(ball.key.Substring(0, ball.key.Length - ball.suffix.Length), c);

            }

        }
    }

    public void CreateCategories()
    {
        Dictionary<string, UnityEngine.Color> colorsLocal = new Dictionary<string, UnityEngine.Color>();
        
        foreach (DataObject ball in dataObjects) {
            if(colors.ContainsKey(ball.key) || colors.ContainsKey(ball.key.Substring(0, ball.key.Length - ball.suffix.Length)))
            {
                if (!colorsLocal.ContainsKey(ball.key) && !colorsLocal.ContainsKey(ball.key.Substring(0, ball.key.Length - ball.suffix.Length)))
                {
                    
                    if (ball.key != "ROOT" && ball.value == "" && !HasChildrens(ball))
                    {
                        continue;
                    }

                    colorsLocal.Add(ball.key.Substring(0, ball.key.Length - ball.suffix.Length), colors[ball.key.Substring(0, ball.key.Length - ball.suffix.Length)]);
                }
            } 
        }

        allCategories = colorsLocal.Count;
        int columns = (int)Mathf.Sqrt(colorsLocal.Count);
        int rows = Mathf.CeilToInt(colorsLocal.Count / (float)columns);

        float sideX = Mathf.Abs(BoundaryBox.GetComponent<Renderer>().bounds.max.x - BoundaryBox.GetComponent<Renderer>().bounds.min.x);
        float sideZ = Mathf.Abs(BoundaryBox.GetComponent<Renderer>().bounds.max.z - BoundaryBox.GetComponent<Renderer>().bounds.min.z);

        //Debug.Log(colors.Count);

        int mostAppearance = 0;

        foreach((string key, int val) in categoryNumbers)
        {
            if (val > mostAppearance) mostAppearance = val;
        }

        int indexCount = 0;
        int loopCount = 0;

        float addX = Mathf.Max((sideX / columns), 1);
        float addZ = Mathf.Max((sideZ / rows), 1);

        Vector3 start = CalculateCategoryStartPoint();

        while (indexCount < colorsLocal.Count)
        {
            for (int x = 0; x < columns; x++)
            {
                for (int z = 0; z < rows; z++)
                {
                    if(indexCount < colorsLocal.Count)
                    {

                        Vector3 v = new Vector3(start.x - x * Mathf.Min(addX,2), start.y + loopCount, start.z + z * Mathf.Min(addZ, 2));

                        if (BoundaryBox.GetComponent<Renderer>().bounds.min.x + 1f > v.x|| BoundaryBox.GetComponent<Renderer>().bounds.max.z - 1f < v.z)
                        {
                            continue;
                        }

                        GameObject categoryPoint = MonoBehaviour.Instantiate(BallPrefab, v, Quaternion.identity);

                        //GameObject categoryPoint = MonoBehaviour.Instantiate(BallPrefab, new Vector3(1 - x, 0, 2 + z + ((x%2) * 0.25f)), Quaternion.identity);
                        TextMeshPro text = categoryPoint.GetComponentInChildren<TextMeshPro>();

                        //Calculate Size of Category Ball
                        text.text = colorsLocal.ElementAt(indexCount).Key;
                        categoryPoint.GetComponentInChildren<Renderer>().material.color = colorsLocal.ElementAt(indexCount).Value;

                        float relevance = (categoryNumbers[text.text] / (float)mostAppearance) / 2.5f;
                        categoryPoint.transform.localScale = new Vector3(0.5f + relevance, 0.5f + relevance, 0.5f + relevance);
                        categoryPoint.transform.localRotation = Quaternion.Euler(0, 90, 0);

                        categoryBalls.Add(categoryPoint);

                        indexCount++;
                    }
                }
            }
            loopCount++;
        }


        MakeCategoryBoundaries(loopCount);

      
        //Make Floor to walk for category inspection
        GameObject prefab = Resources.Load<GameObject>("PlaneFloor");
        categoryPlane = MonoBehaviour.Instantiate(prefab, Vector3.zero, Quaternion.identity);
        categoryPlane.transform.localScale = new Vector3((sideX - 1f) / 10f, 0.1f, (sideZ - 1f) / 10f);
        categoryPlane.transform.position = new Vector3(BoundaryBoxCategories.GetComponent<Renderer>().bounds.max.x - (sideX/2),
            BoundaryBoxCategories.GetComponent<Renderer>().bounds.min.y + 0.5f, BoundaryBoxCategories.GetComponent<Renderer>().bounds.max.z - (sideZ)/ 2);
        //plane.GetComponent<MeshRenderer>().enabled = false;
       
    }

    public Vector3 CalculateCategoryStartPoint()
    {

        float sideX = Mathf.Abs(BoundaryBox.GetComponent<Renderer>().bounds.max.x - BoundaryBox.GetComponent<Renderer>().bounds.min.x);
        float sideZ = Mathf.Abs(BoundaryBox.GetComponent<Renderer>().bounds.max.z - BoundaryBox.GetComponent<Renderer>().bounds.min.z);

        int columns = (int)Mathf.Sqrt(allCategories);
        int rows = Mathf.CeilToInt(allCategories / (float)columns);

        float addX = Mathf.Max((sideX / columns), 1);
        float addZ = Mathf.Max((sideZ / rows), 1);


        while ((Mathf.Min(addZ, 2) * rows) > sideZ)
        {
            rows--;
        }
        while ((Mathf.Min(addX, 2) * columns) > sideX)
        {
            columns--;
        }


        float startZ = BoundaryBox.GetComponent<Renderer>().bounds.min.z + (sideZ - (Mathf.Min(addZ, 2) * rows)) / 2;
        float startX = BoundaryBox.GetComponent<Renderer>().bounds.max.x - (sideX - (Mathf.Min(addX, 2) * columns)) / 2;

        Vector3 start = new Vector3(startX - 1f, BoundaryBox.GetComponent<Renderer>().bounds.max.y + 1, startZ + 1f);

        return start;
    }

    public void MakeCategoryBoundaries(int addedHeight)
    {
        if (BoundaryBoxCategories != null)
        {
            MonoBehaviour.Destroy(BoundaryBoxCategories);
        }

        BoundaryBoxCategories = ld.DrawCube(
            new Vector3(BoundaryBox.GetComponent<Renderer>().bounds.min.x, BoundaryBox.GetComponent<Renderer>().bounds.max.y + 0.5f, BoundaryBox.GetComponent<Renderer>().bounds.min.z) + new Vector3(0.5f, 0, 0.5f),
            new Vector3(BoundaryBox.GetComponent<Renderer>().bounds.max.x, BoundaryBox.GetComponent<Renderer>().bounds.max.y + 1 + addedHeight, BoundaryBox.GetComponent<Renderer>().bounds.max.z) + new Vector3(-0.5f, 0, -0.5f));

    }

    public void MakeGraphBoundaries()
    {
        if(BoundaryBox != null)
        {
            MonoBehaviour.Destroy(BoundaryBox);
        }

        if (dataObjects.Count > 0)
        {
            graphMin = dataObjects[0].DataBall.transform.position;
            graphMax = dataObjects[0].DataBall.transform.position;

            foreach (DataObject point in dataObjects)
            {
                Vector3 pos = point.DataBall.transform.position;

                // Update the min bounds
                if (pos.x < graphMin.x) graphMin.x = pos.x;
                if (pos.y < graphMin.y) graphMin.y = pos.y;
                if (pos.z < graphMin.z) graphMin.z = pos.z;

                // Update the max bounds
                if (pos.x > graphMax.x) graphMax.x = pos.x;
                if (pos.y > graphMax.y) graphMax.y = pos.y;
                if (pos.z > graphMax.z) graphMax.z = pos.z;
            }

            BoundaryBox = ld.DrawCube(graphMin + new Vector3(-1,-1,-1), graphMax + new Vector3(1, 1, 1));
        }
    }

    public void AddSbomLabel()
    {
        string nameText = "";

        if (sbomName != "")
        {
            nameText = sbomName;
        }
        else
        {
            nameText = dbid;
        }

        if (isCVE)
        {
            foreach (DataObject point in dataObjects)
            {
                if(point.key == "CVE-ID")
                {
                    nameText = point.value;
                }
            }
        }

        //Name labels
        sbomLabels.Add(
            CreateSbomlabel(new Vector3(
            BoundaryBox.GetComponent<Renderer>().bounds.min.x + 0.5f,
            BoundaryBox.GetComponent<Renderer>().bounds.min.y,
            (BoundaryBox.GetComponent<Renderer>().bounds.max.z + BoundaryBox.GetComponent<Renderer>().bounds.min.z) / 2),
            Mathf.Abs(BoundaryBox.GetComponent<Renderer>().bounds.max.z - BoundaryBox.GetComponent<Renderer>().bounds.min.z), Quaternion.Euler(0, 90, 0), nameText));


        sbomLabels.Add(
            CreateSbomlabel(new Vector3(
            (BoundaryBox.GetComponent<Renderer>().bounds.max.x + BoundaryBox.GetComponent<Renderer>().bounds.min.x) / 2,
            BoundaryBox.GetComponent<Renderer>().bounds.min.y,
            BoundaryBox.GetComponent<Renderer>().bounds.min.z + 0.5f),
            Mathf.Abs(BoundaryBox.GetComponent<Renderer>().bounds.max.x - BoundaryBox.GetComponent<Renderer>().bounds.min.x), Quaternion.Euler(0, 0, 0), nameText));

        sbomLabels.Add(
            CreateSbomlabel(new Vector3(
            BoundaryBox.GetComponent<Renderer>().bounds.max.x - 0.5f,
            BoundaryBox.GetComponent<Renderer>().bounds.min.y,
            (BoundaryBox.GetComponent<Renderer>().bounds.max.z + BoundaryBox.GetComponent<Renderer>().bounds.min.z) / 2),
            Mathf.Abs(BoundaryBox.GetComponent<Renderer>().bounds.max.z - BoundaryBox.GetComponent<Renderer>().bounds.min.z), Quaternion.Euler(0, -90, 0), nameText));

        sbomLabels.Add(
            CreateSbomlabel(new Vector3(
            (BoundaryBox.GetComponent<Renderer>().bounds.max.x + BoundaryBox.GetComponent<Renderer>().bounds.min.x) / 2,
            BoundaryBox.GetComponent<Renderer>().bounds.min.y,
            BoundaryBox.GetComponent<Renderer>().bounds.max.z - 0.5f),
            Mathf.Abs(BoundaryBox.GetComponent<Renderer>().bounds.max.x - BoundaryBox.GetComponent<Renderer>().bounds.min.x), Quaternion.Euler(0, 0, 0), nameText));

        
        if(isCVE)
        {
            //Number labels
            numberSbomLabel = CreateNumberLabel(new Vector3(
            BoundaryBox.GetComponent<Renderer>().bounds.max.x - 0.5f,
            BoundaryBox.GetComponent<Renderer>().bounds.max.y - 1.1f,
            BoundaryBox.GetComponent<Renderer>().bounds.min.z), Quaternion.Euler(0, 90, 0), "Nodes: " + dataObjects.Count);

            numberCategoriesLabel = CreateNumberLabel(new Vector3(
            BoundaryBoxCategories.GetComponent<Renderer>().bounds.max.x - 0.5f,
            BoundaryBoxCategories.GetComponent<Renderer>().bounds.min.y + 1.1f,
            BoundaryBoxCategories.GetComponent<Renderer>().bounds.min.z), Quaternion.Euler(0, 90, 0), "Nodes: " + allCategories);
        }
        else
        {
            //Number labels
            numberSbomLabel = CreateNumberLabel(new Vector3(
            BoundaryBox.GetComponent<Renderer>().bounds.min.x + 0.5f,
            BoundaryBox.GetComponent<Renderer>().bounds.max.y - 1.1f,
            BoundaryBox.GetComponent<Renderer>().bounds.max.z), Quaternion.Euler(0, 90, 0), "Nodes: " + dataObjects.Count);

            numberCategoriesLabel = CreateNumberLabel(new Vector3(
            BoundaryBoxCategories.GetComponent<Renderer>().bounds.min.x + 0.5f,
            BoundaryBoxCategories.GetComponent<Renderer>().bounds.min.y + 1.1f,
            BoundaryBoxCategories.GetComponent<Renderer>().bounds.max.z), Quaternion.Euler(0, 90, 0), "Nodes: " + allCategories);
        }
    }

    public GameObject CreateSbomlabel(Vector3 position, float fontSizeValue, Quaternion rotation, string name)
    {
        GameObject sbomLabel = new GameObject();

        TextMeshPro sbomLabelText = sbomLabel.AddComponent<TextMeshPro>();
        sbomLabel.transform.localRotation = rotation;
        sbomLabel.transform.localPosition = position;

        ContentSizeFitter contentFitter = sbomLabel.AddComponent<ContentSizeFitter>();
        contentFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        sbomLabelText.text = name;

        sbomLabelText.fontSize = 8;
        sbomLabelText.alignment = TextAlignmentOptions.Center;
        sbomLabelText.color = new UnityEngine.Color(0.1f, 0.1f, 0.1f, 0.7f);

        AdjustTextFontSize(sbomLabel, fontSizeValue);

        return sbomLabel;
    }

    public GameObject CreateNumberLabel(Vector3 position, Quaternion rotation, string name)
    {
        GameObject sbomLabel = new GameObject();

        TextMeshPro sbomLabelText = sbomLabel.AddComponent<TextMeshPro>();
        sbomLabel.transform.localRotation = rotation;
        sbomLabel.transform.localPosition = position;

        sbomLabelText.text = name;
        AdjustNumberTextFontSize(sbomLabel, Math.Max(6, Math.Min(14, sbomLabels[0].GetComponent<TextMeshPro>().fontSize / 2)));
        sbomLabelText.alignment = TextAlignmentOptions.Center;
        sbomLabelText.color = new UnityEngine.Color(0.1f, 0.1f, 0.1f, 0.7f);

        //Debug.Log(" Object position:" + sbomLabel.transform.localPosition);

        sbomLabel.transform.localPosition += new Vector3(0,0, -sbomLabelText.preferredWidth / 2 - 1);

        return sbomLabel;
    }

    public void AdjustNumberTextFontSize(GameObject label, float fontsize)
    {
        TextMeshPro sbomLabelText = label.GetComponent<TextMeshPro>();
        sbomLabelText.fontSize = fontsize;

        label.transform.localPosition += new Vector3(0, 0, -sbomLabelText.preferredWidth / 2 - 1);
    }

    public void AdjustTextFontSize(GameObject label, float fontSizeValue)
    {
        TextMeshPro sbomLabelText = label.GetComponent<TextMeshPro>();
        float value = fontSizeValue;

        while (true)
        {
            if (value - sbomLabelText.preferredWidth < 1)
            {
                if (sbomLabelText.fontSize == 1) break;

                sbomLabelText.fontSize -= 0.2f;

            }
            else
            {
                break;
            }
        }

        while(true)
        {
            if (value - sbomLabelText.preferredWidth > 2)
            {
                if (sbomLabelText.fontSize >= 100) break;

                sbomLabelText.fontSize += 0.2f;
            }
            else
            {
                break;
            }
        }

        label.transform.localPosition += new Vector3(0, (sbomLabelText.fontSize * 0.05f) + 1.5f , 0);

        Canvas.ForceUpdateCanvases();
    }



    public void CountRelationshipsAndDetermineMax()
    {

        foreach(DataObject obj in dataObjects)
        {
            obj.nr_relationships = NumberRelationshipsOfNode(obj);
            highest_relationship_count = Mathf.Max(highest_relationship_count, obj.nr_relationships);
        }
    }

    public void IncreaseBallSizeDependingOnRelationships(float baseValue)
    {
        float multiplier = 1f - baseValue;

        foreach (DataObject obj in dataObjects)
        {
            float relevance = (obj.nr_relationships / (float)highest_relationship_count) * multiplier;
            obj.DataBall.transform.localScale = new Vector3(baseValue + relevance, baseValue + relevance, baseValue + relevance);
        }
    }


    public int NumberRelationshipsOfNode(DataObject obj)
    {
        int relations = 0;
        foreach (DataObject node in dataObjects)
        {
            if (node.parent.Contains(obj))
            {
                relations++;
            }
        }

        obj.nr_children = relations;

        return relations + obj.parent.Count;
    }


    public void CVEOptimizations()
    {
        if (isCVE)
        {
            numberCategoriesLabel.transform.rotation = Quaternion.Euler(0, -90, 0);
            numberSbomLabel.transform.rotation = Quaternion.Euler(0, -90, 0);

            
            foreach(var ball in categoryBalls)
            {
                ball.transform.rotation = Quaternion.Euler(0, -90, 0);
            }
        }
    }

    public void MeshCombiner()
    {
        if (verticeMesh != null)
        {
            MonoBehaviour.Destroy(verticeMesh);
        }

        verticeMesh = new GameObject();
        MeshFilter meshFilter = verticeMesh.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = verticeMesh.AddComponent<MeshRenderer>();
        Mesh combinedMesh = new Mesh();
        List<LineRenderer> lineRenderers = new List<LineRenderer>();

        foreach (var dobj in dataObjects)
        {
            foreach (var line in dobj.relationship_line_parent)
            {

                lineRenderers.Add(line.GetComponent<LineRenderer>());
                line.GetComponent<LineRenderer>().enabled = false;
            }
        }

        CombineInstance[] combineInstances = new CombineInstance[lineRenderers.Count];

        for (int i = 0; i < lineRenderers.Count; i++)
        {
            LineRenderer lineRenderer = lineRenderers[i];
            Mesh lineMesh = ConvertLineRendererToMeshWithColors(lineRenderer);
            combineInstances[i].mesh = lineMesh;
            combineInstances[i].transform = lineRenderer.transform.localToWorldMatrix;
        }

        combinedMesh.CombineMeshes(combineInstances, true, false);
        meshFilter.mesh = combinedMesh;
        meshRenderer.sharedMaterial = lineRenderers[0].sharedMaterial;
    }

    Mesh ConvertLineRendererToMeshWithColors(LineRenderer lineRenderer)
    {
        int pointCount = lineRenderer.positionCount;
        Vector3[] positions = new Vector3[pointCount];
        lineRenderer.GetPositions(positions);

        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[pointCount * 2];
        UnityEngine.Color[] colors = new UnityEngine.Color[pointCount * 2];
        int[] triangles = new int[(pointCount - 1) * 6];

        for (int i = 0; i < pointCount; i++)
        {
            Vector3 direction = (i == pointCount - 1) ? positions[i] - positions[i - 1] : positions[i + 1] - positions[i];
            Vector3 offset = Vector3.Cross(direction.normalized, Vector3.up) * lineRenderer.startWidth/2;

            vertices[i * 2] = positions[i] - offset;
            vertices[i * 2 + 1] = positions[i] + offset;

            UnityEngine.Color color = lineRenderer.colorGradient.Evaluate(i / (float)(pointCount - 1));

            colors[i * 2] = color;
            colors[i * 2 + 1] = color;

            if (i < pointCount - 1)
            {
                int startIndex = i * 6;
                triangles[startIndex] = i * 2;
                triangles[startIndex + 1] = i * 2 + 2;
                triangles[startIndex + 2] = i * 2 + 1;

                triangles[startIndex + 3] = i * 2 + 1;
                triangles[startIndex + 4] = i * 2 + 2;
                triangles[startIndex + 5] = i * 2 + 3;
            }

        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.colors = colors; 
        mesh.RecalculateNormals();
        return mesh;
    }

    public void CreateGlowCubeLegend()
    {
        if (glowLegend != null)
        {
            MonoBehaviour.Destroy(glowLegend);
        }

        if (currentGraphType == "Stacking Radial Tree" || glowEnabled)
        {

            GameObject prefab = Resources.Load<GameObject>("Layer Glow Legend");
            glowLegend = MonoBehaviour.Instantiate(prefab, Vector3.zero, Quaternion.identity);
            glowLegend.SetActive(true);

            List<GameObject> children = new List<GameObject>();
            glowLegend.GetChildGameObjects(children);

            int layers = level_occurrences.Count;
            int counter = 0;

            glowLegend.transform.position = new Vector3(BoundaryBox.GetComponent<Renderer>().bounds.min.x, BoundaryBox.GetComponent<Renderer>().bounds.max.y - 0.5f, BoundaryBox.GetComponent<Renderer>().bounds.max.z);

            foreach (GameObject child in children)
            {
                if (counter < layers)
                {
                    child.SetActive(true);
                    child.transform.localScale = new Vector3(0.3f,0.3f,0.3f);
                    Renderer renderer = child.GetComponent<Renderer>();
                    renderer.material.SetColor("_EmissionColor", dataObjects[0].layerColorPair[counter] * 3);
                    child.transform.position += new Vector3(0, -counter, 0);
                    TextMeshPro text = child.GetNamedChild("Text").GetComponent<TextMeshPro>();
                    text.text = "Layer " + (counter + 1);
                    text.fontSize = 16;
                    counter++;
                }
                else
                {
                    child.SetActive(false);
                }
            }
        }
        
    }
}
