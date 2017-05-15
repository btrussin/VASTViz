﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public struct ipTimelineDetails
{
    public double time;
    public string srcIp;
    public string dstIp;
    public long srcIpNum;
    public long dstIpNum;
    public bool isSrc;
}

public class ActionAreaManager : MonoBehaviour {

    public GameObject listObject;
    public GameObject listOpenObject;
    public GameObject labelTxtPrefab;
    public GameObject linePrefab;
    public GameObject ptPrefab;

    public GameObject dbConnectionObject;

    public GameObject subnetsInteriorObject;
    public GameObject subnetsExteriorObject;

    public bool subnetsExterior = true;
    public bool drawTimeConnections = false;

    TestSQLiteConn dbConnClass;

    List<GameObject> nodeList = new List<GameObject>();
    Dictionary<string, NodeStatus> currentActiveNodes = new Dictionary<string, NodeStatus>();
    List<GameObject> listLabelList = new List<GameObject>();

    string activeNodeIpAddress = "";

    long[] minSubnetIpNum = new long[3];
    long[] maxSubnetIpNum = new long[3];

    List<ipTimelineDetails>[] nodeDetailsLists = new List<ipTimelineDetails>[4];
    Vector3[][] plotPoints = new Vector3[3][];
    Vector3[] outerPlotPoints = new Vector3[400];
    int numOuterPointsUnassigned = 400;

    Dictionary<long, Vector3> subnetPointMaps = new Dictionary<long, Vector3>();


    List<GameObject> acticeNodeObjects = new List<GameObject>();


    // Use this for initialization
    void Start () {

        if( subnetsExterior )
        {
            subnetsExteriorObject.SetActive(true);
            subnetsInteriorObject.SetActive(false);
        }
        else
        {
            subnetsExteriorObject.SetActive(false);
            subnetsInteriorObject.SetActive(true);
        }

        dbConnClass = dbConnectionObject.GetComponent<TestSQLiteConn>();

        for (int i = 0; i < 3; i++)
        {
            minSubnetIpNum[i] = TestSQLiteConn.getIpNumFromString("172." + (i + 1) + "0.0.0");
            maxSubnetIpNum[i] = TestSQLiteConn.getIpNumFromString("172." + (i + 1) + "0.255.255");
        }

        for (int i = 0; i < 4; i++)
        {
            nodeDetailsLists[i] = new List<ipTimelineDetails>();
        }

        setupNodeMap();

        updateList();
	}

