using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LineDrawer
{

    GameObject newLine;
    LineRenderer lineRenderer;
    float lineW = 0.04f;

    public LineDrawer(float lineW)
    {
        this.lineW = lineW;
    }

    public void CreateLine(List<Vector3> pointlist)
    {
        newLine = new GameObject();
        lineRenderer = newLine.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = new Color(0, 0, 1, 0.8f);
        lineRenderer.endColor = new Color(0, 0, 1, 0.8f);
        lineRenderer.startWidth = lineW;
        lineRenderer.endWidth = lineW;

        lineRenderer.positionCount = 2;
        lineRenderer.SetPositions(pointlist.ToArray());
        
    }
}
