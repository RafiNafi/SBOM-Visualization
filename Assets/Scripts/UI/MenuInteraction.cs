using MongoDB.Bson;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices.ComTypes;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

public class MenuInteraction : MonoBehaviour
{
    public InputReader reader;

    public DatabaseDataHandler dbHandler;

    public GameObject scrollViewContent;
    public GameObject buttonTemplate;

    public UnityEngine.UI.Slider sliderLevel;
    public TextMeshProUGUI sliderText;

    public TMP_InputField inputSearch;

    public TMP_Dropdown dropdown;

    // Start is called before the first frame update
    void Start()
    {
        AddScrollviewContent();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void InitSliders()
    {
        int levelCap = 0;

        levelCap = reader.level_occurrences.Count;

        sliderText.text = "Show Layers: " + levelCap;
        sliderLevel.maxValue = levelCap;
        sliderLevel.value = levelCap;

        sliderLevel.onValueChanged.RemoveAllListeners();

        sliderLevel.onValueChanged.AddListener((y) =>
        {
            sliderText.text = "Show Layers: " + y.ToString();

            foreach (DataObject ball in reader.dataObjects)
            {

                if (ball.level + 1 > y)
                {
                    
                    ball.DataBall.SetActive(false);
                    if(ball.relationship_line_parent.Count > 0)
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
        });
    }

    public void AddScrollviewContent()
    {
        List<string> list = dbHandler.GetOnlyAllDocumentNames();


        foreach(string name in list)
        {
            GameObject btn = Instantiate(buttonTemplate, scrollViewContent.transform);
            
            TextMeshProUGUI text = btn.GetComponentInChildren<TextMeshProUGUI>();
            text.text = name;
            text.fontSize = 7;
            
            btn.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(()=>SelectSBOM(text.text));
        }

    }

    public void SelectSBOM(string name)
    {
        
        BsonDocument bsonElements = dbHandler.GetDatabaseDataById(name);
        reader.CreateGraph(bsonElements, dropdown.options[dropdown.value].text);
        Debug.Log(name);
        InitSliders();

    }


    public void OpenKeyboard()
    {
        
    }

    public void HightlightSearchedNode()
    {
        string text = inputSearch.text;

        if(text != null && text != "")
        {
            foreach (DataObject obj in reader.dataObjects)
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
            foreach (DataObject obj in reader.dataObjects)
            {
                ChangeNodeTransparency(obj, 1f, 0.9f);
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

        reader.PositionDataBalls(dropdown.options[dropdown.value].text);

        InitSliders();
    }
}