    void setupNodeMap()
    {
        int idx = 0;
        float maxRadius = 0.5f;
        float radiusInc = maxRadius * 0.2f;
        float radiusVariation;

        float extMinRad = 0.62f;
        float extMaxRad = 0.84f;
        float extRadDiff = extMaxRad - extMinRad;

        Vector3[] subAreaRotations = new Vector3[3];
        subAreaRotations[0] = new Vector3(0.0f, 60.0f, 0.0f);
        subAreaRotations[1] = new Vector3(0.0f, 180.0f, 0.0f);
        subAreaRotations[2] = new Vector3(0.0f, -60.0f, 0.0f);

        float angleInc, currAngle, currRadius;

        for ( int i = 0; i < 3; i++ )
        {
            Quaternion rot = Quaternion.Euler(subAreaRotations[i]);
            idx = 0;
            plotPoints[i] = new Vector3[500];
            if(subnetsExterior )
            {
                angleInc = Mathf.Deg2Rad * (110.0f / 500.0f);
                currAngle = Mathf.Deg2Rad * (5.0f);
                for (int k = 0; k < 500; k++)
                {

                    radiusVariation = extMinRad + extRadDiff * UnityEngine.Random.value;
                    plotPoints[i][idx++] = rot * new Vector3(Mathf.Cos(currAngle) * radiusVariation, 0.0f, Mathf.Sin(currAngle) * radiusVariation);
                    currAngle += angleInc;
                }
            }
            else
            {
                for (int j = 0; j < 5; j++)
                {
                    int numDiv = (2 * j + 1) * 20;

                    angleInc = Mathf.Deg2Rad * (110.0f / numDiv);
                    currAngle = Mathf.Deg2Rad * (4.0f + 2.0f * UnityEngine.Random.value);
                    currRadius = radiusInc * j + radiusInc * 0.25f;
                    for (int k = 0; k < numDiv; k++)
                    {
                        radiusVariation = currRadius + radiusInc * 0.5f * UnityEngine.Random.value;
                        plotPoints[i][idx++] = rot * new Vector3(Mathf.Cos(currAngle) * radiusVariation, 0.0f, Mathf.Sin(currAngle) * radiusVariation);
                        currAngle += angleInc;
                    }

                }
            }
            
        }

        if( subnetsExterior )
        {
            angleInc = Mathf.Deg2Rad * (360.0f / outerPlotPoints.Length);
            currAngle = 0.0f;
            currRadius = extMinRad-0.4f;

            float radVariance = 0.4f;
            float tMaxRad = extMinRad-0.1f;

            for (int i = 0; i < outerPlotPoints.Length; i++)
            {
                currRadius = tMaxRad - radVariance * UnityEngine.Random.value;
                outerPlotPoints[i] = new Vector3(Mathf.Cos(currAngle) * currRadius, 0.0f, Mathf.Sin(currAngle) * currRadius);
                currAngle += angleInc;
            }
        }
        else
        {
            angleInc = Mathf.Deg2Rad * (360.0f / outerPlotPoints.Length);
            currAngle = 0.0f;
            currRadius = 0.62f;

            for (int i = 0; i < outerPlotPoints.Length; i++)
            {
                currRadius = extMinRad + extRadDiff * UnityEngine.Random.value;
                outerPlotPoints[i] = new Vector3(Mathf.Cos(currAngle) * currRadius, 0.0f, Mathf.Sin(currAngle) * currRadius);
                currAngle += angleInc;
            }
        }
       
        Dictionary<long, ipInfo>[] subnetMaps = dbConnClass.getSubnetMaps();

        for( int currSubNetIdx = 0; currSubNetIdx < subnetMaps.Length; currSubNetIdx++ )
        {
            int currMaxIdx = 499;
            int currIdx;
            foreach(long key in subnetMaps[currSubNetIdx].Keys)
            {
                currIdx = (int)(UnityEngine.Random.value * currMaxIdx);

                subnetPointMaps.Add(key, plotPoints[currSubNetIdx][currIdx]);
                plotPoints[currSubNetIdx][currIdx] = plotPoints[currSubNetIdx][currMaxIdx];
                currMaxIdx--;
            }

        }
    }
	
	// Update is called once per frame
	void Update () {
        
    }

    public void openNodeList()
    {
        listObject.SetActive(true);
        listOpenObject.SetActive(false);
    }

    public void closeNodeList()
    {
        listObject.SetActive(false);
        listOpenObject.SetActive(true);
    }

    public void addActiveNode(NodeStatus ns)
    {
        NodeStatus tns;

        if (currentActiveNodes.Count >= 20) return;

        if( !currentActiveNodes.TryGetValue(ns.currIpInfo.ipAddress, out tns))
        {
            currentActiveNodes.Add(ns.currIpInfo.ipAddress, ns);
        }

        updateList();
    }

    public void removeActiveNode(NodeStatus ns)
    {
        NodeStatus tns;

        if (currentActiveNodes.TryGetValue(ns.currIpInfo.ipAddress, out tns))
        {
            currentActiveNodes.Remove(ns.currIpInfo.ipAddress);
        }

        if(activeNodeIpAddress.Equals(ns.currIpInfo.ipAddress))
        {
            activeNodeIpAddress = "";
            updateActiveNode();
        }

        updateList();
    }

    public void activateSelectedNode(NodeStatus ns)
    {
        if (!activeNodeIpAddress.Equals(ns.currIpInfo.ipAddress))
        {
            activeNodeIpAddress = ns.currIpInfo.ipAddress;
            updateList();
            updateActiveNode();
        }
    }

