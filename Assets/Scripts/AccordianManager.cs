﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AccordianManager : MonoBehaviour {

    public GameObject backQuad;
    public GameObject layer1;

    public GameObject sphereKnob;


    public GameObject subnet1;
    public GameObject subnet2;
    public GameObject subnet3;

    SubnetMapping submap1 = null;
    SubnetMapping submap2 = null;
    SubnetMapping submap3 = null;

    // Use this for initialization
    void Start () {
        submap1 = subnet1.GetComponent<SubnetMapping>();
        submap2 = subnet2.GetComponent<SubnetMapping>();
        submap3 = subnet3.GetComponent<SubnetMapping>();
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    public void activate(Vector3 pos, Vector3 dir)
    {
        
        if( !submap1 || !submap2 || !submap3 )
        {
            submap1 = subnet1.GetComponent<SubnetMapping>();
            submap2 = subnet2.GetComponent<SubnetMapping>();
            submap3 = subnet3.GetComponent<SubnetMapping>();
        }

        float dist = submap1.currLayerDist;
        gameObject.SetActive(true);
        gameObject.transform.forward = dir;
        gameObject.transform.position = pos + dir * dist * 2.0f;
        updateSubLayerReps(dist);
    }

    public void deactivate()
    {
        gameObject.SetActive(false);
    }

    public void tryToAdjustDistance(Vector3 pos)
    {
        if (!gameObject.activeInHierarchy) return;

        Vector3 v = pos - gameObject.transform.position;
        float d = Vector3.Dot(v, gameObject.transform.forward);
        if (d > 0.0f) return;

        float halfDist = -d * 0.5f;
        submap1.updateDistance(halfDist);
        submap2.updateDistance(halfDist);
        submap3.updateDistance(halfDist);

        updateSubLayerReps(halfDist);
    }

    void updateSubLayerReps(float dist)
    {
        layer1.transform.localPosition = new Vector3(0.0f, 0.0f, -dist);
        sphereKnob.transform.localPosition = new Vector3(0.0f, 0.0f, -dist *2.0f);
    }


}
