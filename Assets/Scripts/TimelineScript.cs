using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public struct layerOffsetElement
{
    public GameObject gameObject;
    public int offsetLayer;
}

public class TimelineScript : MonoBehaviour {

    public Material lineMaterial;
    public GameObject slider;
    public GameObject sliderLine;
    public GameObject sliderPoint;

    public GameObject sliderEnd;
    public GameObject sliderEndLine;
    public GameObject sliderEndPoint;

    public GameObject baseLine;

    public GameObject[] anchoredLabels;

    float stepDistance;
    Vector3 currPosition;

    float baseScale = 1.0f;
    float currScale = 1.0f;

    float baseCtrlrDist = 0.0f;

    float currSliderPositionRelative = 0.0f;

    Color activeColor;
    Color hightlightColor;
    Color normalColor;

    bool activeScale = false;

    List<GameObject> scalableElements = new List<GameObject>();

    List<layerOffsetElement> layerElements = new List<layerOffsetElement>();

    float currLayerOffset = 0.02f;

    BoxCollider boxCollider;


    // Use this for initialization
    void Start() {
        activeColor = new Color32(255, 255, 0, 255);
        hightlightColor = new Color32(0, 255, 0, 255);
        normalColor = new Color32(0, 128, 0, 255);

        scalableElements.Add(baseLine);
        updateSliderPosition(currSliderPositionRelative);
        for(int i = 0; i < anchoredLabels.Length; i++)
        {
            layerOffsetElement elem;
            elem.offsetLayer = i;
            elem.gameObject = anchoredLabels[i];
            layerElements.Add(elem);
        }

        boxCollider = gameObject.GetComponent<BoxCollider>();
    }

    // Update is called once per frame
    void Update() {
        if (Input.GetKeyDown(KeyCode.M))
        {
            Vector3 p = slider.transform.localPosition;
            p.x += stepDistance;
            slider.transform.localPosition = p;

            p = slider.transform.position;
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            offsetElements(currLayerOffset + 0.01f);
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            offsetElements(currLayerOffset - 0.01f);
        }
    }

    public void createLines(int[] countVals, Color color, Vector3 graphOffset, int layer)
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

        Vector3 mainOffset = transform.position;

        for (int i = 0; i < countVals.Length; i++)
        { 
            tmpV = new Vector3(currX, tmpVals[i] * yScale, 0.0f);

            pts[i].x = Vector3.Dot(tmpV, right);
            pts[i].y = Vector3.Dot(tmpV, up);
            pts[i].z = -Vector3.Dot(tmpV, forward);

            currX += xInc;
        }

        GameObject connCurve = new GameObject();

        connCurve.transform.SetParent(gameObject.transform);
        connCurve.transform.localPosition = graphOffset;

        connCurve.AddComponent<LineRenderer>();
        LineRenderer rend = connCurve.GetComponent<LineRenderer>();
        rend.startWidth = 0.003f;
        rend.endWidth = 0.003f;
        rend.startColor = color;
        rend.endColor = color;
        rend.positionCount = countVals.Length;
        rend.useWorldSpace = false;

        rend.material = lineMaterial;

        rend.SetPositions(pts);

        scalableElements.Add(connCurve);

        layerOffsetElement elem;
        elem.offsetLayer = layer;
        elem.gameObject = connCurve;
        layerElements.Add(elem);

        offsetElements(currLayerOffset);
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


        Vector3 leftPoint = gameObject.transform.position + positions[0].x * gameObject.transform.right * currScale + positions[0].y * gameObject.transform.up + positions[0].z * gameObject.transform.forward;
        Vector3 rightPoint = gameObject.transform.position + positions[1].x * gameObject.transform.right * currScale + positions[1].y * gameObject.transform.up + positions[1].z * gameObject.transform.forward;

        // project that point onto the world positions of the slider ends
        Vector3 v1 = rightPoint - leftPoint;
        Vector3 v2 = propPos - leftPoint;

        // 'd' is the vector-projection amount of v2 onto v1
        float d = Mathf.Clamp(Vector3.Dot(v1, v2) / Vector3.Dot(v1, v1), 0.0f, 1.0f);

        currSliderPositionRelative = d;

        updateSliderPosition(currSliderPositionRelative);
    }

    // d is between 0 and 1
    public void updateSliderPosition(float d)
    {
        // 'd' is also the correct linear combination of the left and right slider edges
        // left * d + right * ( 1 - d )
        currPosition = slider.transform.localPosition;
        currPosition.x = currScale * (d - 0.5f);
        slider.transform.localPosition = currPosition;
    }

    public float getCurrentRelativePosition()
    {
        return currSliderPositionRelative;
    }

    public void startScale(Vector3 posA, Vector3 posB)
    {
        Vector3 vec = posA - posB;
        baseCtrlrDist = vec.magnitude;
        activeScale = true;
    }

    public void updateScale(Vector3 posA, Vector3 posB)
    {
        if (!activeScale) return;

        Vector3 vec = posA - posB;

        currScale = vec.magnitude / baseCtrlrDist * baseScale;

        foreach( GameObject gObj in scalableElements)
        {
            vec = gObj.transform.localScale;
            vec.x = currScale;
            gObj.transform.localScale = vec;
        }

        updateSliderPosition(currSliderPositionRelative);

        Vector3 v;
        foreach (GameObject label in anchoredLabels)
        {
            v = label.transform.localPosition;
            v.x = -currScale * 0.5f;
            label.transform.localPosition = v;
        }

        Vector3 boxSize = boxCollider.size;
        boxSize.x = currScale;
        boxCollider.size = boxSize;
    }

    public void endScale()
    {
        baseScale = currScale;
        activeScale = false;
    }

    public void offsetElements(float amount)
    {
        if (amount < 0.0f) return;

        currLayerOffset = amount;

        Vector3 vec;
        foreach(layerOffsetElement elem in layerElements)
        {
            vec = elem.gameObject.transform.localPosition;
            vec.z = amount * elem.offsetLayer;
            elem.gameObject.transform.localPosition = vec;
        }
    }

    public float getCurrentOffset()
    {
        return currLayerOffset;
    }

}