    public void updateList()
    {
        foreach(GameObject go in listLabelList)
        {
            GameObject.Destroy(go);
        }

        listLabelList.Clear();

        Vector3 offset = new Vector3(0.0f, 0.0f, -0.01f);
        Vector3 yOffsetInc = listObject.transform.up * -0.05f;

        if ( currentActiveNodes.Count < 1 )
        {
            GameObject txtObj = (GameObject)Instantiate(labelTxtPrefab);
            ListLabelManager man = txtObj.GetComponent<ListLabelManager>();
            man.nodeStatus = null;

            txtObj.transform.SetParent(listObject.transform);

            txtObj.transform.position = listObject.transform.position;
            txtObj.transform.rotation = listObject.transform.rotation;

            listLabelList.Add(txtObj);

            return;
        }

        //float yOffsetInc = -0.05f;
        
        foreach(KeyValuePair<string, NodeStatus> entry in currentActiveNodes )
        {
            GameObject txtObj = (GameObject)Instantiate(labelTxtPrefab);
            ListLabelManager man = txtObj.GetComponent<ListLabelManager>();
            man.nodeStatus = entry.Value;

            TextMesh mesh = man.labelText.GetComponent<TextMesh>();
            mesh.text = entry.Key;

            if( activeNodeIpAddress.Equals(entry.Key) )
            {
                mesh.color = Color.yellow;
            }

            txtObj.transform.SetParent(listObject.transform);

            listLabelList.Add(txtObj);

            txtObj.transform.position = listObject.transform.position + offset;
            txtObj.transform.rotation = listObject.transform.rotation;

            offset += yOffsetInc;

        }
    }

