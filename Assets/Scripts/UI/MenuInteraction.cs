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

    /*
    public void ExpandToggle(bool isOn)
    {
        int levelCap = 4;

        Debug.Log(isOn);

        foreach (DataObject ball in reader.dataObjects)
        {
            if (ball.level > levelCap)
            {
                ball.DataBall.SetActive(isOn);
                ball.relationship_line_parent.SetActive(isOn);
            }
        }

    }
    */

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
        reader.CreateGraph(bsonElements);
        Debug.Log(name);
        InitSliders();
    }
}
