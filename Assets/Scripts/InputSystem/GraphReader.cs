using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Policy;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.UIElements;


public class GraphReader
{
    public GameObject BallPrefab;
    public GameObject BoundaryBox;

    public string dbid;

    public List<DataObject> dataObjects = new List<DataObject>();
    public Dictionary<int, int> level_occurrences = new Dictionary<int, int>();
    public LineDrawer ld;
    public static Dictionary<string, UnityEngine.Color> colors = new Dictionary<string, UnityEngine.Color>();
    public List<GameObject> categoryBalls = new List<GameObject>();

    public Vector3 offset = new Vector3(0, 0, 0);
    public Vector3 offsetCategories = new Vector3(0, 0, 0);
    public Vector3 graphMin = Vector3.zero;
    public Vector3 graphMax = Vector3.zero;

    public bool isCVE = false;
    public int lineCounter = 0;

    public GameObject sbomLabel;

    public void CreateGraph(string sbomElement, string graphType, bool showDuplicateNodes)
    {
        Initialization();
        ReadFileAndCreateObjects(sbomElement);

        if(!showDuplicateNodes)
        {
            FuseSameNodes();
        }

        PositionDataBalls(graphType);
        ColorDataBalls();
        CreateCategories();
        AddSbomLabel();
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
        if(sbomLabel != null)
        {
            MonoBehaviour.Destroy (sbomLabel);
        }

        dataObjects.Clear();
        level_occurrences.Clear();
        //colors.Clear();
        categoryBalls.Clear();
        lineCounter = 0;
    }

    public void ReadFileAndCreateObjects(string sbomElement)
    {
        Debug.Log(sbomElement.ToString());
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

                        string sub_node = kv.Key.ToString().Substring(0, kv.Key.Length - 1);
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
        switch (type)
        {
            case "Radial Tidy Tree":
                PositionAsRadialTidyTree();
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



    public void PositionAsForceDirectedGraph()
    {
        float attractionForce = 0.7f;
        float repulsionForce = 0.8f;
        float damping = 0.9f;

        foreach (DataObject node in dataObjects)
        {
            Vector3 force = Vector3.zero;
            node.DataBall.transform.position = UnityEngine.Random.insideUnitSphere * 10;

            foreach (DataObject other in dataObjects)
            {
                if (node != other)
                {
                    Vector3 direction = node.DataBall.transform.position - other.DataBall.transform.position;
                    float distance = direction.magnitude;
                    if (distance > 0)
                    {
                        force += direction.normalized * repulsionForce  / (distance * distance);
                    }
                }
            }
            

            // Apply attractive force to connected nodes
            foreach (DataObject neighbor in node.parent)
            {
                Vector3 direction = neighbor.DataBall.transform.position - node.DataBall.transform.position;
                float distance = direction.magnitude;
                force += direction.normalized * attractionForce * distance;
            }

            node.velocity = (node.velocity + force) * damping;
        }

        UpdatePositions();
    }

    public void UpdatePositions()
    {
        foreach (DataObject node in dataObjects)
        {
            node.DataBall.transform.position += node.velocity;
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
        LineRenderer cubeRenderer = BoundaryBox.GetComponent<LineRenderer>();

        int positionCount = cubeRenderer.positionCount;
        Vector3[] positions = new Vector3[positionCount];
        cubeRenderer.GetPositions(positions);

        for (int i = 0; i < positionCount; i++)
        {
            positions[i] += position;
        }

        cubeRenderer.SetPositions(positions);

        //Set Label Pos
        sbomLabel.transform.localPosition = new Vector3(BoundaryBox.GetComponent<Renderer>().bounds.min.x + 0.5f,
            BoundaryBox.GetComponent<Renderer>().bounds.min.y, (BoundaryBox.GetComponent<Renderer>().bounds.max.z + BoundaryBox.GetComponent<Renderer>().bounds.min.z) / 2);

        AdjustTextFontSize();
    }


    public void DrawLinesBetweenDataBalls()
    {

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
                    ld = new LineDrawer(0.04f);
                    List<Vector3> pointlist = new List<Vector3>();
                    pointlist.Add(point.DataBall.transform.position);
                    pointlist.Add(p.DataBall.transform.position);
                    GameObject line = ld.CreateLine(pointlist, isCVE);

                    point.relationship_line_parent.Add(line);
                }
            }
        }

    }

