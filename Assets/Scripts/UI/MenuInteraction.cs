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
using System.IO;

public class MenuInteraction : MonoBehaviour
{

    public BackendDataHandler dbHandler;

    public LineDrawer ld;

    public GameObject player;
    public GameObject setup;
    public Camera cam;

    public GameObject scrollViewContent;
    public GameObject scrollViewContentPositions;
    public GameObject scrollViewContentSearch;
    public GameObject scrollViewCentextMenu;
    public GameObject buttonTemplate;
    public GameObject tooltip;

    public GameObject searchResultsPanel;
    public GameObject contextMenuPanel;

    public UnityEngine.UI.Slider sliderLevel;
    public TextMeshProUGUI sliderText;

    public UnityEngine.UI.Slider sliderTextLength;
    public TextMeshProUGUI TextLength;

    public TMP_InputField inputSearch;

    public TMP_Dropdown dropdown;
    public TMP_Dropdown dropdownPositions;

    //Other Options Menu Options
    public UnityEngine.UI.Toggle showCWEToggle;
    public UnityEngine.UI.Toggle showDuplicateNodesToggle;
    public UnityEngine.UI.Toggle changeBallSize;
    public UnityEngine.UI.Toggle showUnrelevantNodes;
    public UnityEngine.UI.Toggle enableLayerColors;

    //Search Menu Options
    public UnityEngine.UI.Toggle WithinSelectedTypeToggle;
    public UnityEngine.UI.Toggle HierarchiesFilteredSelectionToggle;
    public UnityEngine.UI.Toggle DependenciesFilteredSelectionToggle;

    public List<GraphReader> cveList = new List<GraphReader>();
    public List<GraphReader> sbomList = new List<GraphReader>();

    public GameObject BallPrefab;

    public GameObject camSphere;

    public Dictionary<string, string> textsPosition = new Dictionary<string, string>();
    public Dictionary<GameObject, DataObject> searchBtnDataPair = new Dictionary<GameObject, DataObject>();
    public Dictionary<GameObject, DataObject> searchBtnDataPairDrilldown = new Dictionary<GameObject, DataObject>();

    public GameObject mainMenu;
    public GameObject sbomMenu;
    public TextMeshProUGUI sbomNameText;

    public TextMeshProUGUI selectedNode;
    public GameObject jsonMenu;
    public bool activateWindow = false;

    public GameObject previousClickedBtn;
    public List<(string, GameObject, TMP_Dropdown.OptionData, string)> selectionlist = new List<(string, GameObject, TMP_Dropdown.OptionData, string)>();

    public string previousSelectedType = "";
    public GraphReader previousGraph;

    public List<string> expandedNodes = new List<string>();

    public GameObject searchResultsMenu;

    // Start is called before the first frame update
    void Start()
    {
        AddScrollviewContent();
        dropdownPositions.onValueChanged.AddListener(OnDropdownValueChanged);
        InitOptions();
    }

