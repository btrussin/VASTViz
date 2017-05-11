using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NFPointManager : MonoBehaviour {

    public ipType nodeType;
    public GameObject lineBaseObject = null;
    public GameObject lineObject = null;
    public bool isActive = false;

    LineRenderer lineRend;
    Vector3[] lineVerts = new Vector3[2];

    // Use this for initialization
    void Start () {
        isActive = false;
    }
	
	// Update is called once per frame
	void Update () {
		if( isActive )
        {
            lineVerts[0] = lineBaseObject.transform.position;
            lineVerts[1] = gameObject.transform.position;

            lineRend.SetPositions(lineVerts);
        }
	}

    public void setLineRendObject(GameObject obj)
    {
        lineObject = obj;
        lineRend = lineObject.GetComponent<LineRenderer>();
        deactivate();
    }

    public void activate()
    {
        gameObject.SetActive(true);

        if( lineBaseObject != null && lineObject != null )
        {
            isActive = true;
            lineObject.SetActive(true);
        }
    }

    public void deactivate()
    {
        gameObject.SetActive(false);
        isActive = false;

        if (lineBaseObject != null && lineObject != null)
        {
            lineObject.SetActive(false);
        }
    }
}
