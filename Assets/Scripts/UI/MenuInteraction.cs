using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices.ComTypes;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityEngine.Networking;
using Microsoft.MixedReality.Toolkit.Experimental.UI;
using System;
using static UnityEngine.GraphicsBuffer;
using Unity.XR.CoreUtils;
using static System.Net.Mime.MediaTypeNames;
using static Unity.VisualScripting.Metadata;
using System.Linq;
public class MenuInteraction : MonoBehaviour
{

    public BackendDataHandler dbHandler;

    public GameObject scrollViewContent;
    public GameObject scrollViewContentPositions;
    public GameObject buttonTemplate;

    public UnityEngine.UI.Slider sliderLevel;
    public TextMeshProUGUI sliderText;

    public TMP_InputField inputSearch;

    public TMP_Dropdown dropdown;
    public TMP_Dropdown dropdownPositions;

    public UnityEngine.UI.Toggle showCWEToggle;
    public UnityEngine.UI.Toggle showDuplicateNodesToggle;

    public List<GraphReader> cveList = new List<GraphReader>();
    public List<GraphReader> sbomList = new List<GraphReader>();

    public GameObject BallPrefab;

    public GameObject camSphere;

    public Dictionary<string, string> textsPosition = new Dictionary<string, string>();

    public GameObject mainMenu;
    public GameObject sbomMenu;
    public TextMeshProUGUI sbomNameText;

    // Start is called before the first frame update
    void Start()
    {
        AddScrollviewContent();
        dropdownPositions.onValueChanged.AddListener(OnDropdownValueChanged);
    }

    // Update is called once per frame
    void Update()
    {

    }


    public void InitSliders()
    {

        determineMaxLevel();

        sliderLevel.onValueChanged.RemoveAllListeners();

        sliderLevel.onValueChanged.AddListener((y) =>
        {
            sliderText.text = "Show Layers: " + y.ToString();

            foreach (GraphReader graph in sbomList)
            {
                foreach (DataObject ball in graph.dataObjects)
                {

                    if (ball.level + 1 > y)
                    {

                        ball.DataBall.SetActive(false);
                        if (ball.relationship_line_parent.Count > 0)
                        {
                            ball.relationship_line_parent.ForEach(x => { x.SetActive(false); });
                        }
                    }
                    else
                    {
                        ball.DataBall.SetActive(true);
                        if (ball.relationship_line_parent.Count > 0)
                        {
                            ball.relationship_line_parent.ForEach(x => { x.SetActive(true); });
                        }
                    }
                }
            }
        });
    }

    public void determineMaxLevel()
    {
        int maxLevel = 0;

        foreach (GraphReader graph in sbomList)
        {
            if (maxLevel < graph.level_occurrences.Count)
            {
                maxLevel = graph.level_occurrences.Count;
            }
        }
        Debug.Log(maxLevel);

        sliderText.text = "Show Layers: " + maxLevel;
        sliderLevel.maxValue = maxLevel;
        sliderLevel.value = maxLevel;
    }

    public void AddScrollviewContent()
    {
        
        StartCoroutine(dbHandler.GetOnlyAllDocumentNames(AddScrollContent));

    }