    private float timer = 0f; 
    public float interval = 1f;

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= interval)
        {
            CheckTextMeshVicinity(); 
            timer = 0f; 
        }
        
    }

    public void CheckTextMeshVicinity()
    {
        foreach (var sbom in sbomList)
        {
            foreach (var dobj in sbom.dataObjects)
            {
                List<GameObject> list = new List<GameObject>();
                dobj.DataBall.GetNamedChild("Ball").GetChildGameObjects(list);
                float distance = Vector3.Distance(list[0].transform.position, camSphere.transform.position);

                if (distance > 50)
                {
                    list[0].SetActive(false);
                }
                else
                {

                    list[0].SetActive(true);
                }
            }
        }
    }

    public void InitOptions()
    {
        TextLength.text = "Text Length Cap: " + (sliderTextLength.value * 10).ToString() + " Character";

        sliderTextLength.onValueChanged.AddListener(OnSliderValueChanged);

        //EventTrigger to handle pointer events
        EventTrigger trigger = sliderTextLength.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry pointerDownEntry = new EventTrigger.Entry();
        pointerDownEntry.eventID = EventTriggerType.PointerDown;
        pointerDownEntry.callback.AddListener((eventData) => { OnPointerDown(); });
        trigger.triggers.Add(pointerDownEntry);

        EventTrigger.Entry pointerUpEntry = new EventTrigger.Entry();
        pointerUpEntry.eventID = EventTriggerType.PointerUp;
        pointerUpEntry.callback.AddListener((eventData) => { OnPointerUp(); });
        trigger.triggers.Add(pointerUpEntry);
    }

    private bool isDragging = false;

    private void OnSliderValueChanged(float count)
    {
        TextLength.text = "Text Length Cap: " + (count * 10).ToString() + " Character";

        if (!isDragging)
        {
            foreach (GraphReader g in sbomList)
            {
                foreach (DataObject obj in g.dataObjects)
                {
                    ChangeTextSize(count, obj);
                }
            }
        }
    }

    public void ChangeTextSize(float size, DataObject obj)
    {
        List<GameObject> children = new List<GameObject>();
        obj.DataBall.GetNamedChild("Ball").GetChildGameObjects(children);
        TextMeshPro text = children[0].GetComponent<TextMeshPro>();

        if (obj.value != "")
        {
            if ((int)size * 10 >= obj.value.Length)
            {
                text.text = obj.key + ":" + obj.value;
            }
            else
            {
                text.text = obj.key + ":" + obj.value.Substring(0, ((int)size * 10)) + "<b>...</b>";
            }
        }
    }


    public void OnPointerDown()
    {
        isDragging = true;
    }


    public void OnPointerUp()
    {
        isDragging = false;
        OnSliderValueChanged(sliderTextLength.value); 
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

                if(showCWEToggle.isOn)
                {
                    ReselectCVE();
                }
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
        CheckForStackingRadialTree();

        //SHOW CVE
        if (showCWEToggle.isOn)
        {
            ReselectCVE();
        }
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
        newGraph.isComparisonGraph = true;

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
        newGraph.CountRelationshipsAndDetermineMax();
        newGraph.PositionDataBalls(dropdown.options[dropdown.value].text);
        newGraph.ColorDataBalls();
        newGraph.CreateCategories();
        newGraph.AddSbomLabel();

        ShowUnrelevantNodes();

        //Delete both Graphs
        Graph1.Initialization();
        Graph2.Initialization();

        sbomList.Add(newGraph);
        InitSliders();
        PositionAllGraphs(sbomList);

        CheckForStackingRadialTree();

        /*
        //Debug.Log("ADDED -----");
        foreach (DataObject d in AddedNodes)
        {
            Debug.Log(d.key + " : " + d.value);
        }

        //Debug.Log("DELETED -----");
        foreach (DataObject d in DeletedNodes)
        {
            Debug.Log(d.key + " : " + d.value);
        }

        //Debug.Log("MODIFIED -----");
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
                    obj.modifiedStatus = true;
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
                    obj.modifiedStatus = true;
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

        List<GameObject> list = new List<GameObject>();
        obj.DataBall.GetNamedChild("Ball").GetChildGameObjects(list);

        TextMeshPro t = list[0].GetComponent<TextMeshPro>();

        UnityEngine.Color currentColor = t.color;
        currentColor.a = valueText;
        t.color = currentColor;
    }


    public void ChangeOnlyNodeTransparency(DataObject obj, float valueBall, float valueText)
    {
        UnityEngine.Color c = obj.DataBall.GetComponentInChildren<Renderer>().material.color;
        c.a = valueBall;

        obj.DataBall.GetComponentInChildren<Renderer>().material.color = c;

        List<GameObject> list = new List<GameObject>();
        obj.DataBall.GetNamedChild("Ball").GetChildGameObjects(list);

        TextMeshPro t = list[0].GetComponent<TextMeshPro>();

        UnityEngine.Color currentColor = t.color;
        currentColor.a = valueText;
        t.color = currentColor;
    }

    public void ChangeOnlyLineTransparency(DataObject from, DataObject to, float valueLines)
    {
        ld = new LineDrawer(0.04f);

        foreach (var line in from.relationship_line_parent)
        {
            LineRenderer lr = line.GetComponent<LineRenderer>();

            Vector3 startPosition = lr.GetPosition(0);
            Vector3 endPosition = lr.GetPosition(lr.positionCount - 1);

            if ((Vector3.Distance(startPosition, from.DataBall.transform.position) <= 0.0001f) && (Vector3.Distance(endPosition, to.DataBall.transform.position) <= 0.0001f))
            {
                lr.colorGradient = ld.GetBlueGradientWithTransparency(valueLines);
            }
        }
    }

    public void ChangeGraphStyle()
    {
        //Debug.Log(dropdown.options[dropdown.value].text);

        foreach (GraphReader graph in sbomList)
        {
            graph.PositionDataBalls(dropdown.options[dropdown.value].text);

            graph.offset = new Vector3(0,0,0);
            graph.offsetCategories = new Vector3(0, 0, 0);

            foreach (var obj in graph.categoryBalls)
            {
                Destroy(obj);
            }

            if(graph.categoryPlane != null)
            {
                Destroy(graph.categoryPlane);
            }

            graph.categoryBalls.Clear();

            graph.CreateCategories();
        }

        InitSliders();
        
        PositionAllGraphs(sbomList);

        //SHOW CVE
        if (showCWEToggle.isOn)
        {
            ReselectCVE();
        }

        CheckForStackingRadialTree();
    }

    [System.Serializable]
    public class StringListWrapper
    {
        public List<string> strings;
    }

    public void CheckForStackingRadialTree()
    {
        //Enable automatically for Stacking Radial Tree
        if (dropdown.options[dropdown.value].text == "Stacking Radial Tree")
        {
            enableLayerColors.isOn = true;
            EnableLayerGlow();
        }
    }

    public void ShowCVENodes()
    {
        if (showCWEToggle.isOn) 
        {
            GetCVEData();
        } 
        else
        {
            ClearCVE();
        }
    }

    public void ReselectCVE()
    {
        ClearCVE();

        GetCVEData();
    }

    public void GetCVEData()
    {
        string field = "cveMetadata.cveId";

        List<string> CVEIds = new List<string>();

        foreach (var graph in sbomList)
        {
            foreach (DataObject dobj in graph.dataObjects)
            {
                if (dobj.key.Contains("CVE"))
                {
                    if(!CVEIds.Contains(dobj.key))
                    {
                        CVEIds.Add(dobj.key);
                    }
                }
            }
        }

        string json = JsonUtility.ToJson(new StringListWrapper { strings = CVEIds });

        //StartCoroutine(dbHandler.GetCVEDataBySubstringAndField(searchCWE_ID, field, ShowAllCVENodes));
        StartCoroutine(dbHandler.GetAllCVEDataBySubstringAndField(json, field, ShowAllCVENodes));
    }

    public void ClearCVE()
    {
        foreach (GraphReader dobj in cveList)
        {
            dobj.Initialization();
        }

        cveList.Clear();
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
                            cveDobj.children.Add(sbomDobj);

                        }

                    }
                }
            }
        }
    }

    /*
    public void CheckCVEConnection()
    {
        List<GraphReader> saveList = new List<GraphReader>();

        foreach(GraphReader cve in cveList)
        {
            foreach(DataObject dobj in cve.dataObjects)
            {
                if(dobj.children.Count > 0)
                {
                    foreach(DataObject child in dobj.children)
                    {
                        if (child != null)
                        {
                            if (!saveList.Contains(cve))
                            {
                                Debug.Log("CVE SAVED");
                                saveList.Add(cve);
                            }
                            break;
                        }
                    }
                }
            }
        }

        foreach (GraphReader cve in cveList)
        {
            if(!saveList.Contains(cve))
            {
                cve.Initialization();
            }
        }
        cveList.Clear();
        cveList = saveList;
    }
    */
    public void PositionAllGraphs(List<GraphReader> list)
    {

        int multiplier = 1;

        //CVE position
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
            //Graph
            //float radius = GetGraphRadiusX(list[i].BoundaryBox);

            Vector3 move = new Vector3(multiplier * maxRadius + (multiplier * 12), 0, edge.z);
            list[i].AdjustEntireGraphPosition(move - list[i].offset);
            list[i].offset = move;


            //Boundary Box
            edge = new Vector3(list[i].BoundaryBox.GetComponent<Renderer>().bounds.min.x, 0, list[i].BoundaryBox.GetComponent<Renderer>().bounds.max.z);

            if (i - 1 >= 0)
            {
                float newZ = list[i-1].BoundaryBox.GetComponent<Renderer>().bounds.max.z - list[i].BoundaryBox.GetComponent<Renderer>().bounds.min.z;
                list[i].AdjustEntireGraphPosition(new Vector3(0, 0, newZ + 5));
                list[i].offset += new Vector3(0,0,newZ + 5);

            }

            //Categories
            //Vector3 adjustCategoriesEdge = list[i].CalculateCategoryStartPoint();
            PositionCategoryBalls(list[i], list[i].offset - list[i].offsetCategories);
            list[i].offsetCategories = list[i].offset;

            //Ball Text Rotation
            ChangeBallRotation(list[i]);
        }

        //Change Ball Text Size 
        OnSliderValueChanged(sliderTextLength.value);

        //Change Ball Size 
        EventBallSizeDependingOnRelationships();
    }

    public void ChangeBallRotation(GraphReader g)
    {
        foreach (DataObject dobj in g.dataObjects)
        {
            Vector3 directionToTarget = camSphere.transform.position - dobj.DataBall.transform.position;
            Vector3 dir = -directionToTarget;
            dobj.DataBall.transform.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z));
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
        
            //Debug.Log(maxRadius);
        }

        return maxRadius;
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
                    //Debug.Log(dobj.key + " : " + dobj.value);

                    MakeAllNodesVisible();
                    MakeOtherNodesTransparent(dobj, graph);
                    ShowPositionInJson(dobj, graph);
                    activateWindow = true;
                    jsonMenu.SetActive(activateWindow); 

                    if(dobj.value != "")
                    {
                        selectedNode.text = "<color=#550000>Type: </color>" + dobj.key + "\n <color=#550000>Value:</color> " + dobj.value + "\n <color=#550000>Relationships:</color> " + dobj.nr_relationships;

                    }
                    else
                    {
                        selectedNode.text = "<color=#550000>Type: </color>" + dobj.key + "\n <color=#550000>Relationships:</color> " + dobj.nr_relationships;

                    }
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

                    //Debug.Log(children[0]);

                    TextMeshPro textui = children[0].GetComponent<TextMeshPro>();

                    if(previousSelectedType == textui.text)
                    {
                        MakeAllNodesVisible();
                        previousSelectedType = "";
                        previousGraph = null;
                    }
                    else
                    {
                        MakeAllNodesInGraphInvisible(graph);
                        MakeOtherNodesTransparentWithString(textui.text, graph);
                        previousSelectedType = textui.text;
                        previousGraph = graph;


                        //TESTS ONLY
                        //SearchOnlyWithinSelectedType("array"); //Search Only In Selected Type
                        //SearchDependenciesFilteredBySelection("externalReferences-1"); //Search in all connected neighbours
                        //SearchHierarchiesFilteredBySelection("Apache"); //Search complete Hirarchies of nodes
                        //StandardSearch("CVE");
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
            if (key == other.key || key == (other.key.Substring(0, other.key.Length - other.suffix.Length)))
            {
                ChangeNodeAndLinesTransparency(other, 1f, 0.9f, 1f);

                foreach (DataObject node in g.dataObjects)
                {
                    if (other.parent.Contains(node) || node.parent.Contains(other))
                    {
                        ld = new LineDrawer(0.04f);

                        foreach (var line in node.relationship_line_parent)
                        {
                            LineRenderer lr = line.GetComponent<LineRenderer>();

                            Vector3 startPosition = lr.GetPosition(0);
                            Vector3 endPosition = lr.GetPosition(lr.positionCount - 1);

                            if ((Vector3.Distance(startPosition, other.DataBall.transform.position) <= 0.0001f) || (Vector3.Distance(endPosition, other.DataBall.transform.position) <= 0.0001f))
                            {
                                lr.colorGradient = ld.GetBlueGradientWithTransparency(0.9f);
                            }
                        }

                    }
                }
            }

        }
    }

    public void CloseJSONPosWindow()
    {
        MakeAllNodesVisible();
        expandedNodes.Clear();
        activateWindow = false;
    }

    public void CloseSearchWindow()
    {
        ClearContentChildren();

        searchResultsPanel.SetActive(true);
        contextMenuPanel.SetActive(false);
        searchResultsMenu.SetActive(false);
    }

    public void ClearContentChildren()
    {

        foreach (Transform child in scrollViewContentSearch.GetComponent<RectTransform>())
        {
            Destroy(child.gameObject);
        }
        foreach (Transform child in scrollViewCentextMenu .GetComponent<RectTransform>())
        {
            Destroy(child.gameObject);
        }
        searchBtnDataPair.Clear();
        searchBtnDataPairDrilldown.Clear();
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

    public void MakeAllNodesInGraphInvisible(GraphReader g)
    {
        foreach (DataObject other in g.dataObjects)
        {
            ChangeNodeAndLinesTransparency(other, 0.2f, 0.1f, 0.3f);
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
                if (other.GetHashCode().ToString() == linkHashID && !expandedNodes.Contains(linkHashID + "Pos" + dropdownPositions.value))
                {
                    TextMeshProUGUI text = scrollViewContentPositions.GetComponent<TextMeshProUGUI>();

                    string displayText = textsPosition[dropdownPositions.options[dropdownPositions.value].text];
                    string addedDisplayText = "";
                    int maxSequence = GetMaxSequence(displayText);
                    string tabs = "";
                    //Debug.Log(maxSequence);

                    for (int i = 0; i < maxSequence + countTabs; i++)
                    {
                        tabs += "\t";
                    }
                    if (countTabs == 0) countTabs++;

                    //Debug.Log(other.key);

                    foreach (DataObject child in graph.dataObjects)
                    {
                        if (child.parent.Contains(other))
                        {
                            addedDisplayText += GetLineText(child, null, tabs);
                        }
                    }

                    string search = "<link=\"" + other.GetHashCode() + "\"><b>" + "{...}" + "</b></link>" + "\n";
                    int index = displayText.IndexOf(search);
                    //Debug.Log(index);

                    displayText = displayText.Substring(0, search.Length + index) + addedDisplayText + displayText.Substring(search.Length + index);

                    textsPosition[dropdownPositions.options[dropdownPositions.value].text] = displayText;
                    
                    text.text = displayText;

                    expandedNodes.Add(linkHashID + "Pos" + dropdownPositions.value);
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
        //expandedNodes.Clear();
    }

    public void AddTextToScrollContent(string text)
    {

        TextMeshProUGUI textUI = scrollViewContentPositions.GetComponent<TextMeshProUGUI>();
        textUI.text = text;
    }

    public void EventBallSizeDependingOnRelationships()
    {
        foreach (GraphReader g in sbomList)
        {
            if(changeBallSize.isOn)
            {
                g.IncreaseBallSizeDependingOnRelationships(0.65f);
            } 
            else
            {
                g.IncreaseBallSizeDependingOnRelationships(1f);
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

        searchResultsMenu.SetActive(true);
        ClearContentChildren();


        if (WithinSelectedTypeToggle.isOn)
        {
            SearchOnlyWithinSelectedType(text);
        }
        else if (HierarchiesFilteredSelectionToggle.isOn)
        {
            SearchHierarchiesFilteredBySelection(text);
        }
        else if (DependenciesFilteredSelectionToggle.isOn)
        {
            SearchDependenciesFilteredBySelection(text);
        }
        else
        {
            StandardSearch(text);
        }

    }

    public void AddNodeToScrollView(DataObject dobj, GraphReader graph)
    {
        GameObject btn = Instantiate(buttonTemplate, scrollViewContentSearch.transform);

        TextMeshProUGUI text = btn.GetComponentInChildren<TextMeshProUGUI>();

        text.enableAutoSizing = true;
        text.fontSizeMax = 7;
        text.fontSizeMin = 4;

        if(dobj.value != "")
        {
            text.text = "<color=#D52929>" + dobj.key + " </color>: " + dobj.value;
        }
        else
        {
            text.text = "<color=#D52929>" + dobj.key + " </color>";
        }

        text.fontSize = 7;
        text.margin = new Vector4(5, 0, 5, 0);

        searchBtnDataPair.Add(btn, dobj);

        btn.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => {

            List<GameObject> list = new List<GameObject>();
            searchBtnDataPair[btn].DataBall.GetNamedChild("Ball").GetChildGameObjects(list);

            TextMeshPro textmesh = list[0].GetComponent<TextMeshPro>();

            player.transform.position = searchBtnDataPair[btn].DataBall.GetNamedChild("Ball").transform.position - 2 * textmesh.transform.forward;
            player.transform.LookAt(searchBtnDataPair[btn].DataBall.transform);

            //player.transform.eulerAngles = new Vector3(player.transform.eulerAngles.x, searchBtnDataPair[btn].DataBall.transform.eulerAngles.y, player.transform.eulerAngles.z);

            PrepareContextMenu(searchBtnDataPair[btn], graph);
        });
    }

    public void PrepareContextMenu(DataObject dobj, GraphReader sbom)
    {
        foreach (Transform child in scrollViewCentextMenu.GetComponent<RectTransform>())
        {
            Destroy(child.gameObject);
        }

        searchBtnDataPairDrilldown.Clear();

        searchResultsPanel.SetActive(false);
        contextMenuPanel.SetActive(true);

        if (sbom.dataObjects.Count <= 0) return;

        List<DataObject> path = FindShortestPath(dobj, sbom.dataObjects[0]);

        foreach(var node in path)
        {
            GameObject btn = Instantiate(buttonTemplate, scrollViewCentextMenu.transform);

            TextMeshProUGUI text = btn.GetComponentInChildren<TextMeshProUGUI>();

            text.enableAutoSizing = true;
            text.fontSizeMax = 7;
            text.fontSizeMin = 4;

            if (node.value != "")
            {
                text.text = "<color=#2E6F40>" + node.level + " - " + "</color>" + "<color=#D52929>" + node.key + " </color>: " + node.value;
            }
            else
            {
                text.text = "<color=#2E6F40>" + node.level + " - " + "</color>" + "<color=#D52929>" + node.key + " </color>";
            }

            text.fontSize = 7;
            text.margin = new Vector4(5, 0, 5, 0);

            searchBtnDataPairDrilldown.Add(btn, node);

            btn.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => {

                List<GameObject> list = new List<GameObject>();
                searchBtnDataPairDrilldown[btn].DataBall.GetNamedChild("Ball").GetChildGameObjects(list);

                TextMeshPro textmesh = list[0].GetComponent<TextMeshPro>();

                player.transform.position = searchBtnDataPairDrilldown[btn].DataBall.GetNamedChild("Ball").transform.position - 2 * textmesh.transform.forward;
                player.transform.LookAt(searchBtnDataPairDrilldown[btn].DataBall.transform);
            });

            string name = node.key;

            foreach(var point in sbom.dataObjects)
            {
                if(point.parent.Contains(node) && point.key == "name")
                {
                    name = point.key + ":" + point.value;
                }
            }

            addButtonTooltip(btn.GetComponent<UnityEngine.UI.Button>(), name);
        }
    }

    public void addButtonTooltip(UnityEngine.UI.Button button, string text)
    {
        EventTrigger trigger = button.AddComponent<EventTrigger>();

        EventTrigger.Entry enterEvent = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerEnter
        };
        enterEvent.callback.AddListener((data) => ShowTooltip(text, button.transform.position));
        trigger.triggers.Add(enterEvent);

        EventTrigger.Entry exitEvent = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerExit
        };
        exitEvent.callback.AddListener((data) => HideTooltip());
        trigger.triggers.Add(exitEvent);
    }
    void ShowTooltip(string message, Vector3 pos)
    {
        List<GameObject> children = new List<GameObject>();
        tooltip.GetChildGameObjects(children);
        children[1].GetComponent<TextMeshProUGUI>().text = message;
        tooltip.SetActive(true);
        tooltip.transform.position = new Vector3(tooltip.transform.position.x, pos.y, tooltip.transform.position.z);
    }

    void HideTooltip()
    {
        tooltip.SetActive(false);
    }

    public void ResetSearch(GraphReader graph)
    {
        foreach (DataObject obj in graph.dataObjects)
        {
            ChangeNodeAndLinesTransparency(obj, 1f, 0.9f, 1f);
        }

        inputSearch.text = "";
        previousGraph = null;
        previousSelectedType = "";

        ClearContentChildren();
    }

    public void ResetSearchAll()
    {
        foreach (GraphReader graph in sbomList)
        {
            foreach (DataObject obj in graph.dataObjects)
            {
                ChangeNodeAndLinesTransparency(obj, 1f, 0.9f, 1f);
            }
        }

        inputSearch.text = "";
        previousGraph = null;
        previousSelectedType = "";

        CloseSearchWindow();
    }

    public void StandardSearch(string text)
    {
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
                        AddNodeToScrollView(obj, graph);
                        ChangeNodeAndLinesTransparency(obj, 1f, 0.9f, 1f);
                    }
                }
            }
            else
            {
                ResetSearch(graph);
            }
        }
    }

    public void SearchOnlyWithinSelectedType(string text)
    {

        MakeAllNodesInGraphInvisible(previousGraph);

        List<DataObject> selectedTypeNodes = new List<DataObject>();

        foreach (DataObject other in previousGraph.dataObjects)
        {
            if (previousSelectedType == other.key || previousSelectedType == (other.key.Substring(0, other.key.Length - other.suffix.Length)))
            {
                selectedTypeNodes.Add(other);
            }
        }

        if (text != null && text != "")
        {
            foreach (DataObject dobj in selectedTypeNodes)
            {
                if (dobj.key.Contains(text) || dobj.value.Contains(text))
                {
                    AddNodeToScrollView(dobj, previousGraph);
                    ChangeOnlyNodeTransparency(dobj, 1f, 1f);
                }
            }
        }
        else
        {
            ResetSearch(previousGraph);
        }
    }


    public void SearchHierarchiesFilteredBySelection(string text)
    {
        MakeAllNodesInGraphInvisible(previousGraph);

        foundPaths.Clear();

        List<DataObject> selectedTypeNodes = new List<DataObject>();

        foreach (DataObject other in previousGraph.dataObjects)
        {
            if (previousSelectedType == other.key || previousSelectedType == (other.key.Substring(0, other.key.Length - other.suffix.Length)))
            {
                selectedTypeNodes.Add(other);
            }
        }

        if (text != null && text != "")
        {
            foreach (DataObject dobj in previousGraph.dataObjects)
            {
                if (dobj.key.Contains(text) || dobj.value.Contains(text))
                {
                    RecursiveSearchConnectionInTree(dobj, dobj, selectedTypeNodes);
                }
            }


            List<List<DataObject>> shortestPath = new List<List<DataObject>>();

            foreach (var path in foundPaths)
            {
                shortestPath.Add(FindShortestPath(path.Item1,path.Item2));
            }

            foreach(var path in shortestPath)
            {
                for (int i = 0; i < shortestPath.Count; i++)
                {
                    if (i + 1 < path.Count)
                    {
                        ChangeOnlyLineTransparency(path[i], path[i + 1], 0.9f);
                        ChangeOnlyLineTransparency(path[i + 1], path[i], 0.9f);
                    }

                }
            }

        }
        else
        {
            ResetSearch(previousGraph);
        }
    }

    public static List<DataObject> FindShortestPath(DataObject startNode, DataObject endNode)
    {
        Queue<Tuple<DataObject, List<DataObject>>> queue = new Queue<Tuple<DataObject, List<DataObject>>>();
        queue.Enqueue(new Tuple<DataObject, List<DataObject>>(startNode, new List<DataObject> { startNode }));

        HashSet<DataObject> visited = new HashSet<DataObject> { startNode };

        while (queue.Count > 0)
        {
            var (currentNode, path) = queue.Dequeue();

            if (currentNode == endNode)
                return path;

            foreach (var neighbor in currentNode.parent)
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    var newPath = new List<DataObject>(path) { neighbor };
                    queue.Enqueue(new Tuple<DataObject, List<DataObject>>(neighbor, newPath));
                }
            }
        }
        return null;
    }

    List<(DataObject, DataObject)> foundPaths = new List<(DataObject, DataObject)>();    

    public void RecursiveSearchConnectionInTree(DataObject startNode, DataObject node, List<DataObject>  selectedTypeNodes)
    {

        if (node == null || node.parent.Count <= 0)
        {
            return;
        }

        foreach (DataObject parent in node.parent)
        {
            if (selectedTypeNodes.Contains(parent))
            {
                ChangeOnlyNodeTransparency(startNode, 1f, 1f);
                ChangeOnlyNodeTransparency(parent, 1f, 1f);

                foundPaths.Add((startNode, parent));
                AddNodeToScrollView(startNode, previousGraph);

                RecursiveSearchConnectionInTree(startNode, parent, selectedTypeNodes);
            }
            else
            {
                RecursiveSearchConnectionInTree(startNode, parent, selectedTypeNodes);
            }

        }
    }


    public void SearchDependenciesFilteredBySelection(string text)
    {

        MakeAllNodesInGraphInvisible(previousGraph);

        List<DataObject> selectedTypeNodes = new List<DataObject>();

        foreach (DataObject other in previousGraph.dataObjects)
        {
            if (previousSelectedType == other.key || previousSelectedType == (other.key.Substring(0, other.key.Length - other.suffix.Length)))
            {
                selectedTypeNodes.Add(other);
            }
        }

        if (text != null && text != "")
        {
            foreach (DataObject obj in previousGraph.dataObjects)
            {
                foreach (DataObject dobj in selectedTypeNodes)
                {
                    if (dobj.parent.Contains(obj) || obj.parent.Contains(dobj))
                    {
                        if(obj.key.Contains(text) ||  obj.value.Contains(text))
                        {
                            ChangeOnlyNodeTransparency(obj, 1f, 1f);
                            ChangeOnlyNodeTransparency(dobj, 1f, 1f);
                            ChangeOnlyLineTransparency(obj, dobj, 0.9f);
                            ChangeOnlyLineTransparency(dobj, obj, 0.9f);
                            AddNodeToScrollView(obj, previousGraph);
                        }
                    }
                }
            }
        }
        else
        {
            ResetSearch(previousGraph);
        }
    }

    public void ShowUnrelevantNodes()
    {
        if (showUnrelevantNodes.isOn)
        {
            foreach (var sbom in sbomList)
            {
                if (sbom.isComparisonGraph)
                {
                    foreach(DataObject obj in sbom.dataObjects)
                    {
                        if (!obj.modifiedStatus)
                        {
                            ChangeNodeAndLinesTransparency(obj, 0.2f, 0.1f, 0.3f);
                        }
                    }
                }
            }
        }
        else
        {
            foreach (var sbom in sbomList)
            {
                if (sbom.isComparisonGraph)
                {
                    foreach (DataObject obj in sbom.dataObjects)
                    {
                        ChangeNodeAndLinesTransparency(obj, 1f, 0.9f, 1f);
                    }
                }
            }
        }
    }

    public void EnableLayerGlow()
    {
        foreach(var sbom in sbomList)
        {
            foreach(DataObject node in sbom.dataObjects)
            {
                List<GameObject> children = new List<GameObject>();
                node.DataBall.GetNamedChild("Ball").GetChildGameObjects(children);

                if (enableLayerColors.isOn)
                {
                    children[3].SetActive(true);
                }
                else
                {
                    children[3].SetActive(false);
                }

            }

            if(enableLayerColors.isOn)
            {
                sbom.glowEnabled = true;
                sbom.CreateGlowCubeLegend();
            }
            else
            {
                sbom.glowEnabled = false;
                sbom.CreateGlowCubeLegend();
            }
            
        }
    }

    public void SwitchSearchMenus()
    {
        searchResultsPanel.SetActive(true);
        contextMenuPanel.SetActive(false);
    }

}
