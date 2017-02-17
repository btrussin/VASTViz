﻿using UnityEngine;
using System.Collections;

public class TimelineScript : MonoBehaviour {

    public Material lineMaterial;
    Color color = Color.white;
    public GameObject slider;
    public GameObject sliderLine;
    public GameObject sliderPoint;

    public GameObject baseLine;

    float stepDistance;
    Vector3 currPosition;

    Color activeColor;
    Color hightlightColor;
    Color normalColor;

    // Use this for initialization
    void Start() {
        activeColor = new Color32(255, 255, 0, 255);
        hightlightColor = new Color32(0, 255, 0, 255);
        normalColor = new Color32(0, 128, 0, 255);
    }

    // Update is called once per frame
    void Update() {
        if (Input.GetKeyDown(KeyCode.M))
        {
            Vector3 p = slider.transform.localPosition;
            Debug.Log("slider localPosition: " + p);
            p.x += stepDistance;
            slider.transform.localPosition = p;

            p = slider.transform.position;
            Debug.Log("slider position: " + p);
        }
    }

    public void createLines(int[] countVals)
    {
        stepDistance = 1.0f / (float)(countVals.Length - 1);


        float[] tmpVals = new float[countVals.Length];
        Vector3[] pts = new Vector3[countVals.Length];

        float max = 0.0f;

        for (int i = 0; i < countVals.Length; i++)
        {
            tmpVals[i] = (float)countVals[i];
            if (tmpVals[i] > max) max = tmpVals[i];
        }

        float currX = -0.5f;
        float xInc = 1.0f / (float)(countVals.Length - 1);

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
        rend.startWidth = 0.003f;
        rend.endWidth = 0.003f;
        rend.startColor = color;
        rend.endColor = color;
        rend.numPositions = countVals.Length;

        rend.material = lineMaterial;

        rend.SetPositions(pts);
    }

    public void hightlightControlLine()
    {
        LineRenderer lineRend = sliderLine.GetComponent<LineRenderer>();
        lineRend.startColor = hightlightColor;
        lineRend.endColor = hightlightColor;
        Renderer rend = sliderPoint.GetComponent<Renderer>();
        rend.material.color = hightlightColor;
    }

    public void activateControlLine()
    {
        LineRenderer lineRend = sliderLine.GetComponent<LineRenderer>();
        lineRend.startColor = activeColor;
        lineRend.endColor = activeColor;
        Renderer rend = sliderPoint.GetComponent<Renderer>();
        rend.material.color = activeColor;
    }

    public void deactivateControlLine()
    {
        LineRenderer lineRend = sliderLine.GetComponent<LineRenderer>();
        lineRend.startColor = normalColor;
        lineRend.endColor = normalColor;
        Renderer rend = sliderPoint.GetComponent<Renderer>();
        rend.material.color = normalColor;
    }

    public void tryMoveSlider(Vector3 propPos)
    {
        LineRenderer lineRend = baseLine.GetComponent<LineRenderer>();
        Vector3[] positions = new Vector3[2];
        lineRend.GetPositions(positions);

        Vector3 leftPoint = positions[0];
        Vector3 rightPoint = positions[1];

        
        // project that point onto the world positions of the slider ends
        Vector3 v1 = rightPoint - leftPoint;
        Vector3 v2 = propPos - leftPoint;

        // 'd' is the vector-projection amount of v2 onto v1
        float d = Vector3.Dot(v1, v2) / Vector3.Dot(v1, v1);

        updateSliderPosition(d);
    }

    public void updateSliderPosition(float d)
    {
        // 'd' is also the correct linear combination of the left and right slider edges
        // left * d + right * ( 1 - d )
        currPosition = slider.transform.localPosition;
        currPosition.x = Mathf.Clamp(d, 0.0f, 1.0f);
        slider.transform.localPosition = currPosition;
    }

    public Vector3 getCurrentPosition()
    {
        return currPosition;
    }

    
    
}
