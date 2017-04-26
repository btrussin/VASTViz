using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeStatus : MonoBehaviour {

    public ipInfo currIpInfo;

    public GameObject bgHighlightQuad;

    int numHighlight = 0;

    void Start()
    {

    }

    void Update()
    {
        
    }

    public void highlightNode()
    {
        numHighlight++;
        if( numHighlight == 1 )
        {
            bgHighlightQuad.SetActive(true);
        }
    }

    public void unhighlightNode()
    {
        numHighlight--;
        if (numHighlight == 0)
        {
            bgHighlightQuad.SetActive(false);
        }
    }
}