    public void AddScrollContent(List<string> list)
    {
        foreach (string name in list)
        {
            GameObject btn = Instantiate(buttonTemplate, scrollViewContent.transform);

            TextMeshProUGUI text = btn.GetComponentInChildren<TextMeshProUGUI>();
            text.text = name;
            text.fontSize = 7;
            text.margin = new Vector4(5,0,5,0);

            UnityEngine.Color lightRed = new UnityEngine.Color(1f, 0.3f, 0.3f);
            UnityEngine.UI.Image img = btn.GetComponent<UnityEngine.UI.Image>();
            img.color = lightRed;

            btn.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => {

                if(img.color == UnityEngine.Color.green)
                {
                    img.color = lightRed;
                    SelectSBOM(text.text);
                } 
                else
                {
                    mainMenu.SetActive(false);
                    sbomNameText.text = text.text;
                    sbomMenu.SetActive(true);

                }

            });
        }
    }

    public void AddDropwdownVersionContent()
    {

        List<GameObject> children = new List<GameObject>();
        scrollViewContent.GetChildGameObjects(children);

        TMP_Dropdown dropdownVersion1 = sbomMenu.GetNamedChild("Comparison").GetNamedChild("Version Selection").GetComponent<TMP_Dropdown>();
        TMP_Dropdown dropdownVersion2 = sbomMenu.GetNamedChild("Comparison").GetNamedChild("Version Selection Other").GetComponent<TMP_Dropdown>();

        dropdownVersion1.ClearOptions();
        dropdownVersion2.ClearOptions();

        foreach (GameObject btn in children)
        {
            TMP_Dropdown.OptionData newOption = new TMP_Dropdown.OptionData(btn.GetComponentInChildren<TextMeshProUGUI>().text);
            dropdownVersion1.options.Add(newOption);
            dropdownVersion2.options.Add(newOption);
        }
        
        dropdownVersion1.value = dropdownVersion1.options.FindIndex(option => option.text == sbomNameText.text);
        dropdownVersion2.value = dropdownVersion2.options.FindIndex(option => option.text == sbomNameText.text);
    }

    public void CreateSBOMButton()
    {
        List<GameObject> children = new List<GameObject>();
        scrollViewContent.GetChildGameObjects(children);

        foreach (GameObject btn in children)
        {
            if(btn.GetComponentInChildren<TextMeshProUGUI>().text == sbomNameText.text)
            {
                UnityEngine.UI.Image img = btn.GetComponent<UnityEngine.UI.Image>();
                img.color = UnityEngine.Color.green;
            }
        }

        UnityEngine.UI.Toggle t = sbomMenu.GetNamedChild("CompareToggle").GetComponent<UnityEngine.UI.Toggle>();

        if (t.isOn)
        {
            TMP_Dropdown dropdownVersion1 = sbomMenu.GetNamedChild("Comparison").GetNamedChild("Version Selection").GetComponent<TMP_Dropdown>();
            TMP_Dropdown dropdownVersion2 = sbomMenu.GetNamedChild("Comparison").GetNamedChild("Version Selection Other").GetComponent<TMP_Dropdown>();


            List<string> ids = new List<string>();

            ids.Add(dropdownVersion1.options[dropdownVersion1.value].text);
            ids.Add(dropdownVersion2.options[dropdownVersion2.value].text);

            string json = JsonUtility.ToJson(new StringListWrapper { strings = ids });

            //CALL ENDPOINT TO GET BOTH JSON files
            StartCoroutine(dbHandler.GetDatabaseCompareDataByIds(ids[0], ids[1], json, "_id", CompareSBOM));
        } 
        else
        {
            SelectSBOM(sbomNameText.text);
        }

    }

    public void SelectSBOM(string name)
    {
        //if graph exists already then delete it
        foreach (GraphReader graph in sbomList)
        {
            if (graph.dbid == name)
            {
                sbomList.Remove(graph);
                graph.Initialization();
                determineMaxLevel();
                Debug.Log("REMOVE: " + graph.dbid);
                PositionAllGraphs(sbomList);
                return;
            }
        }

        StartCoroutine(dbHandler.GetDatabaseDataById(name, SBOMCreation));
    }

    public void SBOMCreation(string name, string bsonElements)
    {

        //else create new graph
        GraphReader newGraph = new GraphReader();
        newGraph.BallPrefab = BallPrefab;
        newGraph.CreateGraph(bsonElements, dropdown.options[dropdown.value].text, showDuplicateNodesToggle.isOn);
        newGraph.dbid = name;
        sbomList.Add(newGraph);
        InitSliders();
        PositionAllGraphs(sbomList);
    }

    public void CompareSBOM(string id1, string id2, List<String> sboms)
    {
        GraphReader Graph1 = new GraphReader();
        GraphReader Graph2 = new GraphReader();
        GraphReader newGraph = new GraphReader();

        Graph1.BallPrefab = BallPrefab;
        Graph2.BallPrefab = BallPrefab;
        newGraph.BallPrefab = BallPrefab;

        Graph1.dbid = id1;
        Graph2.dbid = id2;

        newGraph.dbid = id1;

        Graph1.Initialization();
        Graph1.ReadFileAndCreateObjects(sboms[0]);

        Graph2.Initialization();
        Graph2.ReadFileAndCreateObjects(sboms[1]);

        newGraph.Initialization();
        DataObject main_root = newGraph.CreateDataObjectWithBall(0, "ROOT", "", null);
        newGraph.ProcessLevelOccurence(0);
        newGraph.dataObjects.Add(main_root);

        //Start Recursion With Root and make new combined Graph
        RecursiveCompare(Graph1.dataObjects[0], Graph2.dataObjects[0], Graph1, Graph2, newGraph.dataObjects[0], newGraph);

        newGraph.PositionDataBalls(dropdown.options[dropdown.value].text);
        newGraph.ColorDataBalls();
        newGraph.CreateCategories();

        Debug.Log("ADDED -----");
        foreach(DataObject d in AddedNodes)
        {
            Debug.Log(d.key + " : " + d.value);
        }

        Debug.Log("DELETED -----");
        foreach (DataObject d in DeletedNodes)
        {
            Debug.Log(d.key + " : " + d.value);
        }

        Debug.Log("MODIFIED -----");
        foreach (DataObject d in ModifiedNodes)
        {
            Debug.Log(d.key + " : " + d.value);
        }

        Graph1.Initialization();
        Graph2.Initialization();

        sbomList.Add(newGraph);
        InitSliders();
        PositionAllGraphs(sbomList);
    }

    public List<DataObject> AddedNodes { get; set; } = new List<DataObject>();
    public List<DataObject> DeletedNodes { get; set; } = new List<DataObject>();
    public List<DataObject> ModifiedNodes { get; set; } = new List<DataObject>();


    public void RecursiveCompare(DataObject oldNode, DataObject newNode, GraphReader graph1, GraphReader graph2, DataObject newGraphNode, GraphReader newGraph)
    {
        List<DataObject> list1 = new List<DataObject>();
        List<DataObject> list2 = new List<DataObject>();

        foreach (DataObject other in graph1.dataObjects)
        {
            if(oldNode != null && other.parent.Count > 0)
            {
                if (other.parent[0] == oldNode)
                {
                    list1.Add(other);
                }
            }
        }

        foreach (DataObject other in graph2.dataObjects)
        {
            if(newNode != null && other.parent.Count > 0)
            {
                if (other.parent[0] == newNode)
                {
                    list2.Add(other);
                }
            }
        }


        // For Modified needed comparisson (newObj.key == oldObj.key && newObj.value != oldObj.value)
        // Possible Problem: Multiple nodes that have same key and value in one layer

        foreach (DataObject newObj in list2)
        {

            if (list1.Find(x => x.key == newObj.key && x.value == newObj.value) == null)
            {
                DataObject obj = newGraph.CreateDataObjectWithBall(newObj.level, newObj.key, newObj.value, newGraphNode);
                newGraph.dataObjects.Add(obj);
                List<GameObject> children = new List<GameObject>();
                obj.DataBall.GetNamedChild("Ball").GetChildGameObjects(children);
                children[1].SetActive(true);
                RecursiveCompare(null, newObj, graph1, graph2, obj, newGraph);
            }
        }

        foreach (DataObject oldObj in list1)
        {

            if (list2.Find(x => x.key == oldObj.key && x.value == oldObj.value) == null)
            {
                DataObject obj = newGraph.CreateDataObjectWithBall(oldObj.level, oldObj.key, oldObj.value, newGraphNode);
                newGraph.dataObjects.Add(obj);
                List<GameObject> children = new List<GameObject>();
                obj.DataBall.GetNamedChild("Ball").GetChildGameObjects(children);
                children[1].SetActive(true);
                RecursiveCompare(oldObj, null, graph1, graph2, obj, newGraph);
            }
        }

        foreach (DataObject oldObj in list1)
        {
            foreach (DataObject newObj in list2)
            {
                if (newObj.key == oldObj.key && newObj.value == oldObj.value)
                {
                    DataObject obj = newGraph.CreateDataObjectWithBall(oldObj.level, oldObj.key, oldObj.value, newGraphNode);
                    newGraph.dataObjects.Add(obj);
                    RecursiveCompare(oldObj, newObj, graph1, graph2, obj, newGraph);
                }
            }
        }


        if (oldNode == null && newNode == null) return;


        if(oldNode == null && newNode != null)
        {
            AddedNodes.Add(newNode);
            return;
        }

        if(oldNode != null && newNode == null)
        {
            DeletedNodes.Add(oldNode);
            return;
        }

        if(oldNode.key == newNode.key)
        {
            if(oldNode.value != newNode.value)
            {
                ModifiedNodes.Add(newNode);
                return;
            }
        }


    }


    public void OpenKeyboard()
    {
        NonNativeKeyboard.Instance.InputField = inputSearch;
        NonNativeKeyboard.Instance.OnTextSubmitted -= HightlightSearchedNode;
        NonNativeKeyboard.Instance.OnTextSubmitted += HightlightSearchedNode;
        NonNativeKeyboard.Instance.PresentKeyboard(); //inputSearch.text
    }

    public void HightlightSearchedNode(object sender, EventArgs e)
    {
        string text = inputSearch.text;

        foreach (GraphReader graph in sbomList)
        {
            if (text != null && text != "")
            {
                foreach (DataObject obj in graph.dataObjects)
                {
                    if (!obj.key.Contains(text) && !obj.value.Contains(text))
                    {
                        ChangeNodeTransparency(obj, 0.2f, 0.2f);

                    }
                    else
                    {
                        ChangeNodeTransparency(obj, 1f, 0.9f);
                    }
                }
            }
            else
            {
                foreach (DataObject obj in graph.dataObjects)
                {
                    ChangeNodeTransparency(obj, 1f, 0.9f);
                }
            }
        }
    }

    public void ChangeNodeTransparency(DataObject obj, float valueBall, float valueLines) 
    {
        UnityEngine.Color c = obj.DataBall.GetComponentInChildren<Renderer>().material.color;
        c.a = valueBall;

        obj.DataBall.GetComponentInChildren<Renderer>().material.color = c;

        foreach (var line in obj.relationship_line_parent)
        {
            line.GetComponent<LineRenderer>().startColor = new UnityEngine.Color(0, 0, 1, valueLines);
            line.GetComponent<LineRenderer>().endColor = new UnityEngine.Color(0, 0, 1, valueLines);
        }

    }

    public void ChangeGraphStyle()
    {
        Debug.Log(dropdown.options[dropdown.value].text);

        foreach (GraphReader graph in sbomList)
        {
            graph.PositionDataBalls(dropdown.options[dropdown.value].text);

            graph.offset = new Vector3(0,0,0);
            graph.offsetCategories = new Vector3(0, 0, 0);

            foreach (var obj in graph.categoryBalls)
            {
                Destroy(obj);
            }

            graph.categoryBalls.Clear();

            graph.CreateCategories();
        }

        InitSliders();
        
        PositionAllGraphs(sbomList);
    }

    [System.Serializable]
    public class StringListWrapper
    {
        public List<string> strings;
    }

    public void ShowCVENodes()
    {
        if (showCWEToggle.isOn) 
        {
            string field = "cveMetadata.cveId";

            List<string> CVEIds = new List<string>();

            foreach (var graph in sbomList)
            {
                foreach (DataObject dobj in graph.dataObjects)
                {
                    if (dobj.key.Contains("CVE"))
                    {
                        CVEIds.Add(dobj.key);
                    }
                }
            }

            string json = JsonUtility.ToJson(new StringListWrapper { strings = CVEIds });

            //StartCoroutine(dbHandler.GetCVEDataBySubstringAndField(searchCWE_ID, field, ShowAllCVENodes));
            StartCoroutine(dbHandler.GetAllCVEDataBySubstringAndField(json, field, ShowAllCVENodes));
        } 
        else
        {
            foreach(GraphReader dobj in cveList)
            {
                dobj.Initialization();
            }

            cveList.Clear();
        }
    }

    public void ShowAllCVENodes(List<string> cveData)
    {
        foreach (string cve in cveData)
        {
            GraphReader newGraph = new GraphReader();
            newGraph.isCVE = true;
            newGraph.BallPrefab = BallPrefab;
            newGraph.CreateGraph(cve, "Sphere", showDuplicateNodesToggle.isOn);
            cveList.Add(newGraph);
        }

        PositionAllGraphs(cveList);
        ConnectCVEGraphs();
    }

    public void ConnectCVEGraphs()
    {
        foreach(GraphReader cveGraph in cveList)
        {
            foreach(DataObject cveDobj in cveGraph.dataObjects)
            {
                foreach (GraphReader sbomGraph in sbomList)
                {
                    foreach (DataObject sbomDobj in sbomGraph.dataObjects)
                    {

                        if(sbomDobj.key == cveDobj.value && sbomDobj.key != "ROOT")
                        {
                            cveGraph.ld = new LineDrawer(0.04f);
                            List<Vector3> pointlist = new List<Vector3>();
                            pointlist.Add(sbomDobj.DataBall.transform.position);
                            pointlist.Add(cveDobj.DataBall.transform.position);

                            GameObject line = cveGraph.ld.CreateLine(pointlist,true);
                            cveDobj.relationship_line_parent.Add(line);
                        }


                    }
                }
            }
        }
    }

    public void PositionAllGraphs(List<GraphReader> list)
    {

        int multiplier = 1;

        if (list.Count > 0)
        {
            if (list[0].isCVE)
            {
                multiplier = -1;
            }
        }

        float maxRadius = CalculateMaxCircleRadius();
        Vector3 edge = Vector3.zero;

        for (int i = 0; i < list.Count; i++)
        {
            float radius = GetGraphRadiusX(list[i].BoundaryBox);

            Vector3 move = new Vector3(multiplier * maxRadius + (multiplier * 12), 0, edge.z);
            list[i].AdjustEntireGraphPosition(move - list[i].offset);
            list[i].offset = move;


            edge = new Vector3(list[i].BoundaryBox.GetComponent<Renderer>().bounds.min.x, 0, list[i].BoundaryBox.GetComponent<Renderer>().bounds.max.z);

            if (i - 1 >= 0)
            {
                float newZ = list[i-1].BoundaryBox.GetComponent<Renderer>().bounds.max.z - list[i].BoundaryBox.GetComponent<Renderer>().bounds.min.z;
                list[i].AdjustEntireGraphPosition(new Vector3(0, 0, newZ + 5));
                list[i].offset += new Vector3(0,0,newZ + 5);

            }

            Vector3 adjustCategoriesEdge = new Vector3(list[i].BoundaryBox.GetComponent<Renderer>().bounds.max.x - 2, list[i].BoundaryBox.GetComponent<Renderer>().bounds.max.y + 1, list[i].BoundaryBox.GetComponent<Renderer>().bounds.min.z);

            PositionCategoryBalls(list[i], adjustCategoriesEdge - list[i].offsetCategories);
            list[i].offsetCategories = adjustCategoriesEdge;

            ChangeBallRotation(list[i]);
        }
        
    }

    public void ChangeBallRotation(GraphReader g)
    {
        foreach (DataObject dobj in g.dataObjects)
        {
            Vector3 directionToTarget = camSphere.transform.position - dobj.DataBall.transform.position;

            dobj.DataBall.transform.rotation = Quaternion.LookRotation(-directionToTarget);
        }
    }

    public void PositionCategoryBalls(GraphReader g, Vector3 adjust)
    {
        foreach (var ball in g.categoryBalls)
        {
            if(ball != null)
            {
                ball.transform.position += adjust;
            }

        }
    }

    public float CalculateMaxCircleRadius()
    {
        float maxRadius = 0f;

        foreach (GraphReader obj in sbomList)
        {
            float radius = GetGraphRadiusX(obj.BoundaryBox);

            if (radius > maxRadius)
            {
                maxRadius = radius; 
            }
        
            Debug.Log(maxRadius);
        }

        return maxRadius;
    }

    public float GetGraphRadius(GameObject obj)
    {
        var bounds = obj.GetComponent<Renderer>().bounds;
        return Mathf.Max(bounds.extents.x, bounds.extents.z);
    }

    public float GetGraphRadiusX(GameObject obj)
    {
        var bounds = obj.GetComponent<Renderer>().bounds;
        return bounds.extents.x;
    }

    public void ShowNodePositionInMenu(GameObject ball)
    {
        foreach (GraphReader graph in sbomList) { 
            foreach(DataObject dobj in graph.dataObjects)
            {
                if (ball == dobj.DataBall.GetNamedChild("Ball"))
                {
                    Debug.Log(dobj.key + " : " + dobj.value);

                    MakeSameNodesGlow(dobj);
                    ShowPositionInJson(dobj, graph);
                }
            }
        }
    }

    public void MakeSameNodesGlow(DataObject dobj)
    {

    }

    public void ShowPositionInJson(DataObject dobj, GraphReader graph)
    {
        int pos = 0;
        dropdownPositions.options.Clear();
        textsPosition.Clear();

        
        if(dobj.parent.Count == 1 && showDuplicateNodesToggle.isOn)
        {
            foreach (DataObject other in graph.dataObjects)
            {
                if(other.key == dobj.key && other.value == dobj.value)
                {
                    string displayText = "";

                    foreach (DataObject child in graph.dataObjects)
                    {
                        if (child.parent.Contains(other.parent[0]))
                        {
                            displayText += GetLineText(child, other);
                        }
                    }
                    pos++;
                    TMP_Dropdown.OptionData newOption = new TMP_Dropdown.OptionData("Position " + pos);
                    dropdownPositions.options.Add(newOption);
                    textsPosition.Add("Position " + pos, displayText);
                }
            }
        }
        else
        {
            foreach (DataObject parent in dobj.parent)
            {
                string displayText = "";

                foreach (DataObject other in graph.dataObjects)
                {
                    if (other.parent.Contains(parent))
                    {
                        displayText += GetLineText(other, dobj);
                    }
                }
                pos++;
                TMP_Dropdown.OptionData newOption = new TMP_Dropdown.OptionData("Position " + pos);
                dropdownPositions.options.Add(newOption);
                textsPosition.Add("Position " + pos, displayText);
            }
        }
       
        dropdownPositions.captionText.text = "Position 1";
        AddTextToScrollContent(textsPosition["Position 1"]);
    }


    public string GetLineText(DataObject other, DataObject dobj)
    {
        string displayText = "";
        string lineNumber = "";

        if(showDuplicateNodesToggle.isOn)
        {
            lineNumber = "<color=#010101>" + other.lineNumber.ToString() + "\t </color>";
        }

        if (other == dobj)
        {
            displayText += lineNumber + "<color=#550000>" + other.key + " :</color> ";
        }
        else
        {
            displayText += lineNumber + "<color=#005500>" + other.key + " :</color> ";
        }


        if (other.value != "")
        {
            displayText += "<b>" + other.value + "</b>\n";
        }
        else 
        {
            displayText += "<link=\"other.key\"><b>";
            displayText += "{...}" + "</b></link>" + "\n";

        }

        return displayText;
    }

    public void OnDropdownValueChanged(int index)
    {

        if (textsPosition.Count > 0) 
        {
            Debug.Log("Selected option: " + dropdownPositions.options[index].text);
            AddTextToScrollContent(textsPosition[dropdownPositions.options[index].text]);

        }

    }

    public void AddTextToScrollContent(string text)
    {

        TextMeshProUGUI textUI = scrollViewContentPositions.GetComponent<TextMeshProUGUI>();
        textUI.text = text;
    }

}
