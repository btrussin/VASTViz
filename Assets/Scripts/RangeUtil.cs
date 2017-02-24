using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RangeUtil {

    float minDomain = 0.0f;
    float maxDomain = 1.0f;
    float minRange = 0.0f;
    float maxRange = 1.0f;

    float conversionScale = 1.0f;


    public RangeUtil()
    {

    }

    public RangeUtil(float minD, float maxD, float minR, float maxR)
    {
        setDomain(minD, maxD);
        setRange(minR, maxR);
        setScale();
    }

    public void setDomain(float min, float max)
    {
        minDomain = min;
        maxDomain = max;
        setScale();
    }

    public void setRange(float min, float max)
    {
        minRange = min;
        maxRange = max;

        setScale();
    }

    private void setScale()
    {
        conversionScale = (maxRange - minRange) / (maxDomain - minDomain);
    }

    public float getVal(float d)
    {
        return (d - minDomain) * conversionScale + minRange;
    }
}
