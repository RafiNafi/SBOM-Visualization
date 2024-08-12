using System.Collections;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;

public class GraphCreationScript : MonoBehaviour
{
    public GameObject PointPrefab;

    // Start is called before the first frame update
    void Start()
    {
        //CreateGraphAndLines();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void CreateGraphAndLines()
    {
        LineDrawer ld = new LineDrawer(0.04f);

        int ballCount = 50;
        Vector3 previousV = new Vector3(1, 1, 1); //init vector

        for (var i = 0; i < ballCount; i++)
        {
            float angle = i * Mathf.PI * 2f / ballCount;
            GameObject dataPoint = Instantiate(PointPrefab, new Vector3(Mathf.Cos(angle) * (ballCount / 5), 2, Mathf.Sin(angle) * (ballCount / 5)), Quaternion.identity);
            TextMeshPro text = dataPoint.GetComponentInChildren<TextMeshPro>();
            text.text = i.ToString();
            dataPoint.GetComponentInChildren<Renderer>().material.color = new Color(0, 0, 1, 1.0f);

            //Lines Drawing 
            if (i != 0)
            {
                List<Vector3> pointlist = new List<Vector3>();
                pointlist.Add(previousV);
                pointlist.Add(new Vector3(Mathf.Cos(angle) * (ballCount / 5), 2, Mathf.Sin(angle) * (ballCount / 5)));
                ld.CreateLine(pointlist, false);
            }

            //previousData
            previousV = new Vector3(Mathf.Cos(angle) * (ballCount / 5), 2, Mathf.Sin(angle) * (ballCount / 5));

        }
    }
}
