using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveScalePlayManager : MoveScaleManager
{

	// Use this for initialization
	void Start () {
        subnet = gameObject.transform.parent.gameObject;
        moveQuad = gameObject;
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    new public void initScale(){}

    new public void updateScale(Ray ray){}
}
