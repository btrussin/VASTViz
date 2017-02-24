using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraOrientedBehavior : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        if( gameObject.activeInHierarchy )
        {
            Vector3 v = gameObject.transform.position - Camera.main.transform.position;
            v.Normalize();
            gameObject.transform.forward = v;
        }
    }
}