    public void updateActiveNode()
    {
        //List<GameObject> acticeNodeObjects = new List<GameObject>();
        foreach(GameObject gObj in acticeNodeObjects)
        {
            GameObject.Destroy(gObj);
        }

        acticeNodeObjects.Clear();

        for (int i = 0; i < 4; i++)
        {
            nodeDetailsLists[i].Clear();
        }
        NodeStatus ns;
        long currIpNum = 0;
        long tgtIpNum;
        int tgtIdx;
        int numTotalPoints = 0;

        if (currentActiveNodes.TryGetValue(activeNodeIpAddress, out ns))
        {

            List<ipTimelineDetails> detTraffList = dbConnClass.getDetailedNFTrafficForNode(ns.currIpInfo.ipAddress);
            numTotalPoints = detTraffList.Count;
            currIpNum = TestSQLiteConn.getIpNumFromString(ns.currIpInfo.ipAddress);
            
            foreach (ipTimelineDetails details in detTraffList)
            {
                if (details.srcIpNum == currIpNum) tgtIpNum = details.dstIpNum;
                else tgtIpNum = details.srcIpNum;

                tgtIdx = 3;

                for ( int i = 0; i < 3; i++ )
                {
                    if (tgtIpNum >= minSubnetIpNum[i] && tgtIpNum <= maxSubnetIpNum[i])
                    {
                        tgtIdx = i;
                        break;
                    }
                }

                nodeDetailsLists[tgtIdx].Add(details);
            }

            Debug.Log("Sub1: " + nodeDetailsLists[0].Count +
                "\tSub2: " + nodeDetailsLists[1].Count +
                "\tSub3: " + nodeDetailsLists[2].Count +
                "\tInternet: " + nodeDetailsLists[3].Count);

        }



        double minTime = float.MaxValue;
        double maxTime = float.MinValue;

        ipTimelineDetails tmpDetails;
        bool isSrc;

        for (int i = 0; i < 4; i++)
        {
            Dictionary<long, ipTimelineDetails> uniqueNodes = new Dictionary<long, ipTimelineDetails>();

            foreach (ipTimelineDetails details in nodeDetailsLists[i])
            {
                if (details.time < minTime) minTime = details.time;
                if (details.time > maxTime) maxTime = details.time;

                if (details.srcIpNum == currIpNum) { tgtIpNum = details.dstIpNum; isSrc = true; }
                else { tgtIpNum = details.srcIpNum; isSrc = false; }

                if (!uniqueNodes.TryGetValue(tgtIpNum, out tmpDetails))
                {
                    uniqueNodes.Add(tgtIpNum, details);
                    uniqueNodes.TryGetValue(tgtIpNum, out tmpDetails);
                    tmpDetails.isSrc = isSrc;
                }

            }

        }

        double timeDiff = maxTime - minTime;
        float minHeight = 0.1f;
        float maxHeight = 1.0f;
        float heightDiff = maxHeight - minHeight;
        double heighTimeRation = heightDiff / timeDiff;
        Vector3 pos;

        Vector3[] tmpPts = new Vector3[2];
        Vector3 offset = Vector3.zero;

       
        timePosition[] tpArray = new timePosition[numTotalPoints];
        int tpIdx = 0;

        for (int i = 0; i < 4; i++)
        {
            foreach(ipTimelineDetails det in nodeDetailsLists[i])
            {
                tgtIpNum = det.isSrc ? det.srcIpNum : det.dstIpNum;
                offset.y = (float)((det.time - minTime) * heighTimeRation + minHeight);
                if (!subnetPointMaps.TryGetValue(tgtIpNum, out pos) )
                {

                    if (numOuterPointsUnassigned < 1)
                    {
                        Debug.LogError("Reached maximum number of internet node points in active area");
                        continue;
                    }
                    int tmpIdx = (int)(UnityEngine.Random.value * (numOuterPointsUnassigned - 1));
                    pos = outerPlotPoints[tmpIdx];

                    subnetPointMaps.Add(tgtIpNum, outerPlotPoints[tmpIdx]);
                    outerPlotPoints[tmpIdx] = outerPlotPoints[numOuterPointsUnassigned - 1];
                    numOuterPointsUnassigned--;
                }

                GameObject lineObj = (GameObject)Instantiate(linePrefab);
                lineObj.transform.SetParent(gameObject.transform);

                tmpPts[0] = gameObject.transform.TransformPoint(pos);
                tmpPts[1] = gameObject.transform.TransformPoint(pos + offset);

                tpArray[tpIdx++] = new timePosition(det.time, tmpPts[1]);

                LineRenderer lineRend = lineObj.GetComponent<LineRenderer>();
                lineRend.useWorldSpace = false;
                lineRend.SetPositions(tmpPts);

                GameObject ptObj = (GameObject)Instantiate(ptPrefab);
                ptObj.transform.SetParent(gameObject.transform);
                ptObj.transform.localPosition = pos + offset;

                acticeNodeObjects.Add(lineObj);
                acticeNodeObjects.Add(ptObj);
            }
        }

        if(drawTimeConnections)
        {
            Array.Sort<timePosition>(tpArray);
            Vector3[] tmpLinePts = new Vector3[tpArray.Length];

            for (int i = 0; i < tpArray.Length; i++)
            {
                tmpLinePts[i] = tpArray[i].position;
            }

            GameObject lineObj = (GameObject)Instantiate(linePrefab);
            lineObj.transform.SetParent(gameObject.transform);

            LineRenderer lineRend = lineObj.GetComponent<LineRenderer>();
            lineRend.useWorldSpace = false;
            lineRend.numPositions = tmpLinePts.Length;
            lineRend.SetPositions(tmpLinePts);
            lineRend.startColor = Color.white;
            lineRend.endColor = Color.black;

            acticeNodeObjects.Add(lineObj);
        }

    }

    public void updateActionArea()
    {
        
    }
}

class timePosition : IComparable<timePosition>
{
    public double time;
    public Vector3 position;

    public timePosition(double t, Vector3 p)
    {
        time = t;
        position = p;
    }

    public int CompareTo(timePosition other)
    {
        if (other == null)
        {
            return 1;
        }

        //Return the difference in power.
        return (other.time - time < 0.0 ? -1 : 1 );
    }
}
