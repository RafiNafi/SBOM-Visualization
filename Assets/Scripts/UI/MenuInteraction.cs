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
using System.Linq;
using System.Xml;
using UnityEngine.Windows;
using static Unity.VisualScripting.Metadata;
using UnityEngine.XR.OpenXR.Input;
public class MenuInteraction : MonoBehaviour
{

    public BackendDataHandler dbHandler;

    public LineDrawer ld;

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

    public GameObject jsonMenu;
    public bool activateWindow = false;

    public GameObject previousClickedBtn;
    public List<(string, GameObject, TMP_Dropdown.OptionData, string)> selectionlist = new List<(string, GameObject, TMP_Dropdown.OptionData, string)>();

    public string previousSelectedType = "";

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

    public void AddScrollContent(List<(string,string)> list)
    {

        foreach ((string id, string name) in list)
        {
            GameObject btn = Instantiate(buttonTemplate, scrollViewContent.transform);

            TextMeshProUGUI text = btn.GetComponentInChildren<TextMeshProUGUI>();

            text.enableAutoSizing = true;
            text.fontSizeMax = 7;
            text.fontSizeMin = 2;

            if(name != "")
            {
                text.text = name;
            }
            else
            {
                text.text = id;
            }

            text.fontSize = 7;
            text.margin = new Vector4(5,0,5,0);

            UnityEngine.Color lightRed = new UnityEngine.Color(1f, 0.3f, 0.3f);
            UnityEngine.UI.Image img = btn.GetComponent<UnityEngine.UI.Image>();
            img.color = lightRed;


            selectionlist.Add((id,btn,null, name));


            btn.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => {

                previousClickedBtn = btn;

                if (img.color == UnityEngine.Color.green)
                {
                    img.color = lightRed;
                    SelectSBOM(selectionlist[getIndexByButton(btn)].Item1, selectionlist[getIndexByButton(btn)].Item4);
                } 
                else
                {
                    mainMenu.SetActive(false);
                    sbomNameText.text = text.text;
                    sbomMenu.SetActive(true);

                    if(sbomMenu.GetNamedChild("CompareToggle").GetComponent<UnityEngine.UI.Toggle>().isOn)
                    {
                        AddDropdownVersionContent();
                    }
                }

            });
        }
    }

    public int getIndexByButton(GameObject obj)
    {
        for (int index=0; index < selectionlist.Count; index++)
        {
            if (obj == selectionlist[index].Item2)
            {
                return index;
            }
        }

        return -1;
    }

    public void AddDropdownVersionContent()
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

            selectionlist[getIndexByButton(btn)] = (selectionlist[getIndexByButton(btn)].Item1, btn, newOption, selectionlist[getIndexByButton(btn)].Item4);
        }


        dropdownVersion1.value = dropdownVersion1.options.FindIndex(option => option == selectionlist[getIndexByButton(previousClickedBtn)].Item3);
        dropdownVersion2.value = dropdownVersion2.options.FindIndex(option => option == selectionlist[getIndexByButton(previousClickedBtn)].Item3);

        dropdownVersion1.RefreshShownValue();
        dropdownVersion2.RefreshShownValue();
    }

    public void CreateSBOMButton()
    {
        List<GameObject> children = new List<GameObject>();
        scrollViewContent.GetChildGameObjects(children);

        foreach (GameObject btn in children)
        {
            if(btn == previousClickedBtn)
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

            

            for (int index = 0; index < selectionlist.Count; index++)
            {
                if (dropdownVersion1.options[dropdownVersion1.value] == selectionlist[index].Item3)
                {
                    ids.Add(selectionlist[index].Item1);
                }
                if(dropdownVersion2.options[dropdownVersion2.value] == selectionlist[index].Item3)
                {
                    ids.Add(selectionlist[index].Item1);
                }
            }

            string json = JsonUtility.ToJson(new StringListWrapper { strings = ids });

            //CALL ENDPOINT TO GET BOTH JSON files
            StartCoroutine(dbHandler.GetDatabaseCompareDataByIds(ids[0], ids[1], json, "_id", CompareSBOM));
        } 
        else
        {
            SelectSBOM(selectionlist[getIndexByButton(previousClickedBtn)].Item1, selectionlist[getIndexByButton(previousClickedBtn)].Item4);
        }

    }

    public void SelectSBOM(string id, string name)
    {
        //if graph exists already then delete it
        foreach (GraphReader graph in sbomList)
        {
            if (graph.dbid == id)
            {
                sbomList.Remove(graph);
                graph.Initialization();
                determineMaxLevel();
                Debug.Log("REMOVE: " + graph.dbid);
                PositionAllGraphs(sbomList);
                return;
            }
        }
        StartCoroutine(dbHandler.GetDatabaseDataById(id, name, SBOMCreation));
    }

    public void SBOMCreation(string id, string name, string bsonElements)
    {
        //else create new graph
        GraphReader newGraph = new GraphReader();
        newGraph.BallPrefab = BallPrefab;
        newGraph.dbid = id;
        newGraph.sbomName = name;
        newGraph.CreateGraph(bsonElements, dropdown.options[dropdown.value].text, showDuplicateNodesToggle.isOn);
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
        newGraph.sbomName = "";

        Graph1.Initialization();
        Graph1.ReadFileAndCreateObjects(sboms[0]);

        Graph2.Initialization();
        Graph2.ReadFileAndCreateObjects(sboms[1]);

        newGraph.Initialization();
        DataObject main_root = newGraph.CreateDataObjectWithBall(0, "ROOT", "", null);
        newGraph.ProcessLevelOccurence(0);
        newGraph.dataObjects.Add(main_root);

        //Initialization Lists
        AddedNodes = new List<DataObject>();
        DeletedNodes = new List<DataObject>();
        ModifiedNodes = new List<DataObject>();

        //Start Recursion With Root and make new combined Graph
        RecursiveCompare(Graph1.dataObjects[0], Graph2.dataObjects[0], newGraph.dataObjects[0], Graph1, Graph2, newGraph);

        newGraph.CountCategoryNumbers();
        newGraph.PositionDataBalls(dropdown.options[dropdown.value].text);
        newGraph.ColorDataBalls();
        newGraph.CreateCategories();
        newGraph.AddSbomLabel();

        //Delete both Graphs
        Graph1.Initialization();
        Graph2.Initialization();

        sbomList.Add(newGraph);
        InitSliders();
        PositionAllGraphs(sbomList);

        /*
        Debug.Log("ADDED -----");
        foreach (DataObject d in AddedNodes)
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
        */
    }

    public List<DataObject> AddedNodes = new List<DataObject>();
    public List<DataObject> DeletedNodes = new List<DataObject>();
    public List<DataObject> ModifiedNodes = new List<DataObject>();


    public void RecursiveCompare(DataObject oldNode, DataObject newNode, DataObject newGraphNode, GraphReader graph1, GraphReader graph2, GraphReader newGraph)
    {

        // For Modified needed comparisson (newObj.key == oldObj.key && newObj.value != oldObj.value)
        // Possible Problem: Multiple nodes that have same key and value in one layer

        if(oldNode != null && newNode != null)
        {
            foreach (DataObject newObj in newNode.children)
            {

                if (oldNode.children.Find(x => x.key == newObj.key && x.value == newObj.value) == null)
                {
                    DataObject obj = newGraph.CreateDataObjectWithBall(newObj.level, newObj.key, newObj.value, newGraphNode);
                    newGraph.dataObjects.Add(obj);
                    List<GameObject> children = new List<GameObject>();
                    obj.DataBall.GetNamedChild("Ball").GetChildGameObjects(children);
                    children[2].SetActive(true);
                    RecursiveCompare(null, newObj, obj, graph1, graph2, newGraph);
                }
            }

            foreach (DataObject oldObj in oldNode.children)
            {

                if (newNode.children.Find(x => x.key == oldObj.key && x.value == oldObj.value) == null)
                {
                    DataObject obj = newGraph.CreateDataObjectWithBall(oldObj.level, oldObj.key, oldObj.value, newGraphNode);
                    newGraph.dataObjects.Add(obj);
                    List<GameObject> children = new List<GameObject>();
                    obj.DataBall.GetNamedChild("Ball").GetChildGameObjects(children);
                    children[1].SetActive(true);
                    RecursiveCompare(oldObj, null, obj, graph1, graph2, newGraph);
                }
            }

            foreach (DataObject oldObj in oldNode.children)
            {
                foreach (DataObject newObj in newNode.children)
                {
                    if (newObj.key == oldObj.key && newObj.value == oldObj.value)
                    {
                        DataObject obj = newGraph.CreateDataObjectWithBall(oldObj.level, oldObj.key, oldObj.value, newGraphNode);
                        newGraph.dataObjects.Add(obj);
                        RecursiveCompare(oldObj, newObj, obj, graph1, graph2,  newGraph);
                    }
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
                        ChangeNodeAndLinesTransparency(obj, 0.2f, 0.1f, 0.3f);

                    }
                    else
                    {
                        ChangeNodeAndLinesTransparency(obj, 1f, 0.9f, 1f);
                    }
                }
            }
            else
            {
                foreach (DataObject obj in graph.dataObjects)
                {
                    ChangeNodeAndLinesTransparency(obj, 1f, 0.9f,1f);
                }
            }
        }
    }

    public void ChangeNodeAndLinesTransparency(DataObject obj, float valueBall, float valueLines, float valueText) 
    {
        UnityEngine.Color c = obj.DataBall.GetComponentInChildren<Renderer>().material.color;
        c.a = valueBall;

        obj.DataBall.GetComponentInChildren<Renderer>().material.color = c;

        ld = new LineDrawer(0.04f);

        foreach (var line in obj.relationship_line_parent)
        {
            line.GetComponent<LineRenderer>().colorGradient = ld.GetBlueGradientWithTransparency(valueLines);
        }

        TextMeshPro t = obj.DataBall.GetNamedChild("Ball").GetNamedChild("Text").GetComponent<TextMeshPro>();

        UnityEngine.Color currentColor = t.color;
        currentColor.a = valueText;
        t.color = currentColor;
    }


    public void ChangeOnlyNodeTransparency(DataObject obj, float valueBall, float valueText)
    {
        UnityEngine.Color c = obj.DataBall.GetComponentInChildren<Renderer>().material.color;
        c.a = valueBall;

        obj.DataBall.GetComponentInChildren<Renderer>().material.color = c;

        TextMeshPro t = obj.DataBall.GetNamedChild("Ball").GetNamedChild("Text").GetComponent<TextMeshPro>();

        UnityEngine.Color currentColor = t.color;
        currentColor.a = valueText;
        t.color = currentColor;
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

            Vector3 adjustCategoriesEdge = new Vector3(list[i].BoundaryBox.GetComponent<Renderer>().bounds.max.x, list[i].BoundaryBox.GetComponent<Renderer>().bounds.max.y + 1, list[i].BoundaryBox.GetComponent<Renderer>().bounds.min.z);

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
        //Graph Node Clicked
        foreach (GraphReader graph in sbomList) { 
            foreach(DataObject dobj in graph.dataObjects)
            {
                if (ball == dobj.DataBall.GetNamedChild("Ball"))
                {
                    Debug.Log(dobj.key + " : " + dobj.value);

                    MakeAllNodesVisible();
                    MakeOtherNodesTransparent(dobj, graph);
                    ShowPositionInJson(dobj, graph);
                    activateWindow = true;
                    jsonMenu.SetActive(activateWindow);
                    return;
                }
            }

            //Category Node Clicked
            foreach (GameObject cobj in graph.categoryBalls)
            {
                if(ball == cobj.GetNamedChild("Ball"))
                {
                    List<GameObject> children = new List<GameObject>();
                    ball.GetChildGameObjects(children);

                    Debug.Log(children[0]);

                    TextMeshPro textui = children[0].GetComponent<TextMeshPro>();

                    if(previousSelectedType == textui.text)
                    {
                        MakeAllNodesVisible();
                    }
                    else
                    {
                        MakeAllNodesVisible();
                        MakeOtherNodesTransparentWithString(textui.text, graph);
                        previousSelectedType = textui.text;
                    }

                    return;
                }
            }
        }


    }

    public void MakeOtherNodesTransparent(DataObject dobj, GraphReader g)
    {

        foreach (DataObject other in g.dataObjects)
        {
            if (other.key != dobj.key || other.value != dobj.value)
            {
                ChangeNodeAndLinesTransparency(other, 0.2f, 0.1f, 0.3f);

                if (other.parent.Contains(dobj) || dobj.parent.Contains(other))
                {
                    ld = new LineDrawer(0.04f);

                    foreach (var line in other.relationship_line_parent)
                    {
                        LineRenderer lr = line.GetComponent<LineRenderer>();

                        Vector3 startPosition = lr.GetPosition(0);
                        Vector3 endPosition = lr.GetPosition(lr.positionCount - 1);

                        if((Vector3.Distance(startPosition, dobj.DataBall.transform.position) <= 0.0001f) || (Vector3.Distance(endPosition, dobj.DataBall.transform.position) <= 0.0001f))
                        {
                            lr.colorGradient = ld.GetBlueGradientWithTransparency(0.9f);
                        }

                        ChangeOnlyNodeTransparency(other, 1f, 1f);
                    }

                }
            }
        }
    }

    public void MakeOtherNodesTransparentWithString(string key, GraphReader g)
    {
        foreach (DataObject other in g.dataObjects)
        {
            if (key != other.key && key != (other.key.Substring(0, other.key.Length - other.suffix.Length)))
            {
                ChangeNodeAndLinesTransparency(other, 0.2f, 0.1f, 0.3f);
            }
        }
    }

    public void CloseJSONPosWindow()
    {
        MakeAllNodesVisible();

        activateWindow = false;
    }

    public void MakeAllNodesVisible()
    {
        foreach (GraphReader graph in sbomList)
        {
            foreach (DataObject other in graph.dataObjects)
            {
                ChangeNodeAndLinesTransparency(other, 1f, 0.9f, 1f);
            }
        }
    }

    public void CheckPosWindowVisibility()
    {
        jsonMenu.SetActive(activateWindow);
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
                            displayText += GetLineText(child, other, "");
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
                        displayText += GetLineText(other, dobj, "");
                    }
                }
                displayText += "\t";
                pos++;
                TMP_Dropdown.OptionData newOption = new TMP_Dropdown.OptionData("Position " + pos);
                dropdownPositions.options.Add(newOption);
                textsPosition.Add("Position " + pos, displayText);
            }
        }
       
        dropdownPositions.captionText.text = "Position 1";
        AddTextToScrollContent(textsPosition["Position 1"]);
    }


    public string GetLineText(DataObject other, DataObject dobj, string tabs)
    {
        string displayText = "" + tabs;
        string lineNumber = "";

        if(showDuplicateNodesToggle.isOn)
        {
            lineNumber = "<color=#010101>" + other.lineNumber.ToString() + "</color> \t";
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
            displayText += "<link=\""+other.GetHashCode()+"\"><b>";
            displayText += "{...}" + "</b></link>" + "\n";

        }

        return displayText;
    }

    int countTabs = 0;

    public void ExpandNodeInMenu(string linkHashID)
    {
        foreach (GraphReader graph in sbomList)
        {
            foreach (DataObject other in graph.dataObjects)
            {
                if (other.GetHashCode().ToString() == linkHashID)
                {
                    TextMeshProUGUI text = scrollViewContentPositions.GetComponent<TextMeshProUGUI>();

                    string displayText = textsPosition[dropdownPositions.options[dropdownPositions.value].text];
                    string addedDisplayText = "";
                    int maxSequence = GetMaxSequence(displayText);
                    string tabs = "";
                    Debug.Log(maxSequence);

                    for (int i = 0; i < maxSequence + countTabs; i++)
                    {
                        tabs += "\t";
                    }
                    if (countTabs == 0) countTabs++;

                    Debug.Log(other.key);

                    foreach (DataObject child in graph.dataObjects)
                    {
                        if (child.parent.Contains(other))
                        {
                            addedDisplayText += GetLineText(child, null, tabs);
                        }
                    }

                    string search = "<link=\"" + other.GetHashCode() + "\"><b>" + "{...}" + "</b></link>" + "\n";
                    int index = displayText.IndexOf(search);
                    Debug.Log(index);

                    displayText = displayText.Substring(0, search.Length + index) + addedDisplayText + displayText.Substring(search.Length + index);

                    textsPosition[dropdownPositions.options[dropdownPositions.value].text] = displayText;
                    
                    text.text = displayText;

                    return;
                }
            }
        }
    }

    public int GetMaxSequence(string input)
    {
        int maxCount = 0;
        int currentCount = 0;

        foreach (char c in input)
        {
            if (c == '\t')
            {
                currentCount++;  
                maxCount = Math.Max(maxCount, currentCount);
            }
            else
            {
                currentCount = 0; 
            }
        }

        return maxCount;
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
