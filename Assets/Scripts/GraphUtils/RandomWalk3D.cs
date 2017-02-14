using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RandomWalk3D
{

    List<Vector3> graphPts = new List<Vector3>();

    public RandomWalk3D() { }

    public RandomWalk3D(List<Vector3> pts)
    {
        graphPts.AddRange(pts);
    }

    public void clear()
    {
        graphPts.Clear();
    }

    public Vector3 getPoint(int idx)
    {
        if (idx < 0 || idx >= graphPts.Count) return Vector3.zero;
        else return graphPts[idx];
    }

    public List<Vector3> getPoints()
    {
        return graphPts;
    }

    public void addPoint(Vector3 pt)
    {
        graphPts.Add(pt);
    }

    public bool setPoint(int idx, Vector3 p)
    {
        if (idx < 0 || idx >= graphPts.Count) return false;

        if (idx == graphPts.Count) graphPts.Add(p);
        else
        {
            graphPts[idx] = p;
        }

        return true;
    }

    public void scale()
    {
        scale(Vector3.zero, Vector3.one);
    }

    public void scale(Vector3 rangeMin, Vector3 rangeMax)
    {
        //double [] range = range();

        Vector3 tmpPt = graphPts[0];

        Vector3 min = tmpPt;
        Vector3 max = tmpPt;

        foreach (Vector3 curr in graphPts)
        {
            if (curr.x < min.x) min.x = curr.x;
            else if (curr.x > max.x) max.x = curr.x;

            if (curr.y < min.y) min.y = curr.y;
            else if (curr.y > max.y) max.y = curr.y;

            if (curr.z < min.z) min.z = curr.z;
            else if (curr.z > max.z) max.z = curr.z;

        }

        Vector3 domainInv = max - min;
        domainInv.x = 1.0f / domainInv.x;
        domainInv.y = 1.0f / domainInv.y;
        domainInv.z = 1.0f / domainInv.z;

        Vector3 rangeScale = rangeMax - rangeMin;



        Vector3 p;
        for (int i = 0; i < graphPts.Count; i++)
        {
            p = graphPts[i];

            p.x = ((p.x - min.x) * domainInv.x) * rangeScale.x + rangeMin.x;
            p.y = ((p.y - min.y) * domainInv.y) * rangeScale.y + rangeMin.y;
            p.z = ((p.z - min.z) * domainInv.z) * rangeScale.z + rangeMin.z;

            graphPts[i] = p;
        }


    }

    public int size()
    {
        return graphPts.Count;
    }

    public void addOffset(float x, float y, float z)
    {
        addOffset(new Vector3(x, y, z));
    }

    public void addOffset(Vector3 p)
    {
        Vector3 curr;
        for (int i = 0; i < graphPts.Count; i++)
        {
            curr = graphPts[i];
            curr += p;
            graphPts[i] = curr;
        }

    }

    public RandomWalk3D partition()
    {
        return new RandomWalk3D(graphPts);
    }


    public bool isAcuteXAngle()
    {
        Vector3 last1 = getPoint(size() - 1);
        Vector3 last2 = getPoint(size() - 2);
        Vector3 last3 = getPoint(size() - 3);

        float diffX = last1.x - last2.x;
        float prevDiffX = last2.x - last3.x;

        // if the difference between the x values of the points have opposite signs, increase the acute angle count
        return (diffX < 0.0 && prevDiffX > 0.0) || (diffX > 0.0 && prevDiffX < 0.0);
    }

    public bool isAcuteYAngle()
    {
        Vector3 last1 = getPoint(size() - 1);
        Vector3 last2 = getPoint(size() - 2);
        Vector3 last3 = getPoint(size() - 3);

        float diffY = last1.y - last2.y;
        float prevDiffY = last2.y - last3.y;

        // if the difference between the x values of the points have opposite signs, increase the acute angle count
        return (diffY < 0.0 && prevDiffY > 0.0) || (diffY > 0.0 && prevDiffY < 0.0);
    }

    public bool isAcuteZAngle()
    {
        Vector3 last1 = getPoint(size() - 1);
        Vector3 last2 = getPoint(size() - 2);
        Vector3 last3 = getPoint(size() - 3);

        float diffZ = last1.z - last2.z;
        float prevDiffZ = last2.z - last3.z;

        // if the difference between the x values of the points have opposite signs, increase the acute angle count
        return (diffZ < 0.0 && prevDiffZ > 0.0) || (diffZ > 0.0 && prevDiffZ < 0.0);
    }


    private static float getRand()
    {
        float result = Random.value;
        if (result == 0.0f) return 0.0001f;
        else return result * 0.5f;
    }





    public static Vector3 getRandomPoint()
    {
        // Get a random number b/t aero and 2*PI
        float theta = Random.value * 2.0f * Mathf.PI;
        float theta2 = Random.value * 2.0f * Mathf.PI;

        // add the sqrt(-ln(Random))*cos(theta) to the existing point
        float x = Mathf.Sqrt(-1.0f * Mathf.Log(getRand())) * Mathf.Cos(theta);
        float y = Mathf.Sqrt(-1.0f * Mathf.Log(getRand())) * Mathf.Sin(theta);
        float z = Mathf.Sqrt(-1.0f * Mathf.Log(getRand())) * Mathf.Cos(theta2);

        return new Vector3(x, y, z);
    }



    public static RandomWalk3D createWalk(int numPts, int maxAcutePercent, int maxClusterSize, float percAreNodes = 1.0f)
    {
        int redraws = 0;
        RandomWalk3D line = new RandomWalk3D();

        if (maxAcutePercent < 0) maxAcutePercent = 100;
        if (maxClusterSize < 0) maxClusterSize = numPts;


        Vector3 last = Vector3.zero;
        Vector3 randPt;

        int maxAcute = (numPts * maxAcutePercent) / 100;

        int numAcuteX, numAcuteY, numAcuteZ;
        int consecAcuteX, consecAcuteY, consecAcuteZ;


        bool foundCluster = true;
        bool isAcute = true;

        //Random rand;
        //long seed = 45;

        while (foundCluster || isAcute)
        {
            last = Vector3.zero;

            foundCluster = false;
            isAcute = false;

            numAcuteX = 0;
            numAcuteY = 0;
            numAcuteZ = 0;
            consecAcuteX = 0;
            consecAcuteY = 0;
            consecAcuteZ = 0;


            line.clear();

            Vector3 tmpPt;

            // Loop through the algorithm the specified number of times.
            for (int i = 0; i < numPts; i++)
            {
                //randPt = Point.getRandomPoint(rand);
                randPt = getRandomPoint();

                // add random Point
                last += randPt;

                if (Random.value > percAreNodes) continue;

                tmpPt = new Vector3(last.x, last.y, last.z);

                // add the new point to the list
                line.addPoint(tmpPt);

                if (line.size() > 2)
                {
                    if (line.isAcuteXAngle()) { numAcuteX++; consecAcuteX++; }
                    else consecAcuteX = 0;
                    if (line.isAcuteYAngle()) { numAcuteY++; consecAcuteY++; }
                    else consecAcuteY = 0;
                    if (line.isAcuteZAngle()) { numAcuteZ++; consecAcuteZ++; }
                    else consecAcuteZ = 0;
                }

                if (consecAcuteX > maxClusterSize || consecAcuteY > maxClusterSize || consecAcuteZ > maxClusterSize)
                {
                    redraws++;
                    i += numPts;
                    foundCluster = true;
                    break;

                }

            }

            if (numAcuteX > maxAcute || numAcuteY > maxAcute || numAcuteZ > maxAcute)
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


