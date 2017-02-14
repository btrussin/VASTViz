using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RandomWalk2D
{

    List<Vector2> graphPts = new List<Vector2>();

    public RandomWalk2D() { }

    public RandomWalk2D(List<Vector2> pts)
    {
        graphPts.AddRange(pts);
    }

    public void clear()
    {
        graphPts.Clear();
    }

    public Vector2 getPoint(int idx)
    {
        if (idx < 0 || idx >= graphPts.Count) return Vector2.zero;
        else return graphPts[idx];
    }

    public List<Vector2> getPoints()
    {
        return graphPts;
    }

    public void addPoint(Vector2 pt)
    {
        graphPts.Add(pt);
    }

    public bool setPoint(int idx, Vector2 p)
    {
        if (idx < 0 || idx >= graphPts.Count) return false;

        if (idx == graphPts.Count) graphPts.Add(p);
        else graphPts[idx] = p;

        return true;
    }

    public void scale()
    {
        scale(Vector2.zero, Vector2.one);
    }

    public void scale(Vector2 rangeMin, Vector2 rangeMax)
    {
        //double [] range = range();

        Vector2 tmpPt = graphPts[0];

        Vector2 min = tmpPt;
        Vector2 max = tmpPt;

        foreach (Vector2 curr in graphPts)
        {
            if (curr.x < min.x) min.x = curr.x;
            else if (curr.x > max.x) max.x = curr.x;

            if (curr.y < min.y) min.y = curr.y;
            else if (curr.y > max.y) max.y = curr.y;

        }

        Vector2 domainInv = max - min;
        domainInv.x = 1.0f / domainInv.x;
        domainInv.y = 1.0f / domainInv.y;

        Vector2 rangeScale = rangeMax - rangeMin;



        Vector2 p;
        for (int i = 0; i < graphPts.Count; i++)
        {
            p = graphPts[i];

            p.x = ((p.x - min.x) * domainInv.x) * rangeScale.x + rangeMin.x;
            p.y = ((p.y - min.y) * domainInv.y) * rangeScale.y + rangeMin.y;

            graphPts[i] = p;
        }


    }

    public int size()
    {
        return graphPts.Count;
    }

    public void addOffset(float x, float y)
    {
        addOffset(new Vector2(x, y));
    }

    public void addOffset(Vector2 p)
    {
        Vector2 curr;
        for (int i = 0; i < graphPts.Count; i++)
        {
            curr = graphPts[i];
            curr += p;
            graphPts[i] = curr;
        }

    }

    public RandomWalk2D partition()
    {
        return new RandomWalk2D(graphPts);
    }


    public bool isAcuteXAngle()
    {
        Vector2 last1 = getPoint(size() - 1);
        Vector2 last2 = getPoint(size() - 2);
        Vector2 last3 = getPoint(size() - 3);

        float diffX = last1.x - last2.x;
        float prevDiffX = last2.x - last3.x;

        // if the difference between the x values of the points have opposite signs, increase the acute angle count
        return (diffX < 0.0 && prevDiffX > 0.0) || (diffX > 0.0 && prevDiffX < 0.0);
    }

    public bool isAcuteYAngle()
    {
        Vector2 last1 = getPoint(size() - 1);
        Vector2 last2 = getPoint(size() - 2);
        Vector2 last3 = getPoint(size() - 3);

        float diffY = last1.y - last2.y;
        float prevDiffY = last2.y - last3.y;

        // if the difference between the x values of the points have opposite signs, increase the acute angle count
        return (diffY < 0.0 && prevDiffY > 0.0) || (diffY > 0.0 && prevDiffY < 0.0);
    }

    private static float getRand()
    {
        float result = Random.value;
        if (result == 0.0f) return 0.0001f;
        else return result * 0.5f;
    }





    public static Vector2 getRandomPoint()
    {
        // Get a random number b/t aero and 2*PI
        float theta = Random.value * 2.0f * Mathf.PI;

        // add the sqrt(-ln(Random))*cos(theta) to the existing point
        float x = Mathf.Sqrt(-1.0f * Mathf.Log(getRand())) * Mathf.Cos(theta);
        float y = Mathf.Sqrt(-1.0f * Mathf.Log(getRand())) * Mathf.Sin(theta);

        return new Vector2(x, y);
    }



    public static RandomWalk2D createWalk(int numPts, int maxAcutePercent, int maxClusterSize, float percAreNodes = 1.0f)
    {
        int redraws = 0;
        RandomWalk2D line = new RandomWalk2D();

        if (maxAcutePercent < 0) maxAcutePercent = 100;
        if (maxClusterSize < 0) maxClusterSize = numPts;


        Vector2 last = Vector2.zero;
        Vector2 randPt;

        int maxAcute = (numPts * maxAcutePercent) / 100;

        int numAcuteX, numAcuteY;
        int consecAcuteX, consecAcuteY;


        bool foundCluster = true;
        bool isAcute = true;

        //Random rand;
        //long seed = 45;

        while (foundCluster || isAcute)
        {
            last = Vector2.zero;

            foundCluster = false;
            isAcute = false;

            numAcuteX = 0;
            numAcuteY = 0;
            consecAcuteX = 0;
            consecAcuteY = 0;

            line.clear();

            Vector2 tmpPt;

            // Loop through the algorithm the specified number of times.
            for (int i = 0; i < numPts; i++)
            {
                //randPt = Point.getRandomPoint(rand);
                randPt = getRandomPoint();

                // add random Point
                last += randPt;

                if (Random.value > percAreNodes) continue;

                tmpPt = new Vector2(last.x, last.y);

                // add the new point to the list
                line.addPoint(tmpPt);

                if (line.size() > 2)
                {
                    if (line.isAcuteXAngle()) { numAcuteX++; consecAcuteX++; }
                    else consecAcuteX = 0;
                    if (line.isAcuteYAngle()) { numAcuteY++; consecAcuteY++; }
                    else consecAcuteY = 0;
                }

                if (consecAcuteX > maxClusterSize || consecAcuteY > maxClusterSize)
                {
                    redraws++;
                    i += numPts;
                    foundCluster = true;
                    break;

                }

            }

            if (numAcuteX > maxAcute || numAcuteY > maxAcute)
            {
                if (!foundCluster) redraws++;
                isAcute = true;
            }
        }

        Debug.Log("Num Redraws: " + redraws);

        line.scale();
        return line;
    }



}


