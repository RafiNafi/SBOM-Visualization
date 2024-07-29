using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataObject
{
    public GameObject DataBall;
    public int level = 0;
    public string key;
    public string value;
    public DataObject parent;
    public GameObject relationtship_line_parent;
    public int nr_children = 0;
    public bool isExpanded = true;
    //public Vector3 line_position1, line_position2;
    //public List<DataObject> children;

    // ALle Objekte mit Bälle auf einen Punkt generieren und in Liste speichern => Bälle positionieren und parallel Beziehungen über Linien erstellen

    public DataObject(GameObject ball,int lvl,string key, string value, DataObject p)
    {
        this.DataBall = ball;
        this.level = lvl;
        this.key = key;
        this.value = value;
        this.parent = p;
    }
}
