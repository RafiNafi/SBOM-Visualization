using DnsClient;
using MongoDB.Bson;
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
public class MenuInteraction : MonoBehaviour
{

    public DatabaseDataHandler dbHandler;

    public GameObject scrollViewContent;
    public GameObject buttonTemplate;

    public UnityEngine.UI.Slider sliderLevel;
    public TextMeshProUGUI sliderText;

    public TMP_InputField inputSearch;

    public TMP_Dropdown dropdown;

    public UnityEngine.UI.Toggle showCWEToggle;
    public UnityEngine.UI.Toggle showDuplicateNodesToggle;

    public List<GraphReader> cveList = new List<GraphReader>();
    public List<GraphReader> sbomList = new List<GraphReader>();

    public GameObject BallPrefab;

    public Canvas c;


    // Start is called before the first frame update
    void Start()
    {
        AddScrollviewContent();
        SetupInputField();

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

            btn.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => SelectSBOM(text.text));
        }
    }

    public void SelectSBOM(string name)
    {
        
        StartCoroutine(dbHandler.GetDatabaseDataById(name, SBOMCreation));

    }

    public void SBOMCreation(string name, string bsonElements)
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

        //else create new graph
        GraphReader newGraph = new GraphReader();
        newGraph.BallPrefab = BallPrefab;
        newGraph.CreateGraph(bsonElements, dropdown.options[dropdown.value].text, showDuplicateNodesToggle.isOn);
        newGraph.dbid = name;
        sbomList.Add(newGraph);
        InitSliders();
        PositionAllGraphs(sbomList);
    }

    public void SetupInputField()
    {
        inputSearch.onSelect.AddListener( x => OpenKeyboard());
        
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
        
        string field = "cveMetadata.cveId";
        //string searchCWE_Name = "Log4j";
        //string field = "containers.cna.affected.product";

        List<string> CVEIds = new List<string>();

        foreach(var graph in sbomList)
        {
            foreach(DataObject dobj in graph.dataObjects)
            {
                if(dobj.key.Contains("CVE"))
                {
                    CVEIds.Add(dobj.key);
                }
            }
        }

        string json = JsonUtility.ToJson(new StringListWrapper { strings = CVEIds });

        //StartCoroutine(dbHandler.GetCVEDataBySubstringAndField(searchCWE_ID, field, ShowAllCVENodes));
        StartCoroutine(dbHandler.GetAllCVEDataBySubstringAndField(json, field, ShowAllCVENodes));
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
        /*
        for (int i = 0; i < sbomList.Count; i++)
        {
            float radius = 1f;

            if (Mathf.Sin(Mathf.PI / sbomList.Count) > 0)
            {
                radius = CalculateMaxCircleRadius() / Mathf.Sin(Mathf.PI / sbomList.Count);
            }
            int graphCount = sbomList.Count;
            float angle = (i * Mathf.PI * 2f) / graphCount;
            Vector3 move = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            sbomList[i].AdjustEntireGraphPosition(move);
        }
        */
        //float countRadius = (sbomList.Count * maxRadius) / 2;

        int multiplier = 1;

        if (list.Count > 0)
        {
            if (list[0].isCVE)
            {
                multiplier = -1;
            }
        }

        float maxRadius = CalculateMaxCircleRadius();
        float previousValues = 0;
        Vector3 edge = Vector3.zero;

        for (int i = 0; i < list.Count; i++)
        {
            float radius = GetGraphRadiusX(list[i].BoundaryBox);

            Vector3 move = new Vector3(multiplier * maxRadius + (multiplier * maxRadius / 2), 0, edge.z);
            list[i].AdjustEntireGraphPosition(move - list[i].offset);
            list[i].offset = move;


            edge = new Vector3(list[i].BoundaryBox.GetComponent<Renderer>().bounds.min.x, 0, list[i].BoundaryBox.GetComponent<Renderer>().bounds.max.z);

            if (i - 1 >= 0)
            {
                float newZ = list[i-1].BoundaryBox.GetComponent<Renderer>().bounds.max.z - list[i].BoundaryBox.GetComponent<Renderer>().bounds.min.z;
                list[i].AdjustEntireGraphPosition(new Vector3(0, 0, newZ + 5));
                list[i].offset += new Vector3(0,0,newZ + 5);

            }

            Vector3 adjustCategoriesEdge = new Vector3(list[i].BoundaryBox.GetComponent<Renderer>().bounds.min.x - 2, 0, list[i].BoundaryBox.GetComponent<Renderer>().bounds.min.z);

            PositionCategoryBalls(list[i], adjustCategoriesEdge - list[i].offsetCategories);
            list[i].offsetCategories = adjustCategoriesEdge;

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
        // Calculate a base radius that ensures no overlap
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

    public float GetGraphRadiusZ(GameObject obj)
    {
        var bounds = obj.GetComponent<Renderer>().bounds;
        return bounds.extents.z;
    }

}
