using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class DataObject
{
    public GameObject DataBall;
    public int level = 0;
    public string key;
    public string value;
    public List<DataObject> parent = new List<DataObject>();
    public List<GameObject> relationship_line_parent = new List<GameObject>();
    public int nr_children = 0;
    public bool isExpanded = true;
    public Vector3 velocity;
    public string suffix = "";
    public int lineNumber = 0;
    public List<DataObject> children = new List<DataObject>();
    public int nr_relationships = 0;
    public bool modifiedStatus = false;

    public DataObject(GameObject ball,int lvl,string key, string value, DataObject p, int lineNumber)
    {
        this.DataBall = ball;
        this.level = lvl;
        this.key = key;
        this.value = value;
        this.velocity = Vector3.zero;
        this.lineNumber = lineNumber;

        if (p != null)
        {
            this.parent.Add(p);
        }

        List<GameObject> children = new List<GameObject>();
        DataBall.GetNamedChild("Ball").GetChildGameObjects(children);
        Renderer ballRenderer = children[3].GetComponent<Renderer>();
        ballRenderer.material.SetColor("_EmissionColor", layerColorPair[level] * 3);
    }

    public Dictionary<int, UnityEngine.Color> layerColorPair = new Dictionary<int, UnityEngine.Color>()
    {
        { 0, UnityEngine.Color.gray },                   // Standard
        { 1, UnityEngine.Color.red },                    // Red
        { 2, UnityEngine.Color.green },                  // Green
        { 3, UnityEngine.Color.blue },                   // Blue
        { 4, UnityEngine.Color.yellow },                 // Yellow
        { 5, UnityEngine.Color.magenta },                // Magenta
        { 6, UnityEngine.Color.cyan },                   // Cyan
        { 7, new UnityEngine.Color(1f, 0.5f, 0f) },      // Orange
        { 8, new UnityEngine.Color(0.5f, 0f, 0.5f) },    // Purple
        { 9, UnityEngine.Color.white },                  // White
        { 10, new UnityEngine.Color(0.7f, 0.7f, 0.7f) }, // Light Gray
        { 11, new UnityEngine.Color(0f, 0.5f, 0.5f) },   // Teal
        { 12, new UnityEngine.Color(0.5f, 0.5f, 0f) },   // Olive
        { 13, new UnityEngine.Color(0.8f, 0.4f, 0.6f) }, // Pinkish
        { 14, new UnityEngine.Color(0.5f, 0.2f, 0.1f) }, // Brown
        { 15, new UnityEngine.Color(0f, 0.4f, 0.2f) },   // Dark Green
        { 16, new UnityEngine.Color(0.3f, 0f, 0.5f) },   // Deep Purple
        { 17, new UnityEngine.Color(0.5f, 0.1f, 0.4f) }, // Dark Pink
        { 18, new UnityEngine.Color(0.1f, 0.3f, 0.9f) }, // Dark Blue
        { 19, new UnityEngine.Color(0.9f, 0.6f, 0.2f) }, // Gold
        { 20, new UnityEngine.Color(0.3f, 0.8f, 0.6f) }, // Mint Green
    };
}
