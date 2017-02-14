using UnityEngine;
using System.Collections;

public class TimelineScript : MonoBehaviour {

    public Material lineMaterial;
    Color color = Color.white;

    // Use this for initialization
    void Start () {
        //lineMaterial = new Material(Shader.Find("Sprites/Default"));
    }
	
	// Update is called once per frame
	void Update () {
	
	}

    public void createLines(int[] countVals)
    {

        float[] tmpVals = new float[countVals.Length];
        Vector3[] pts = new Vector3[countVals.Length];

        float max = 0.0f;

        for(int i = 0; i < countVals.Length; i++)
        {
            tmpVals[i] = (float)countVals[i];
            if (tmpVals[i] > max) max = tmpVals[i];
        }

        float currX = -0.5f;
        float xInc = 1.0f / (float)(countVals.Length-1);

        float yScale = 0.1f / max;

        Vector3 tmpV;

        Vector3 up = transform.up;
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        for (int i = 0; i < countVals.Length; i++)
        {
            tmpV = new Vector3(currX, tmpVals[i] * yScale, 0.0f);
            
            pts[i].x = Vector3.Dot(tmpV, right);
            pts[i].y = Vector3.Dot(tmpV, up);
            pts[i].z = Vector3.Dot(tmpV, forward);

            pts[i] += transform.position;

            currX += xInc;
        }

        GameObject connCurve = new GameObject();

        connCurve.transform.SetParent(gameObject.transform);

        connCurve.AddComponent<LineRenderer>();
        LineRenderer rend = connCurve.GetComponent<LineRenderer>();
        rend.SetWidth(0.003f, 0.003f);
        rend.SetColors(color, color);
        rend.SetVertexCount(countVals.Length);
        rend.material = lineMaterial;

        rend.SetPositions(pts);




        Vector3[] basePts = new Vector3[2];
        basePts[0] = new Vector3(-0.5f, -0.05f, 0.0f);
        basePts[1] = new Vector3(0.5f, -0.05f, 0.0f);

        for (int i = 0; i < 2; i++)
        {
            tmpV = basePts[i];

            basePts[i].x = Vector3.Dot(tmpV, right);
            basePts[i].y = Vector3.Dot(tmpV, up);
            basePts[i].z = Vector3.Dot(tmpV, forward);

            basePts[i] += transform.position;
        }

        GameObject baseLine = new GameObject();

        baseLine.transform.SetParent(gameObject.transform);

        baseLine.AddComponent<LineRenderer>();
        rend = baseLine.GetComponent<LineRenderer>();
        rend.SetWidth(0.003f, 0.003f);
        rend.SetColors(color, color);
        rend.SetVertexCount(2);
        rend.material = lineMaterial;

        rend.SetPositions(basePts);


        basePts[0] = new Vector3(-0.5f, -0.05f, 0.0f);
        basePts[1] = new Vector3(-0.5f, 0.1f, 0.0f);

        for (int i = 0; i < 2; i++)
        {
            tmpV = basePts[i];

            basePts[i].x = Vector3.Dot(tmpV, right);
            basePts[i].y = Vector3.Dot(tmpV, up);
            basePts[i].z = Vector3.Dot(tmpV, forward);

            basePts[i] += transform.position;
        }

        GameObject currLine = new GameObject();

        currLine.transform.SetParent(gameObject.transform);

        currLine.AddComponent<LineRenderer>();
        rend = currLine.GetComponent<LineRenderer>();
        rend.SetWidth(0.003f, 0.003f);
        rend.SetColors(Color.green, Color.green);
        rend.SetVertexCount(2);
        rend.material = lineMaterial;

        rend.SetPositions(basePts);

    }



}
