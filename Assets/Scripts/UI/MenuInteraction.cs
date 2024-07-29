using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using UnityEngine;

public class MenuInteraction : MonoBehaviour
{
    public InputReader reader;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

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


}
