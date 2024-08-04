using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    //public Vector3 line_position1, line_position2;
    //public List<DataObject> children;

    // ALle Objekte mit Bälle auf einen Punkt generieren und in Liste speichern => Bälle positionieren und parallel Beziehungen über Linien erstellen

    public DataObject(GameObject ball,int lvl,string key, string value, DataObject p)
    {
        this.DataBall = ball;
        this.level = lvl;
        this.key = key;
        this.value = value;
        this.velocity = Vector3.zero;

        if (p != null)
        {
            this.parent.Add(p);
        }
    }
}
