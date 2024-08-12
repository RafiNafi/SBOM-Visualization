using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class LineDrawer
{

    GameObject newLine;
    LineRenderer lineRenderer;
    float lineW = 0.04f;

    public LineDrawer(float lineW)
    {
        this.lineW = lineW;
    }

    public GameObject CreateLine(List<Vector3> pointlist, bool isCVE)
    {
        newLine = new GameObject();
        lineRenderer = newLine.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

        if (isCVE) 
        {
            lineRenderer.startColor = new Color(1, 0, 0, 0.9f);
            lineRenderer.endColor = new Color(1, 0, 0, 0.9f);
        } 
        else
        {
            lineRenderer.startColor = new Color(0, 0, 1, 0.9f);
            lineRenderer.endColor = new Color(0, 0, 1, 0.9f);
        }

        lineRenderer.startWidth = lineW;
        lineRenderer.endWidth = lineW;

        lineRenderer.positionCount = 2;
        lineRenderer.SetPositions(pointlist.ToArray());
        
        return newLine;
    }

    public GameObject DrawCube(Vector3 graphMin, Vector3 graphMax)
    {
        newLine = new GameObject();
        lineRenderer = newLine.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = new Color(0, 0, 0, 0.8f);
        lineRenderer.endColor = new Color(0, 0, 0, 0.8f);
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;

        Vector3[] vertices = new Vector3[]
        {
            graphMin,
            new Vector3(graphMax.x, graphMin.y, graphMin.z),
            new Vector3(graphMax.x, graphMin.y, graphMax.z),
            new Vector3(graphMin.x, graphMin.y, graphMax.z),
            graphMin,
            new Vector3(graphMin.x, graphMax.y, graphMin.z),
            new Vector3(graphMax.x, graphMax.y, graphMin.z),
            graphMax,
            new Vector3(graphMin.x, graphMax.y, graphMax.z),
            new Vector3(graphMin.x, graphMax.y, graphMin.z),
            new Vector3(graphMin.x, graphMin.y, graphMin.z),
            new Vector3(graphMin.x, graphMin.y, graphMax.z),
            new Vector3(graphMin.x, graphMax.y, graphMax.z),
            graphMax,
            new Vector3(graphMax.x, graphMin.y, graphMax.z),
            new Vector3(graphMax.x, graphMin.y, graphMin.z),
            new Vector3(graphMax.x, graphMax.y, graphMin.z)
        };

        lineRenderer.positionCount = vertices.Length;
        lineRenderer.SetPositions(vertices);

        return newLine;
    }
}
