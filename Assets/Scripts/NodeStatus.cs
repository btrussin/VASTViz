using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeStatus : MonoBehaviour {

    public ipInfo currIpInfo;

    public GameObject bgHighlightQuad;
    public GameObject bgSelectedQuad;

    int numHighlight = 0;

    bool currentlySelected = false;

    void Start()
    {

    }

    void Update()
    {
        
    }

    public void highlightNode()
    {
        numHighlight++;

        if (currentlySelected) return;

        if (numHighlight > 0)
        {
            bgHighlightQuad.SetActive(true);
        }
    }

    public void unhighlightNode()
    {
        numHighlight--;

        if (currentlySelected) return;

        if (numHighlight == 0)
        {
            bgHighlightQuad.SetActive(false);
        }
    }

    public void selectNode()
    {
        currentlySelected = true;

        bgHighlightQuad.SetActive(false);
        bgSelectedQuad.SetActive(true);
    }

    public void unselectNode()
    {
        currentlySelected = false;

        bgSelectedQuad.SetActive(false);

        if (numHighlight > 0) bgHighlightQuad.SetActive(true);
    }
}
