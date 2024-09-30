using System.Collections;
using System.Collections.Generic;
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
    }
}