    public void ColorDataBalls()
    {

        foreach (DataObject ball in dataObjects)
        {

            if (colors.ContainsKey(ball.key) || colors.ContainsKey(ball.key.Substring(0, ball.key.Length - ball.suffix.Length)))
            {
                colors.TryGetValue(ball.key.Substring(0, ball.key.Length - ball.suffix.Length), out var color);
                ball.DataBall.GetComponentInChildren<Renderer>().material.color = color;
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
                    colorsLocal.Add(ball.key.Substring(0, ball.key.Length - ball.suffix.Length), colors[ball.key.Substring(0, ball.key.Length - ball.suffix.Length)]);
                }
            } 
        }

        float sqrt_val = Mathf.Sqrt(colorsLocal.Count);
        int rounded_val = Mathf.CeilToInt(sqrt_val);

        //Debug.Log(rounded_val);
        //Debug.Log(colors.Count);

        for (int x = 0; x < rounded_val; x++)
        {
            for (int z = 0; z < rounded_val; z++)
            {
                int index = z * rounded_val + x;

                if(index < colorsLocal.Count)
                {

                    GameObject categoryPoint = MonoBehaviour.Instantiate(BallPrefab, new Vector3(1 - (x * 0.75f), 0, 2 + (z * 1f) + (x%2) * 0.25f), Quaternion.identity);
                    TextMeshPro text = categoryPoint.GetComponentInChildren<TextMeshPro>();


                    text.text = colorsLocal.ElementAt(index).Key;
                    categoryPoint.GetComponentInChildren<Renderer>().material.color = colorsLocal.ElementAt(index).Value;

                    categoryPoint.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

                    categoryBalls.Add(categoryPoint);
                }
            }
        }
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
        sbomLabel = new GameObject();

        TextMeshPro sbomLabelText = sbomLabel.AddComponent<TextMeshPro>();
        sbomLabel.transform.localRotation = Quaternion.Euler(0, 90, 0);
        sbomLabel.transform.localPosition = new Vector3(BoundaryBox.GetComponent<Renderer>().bounds.min.x + 0.5f,
    BoundaryBox.GetComponent<Renderer>().bounds.min.y, (BoundaryBox.GetComponent<Renderer>().bounds.max.z + BoundaryBox.GetComponent<Renderer>().bounds.min.z) / 2);

        ContentSizeFitter contentFitter = sbomLabel.AddComponent<ContentSizeFitter>();
        contentFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        sbomLabelText.text = dbid;
        sbomLabelText.fontSize = 8;
        sbomLabelText.alignment = TextAlignmentOptions.Center;
        sbomLabelText.color = new UnityEngine.Color(0.1f, 0.1f, 0.1f, 0.9f);

        AdjustTextFontSize();
    }

    public void AdjustTextFontSize()
    {
        TextMeshPro sbomLabelText = sbomLabel.GetComponent<TextMeshPro>();
        float value = Mathf.Abs(BoundaryBox.GetComponent<Renderer>().bounds.max.z - BoundaryBox.GetComponent<Renderer>().bounds.min.z);

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
                if (sbomLabelText.fontSize >= 120) break;

                sbomLabelText.fontSize += 0.2f;
            }
            else
            {
                break;
            }
        }

        sbomLabel.transform.localPosition += new Vector3(0, (sbomLabelText.fontSize * 0.05f) + 1.5f , 0);

        //sbomLabel.transform.localPosition += new Vector3(0, sbomLabelText.preferredHeight, 0);

        /*
        MeshFilter meshFilter = sbomLabelText.GetComponent<MeshFilter>();

        if (meshFilter != null && meshFilter.mesh != null)
        {
            Vector3[] vertices = meshFilter.mesh.vertices;
            float minY = Mathf.Infinity;

            foreach (Vector3 vertex in vertices)
            {
                Vector3 worldVertex = sbomLabel.transform.TransformPoint(vertex);

                if (worldVertex.y < minY)
                {
                    minY = worldVertex.y;
                }
            }

            Vector3 bottomCoordinate = new Vector3(sbomLabel.transform.position.x, minY, sbomLabel.transform.position.z);

            Debug.Log("Bottom Coord: " + bottomCoordinate);

            if(!float.IsInfinity(bottomCoordinate.y))
            {
                float difference = Mathf.Abs(bottomCoordinate.y - BoundaryBox.GetComponent<Renderer>().bounds.min.y);

                if (difference > 0)
                {
                    sbomLabel.transform.localPosition += new Vector3(0, difference, 0);
                }
            }
        }
        */

        Canvas.ForceUpdateCanvases();
    }
}
