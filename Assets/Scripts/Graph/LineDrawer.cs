using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
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

        lineRenderer.startWidth = lineW;
        lineRenderer.endWidth = lineW;

        Vector3 midPoint = Vector3.Lerp(pointlist[0], pointlist[1], 0.8f);
        pointlist.Insert(1, midPoint);

        lineRenderer.positionCount = 3;
        lineRenderer.SetPositions(pointlist.ToArray());

        Gradient g = new Gradient();

        if (isCVE)
        {

            g = GetRedGradientWithTransparency(0.9f);
        }
        else
        {
            g = GetBlueGradientWithTransparency(0.9f);
        }

        lineRenderer.colorGradient = g;

        return newLine;
    }

    public Gradient GetBlueGradientWithTransparency(float transparency)
    {
        Gradient g = new Gradient();

        var colors = new GradientColorKey[]{
            new GradientColorKey(new Color(0, 0, 1, transparency), 0.0f),
            new GradientColorKey(new Color(0, 0, 1, transparency), 0.8f),
            new GradientColorKey(new Color(65f / 255f, 2220f / 255f, 210f / 255f, transparency),1f)
            };

        GradientAlphaKey[] alphas = new GradientAlphaKey[] {

                new GradientAlphaKey(transparency,0f),
                new GradientAlphaKey(transparency,1f)
        };

        g.SetKeys(colors, alphas);

        return g;
    }

    public Gradient GetRedGradientWithTransparency(float transparency)
    {
        Gradient g = new Gradient();

        var colors = new GradientColorKey[]{
            new GradientColorKey(new Color(1, 0, 0, transparency), 0.0f),
            new GradientColorKey(new Color(1, 0, 0, transparency), 0.8f),
            new GradientColorKey(new Color(1f, 200f/255f, 200f/255f, transparency),1f)
            };

        GradientAlphaKey[] alphas = new GradientAlphaKey[] {

                new GradientAlphaKey(transparency,0f),
                new GradientAlphaKey(transparency,1f)
        };

        g.SetKeys(colors, alphas);

        return g;
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
