using System.Collections;
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
    public int firstSeenSrcPayloadBytes;
    public int firstSeenDestPayloadBytes;
    public int firstSeenSrcPort;
    public int firstSeenDstPort;
}

public enum actionAreaDisplayType
{
    ALL_TRAFFIC,
    SELECTED_TRAFFIC
}

public class ActionAreaManager : MonoBehaviour {

    public GameObject listObject;
    public GameObject extListObject;
    public GameObject listOpenObject;
    public GameObject labelTxtPrefab;
    public GameObject labelTxtExtNodePrefab;
    public GameObject linePrefab;
    public GameObject ptPrefab;

    public GameObject prevPageQuad;
    public GameObject nextPageQuad;
    public GameObject pageText;
    public GameObject prevPageExtQuad;
    public GameObject nextPageExtQuad;
    public GameObject extPageText;

    public GameObject dbConnectionObject;

    public GameObject subnetsInteriorObject;
    public GameObject subnetsExteriorObject;

    public bool subnetsExterior = true;
    public bool drawTimeConnections = false;

    public actionAreaDisplayType displayType;

    TestSQLiteConn dbConnClass;

    List<GameObject> nodeList = new List<GameObject>();
    Dictionary<string, NodeStatus> currentActiveNodes = new Dictionary<string, NodeStatus>();
    Dictionary<long, string> currActiveExteriorNodes = new Dictionary<long, string>();
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

    Dictionary<long, HashSet<long>> exteriorNodeRefs = new Dictionary<long, HashSet<long>>();

    int maxNodesPerPage = 20;
    int currNodePage = 1;
    int currExtNodePage = 1;

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
        extListObject.SetActive(true);
        listOpenObject.SetActive(false);
    }

    public void closeNodeList()
    {
        listObject.SetActive(false);
        extListObject.SetActive(false);
        listOpenObject.SetActive(true);
    }

    public void addActiveNode(NodeStatus ns)
    {
        NodeStatus tns;

        ns.selectNode();

        if ( !currentActiveNodes.TryGetValue(ns.currIpInfo.ipAddress, out tns))
        {
            currentActiveNodes.Add(ns.currIpInfo.ipAddress, ns);
        }

        if( displayType == actionAreaDisplayType.SELECTED_TRAFFIC )
        {
            long ipNum = TestSQLiteConn.getIpNumFromString(ns.currIpInfo.ipAddress);
            List < long > extNodes = dbConnClass.getExteriorNFIpsForNode(ns.currIpInfo.ipAddress);
            addExteriorNodeReferences(ipNum, extNodes);

            updateList();

            updateActionAreaForSelectedTraffic();
        }
        else
        {
            updateList();
        }
    }

    public bool nodeIsActive(NodeStatus ns)
    {
        return currentActiveNodes.ContainsKey(ns.currIpInfo.ipAddress);
    }

    void addExteriorNodeReferences(long activeIpNum, List<long> extIpNums)
    {
        HashSet<long> currSet;
        foreach (long extIp in extIpNums)
        {
            if (exteriorNodeRefs.TryGetValue(extIp, out currSet))
            {
                if (!currSet.Contains(activeIpNum)) currSet.Add(activeIpNum);
            }
            else
            {
                currSet = new HashSet<long>();
                currSet.Add(activeIpNum);
                exteriorNodeRefs.Add(extIp, currSet);
            }
        }
    }

    void removeExteriorNodeReferences(long activeIpNum)
    {
        HashSet<long> currSet;
        List<long> ipsToDelete = new List<long>();

        foreach(KeyValuePair<long, HashSet<long>> kv in exteriorNodeRefs)
        {
            currSet = kv.Value;
            if (currSet.Contains(activeIpNum))
            {
                currSet.Remove(activeIpNum);
                if (currSet.Count == 0) ipsToDelete.Add(kv.Key);
            }
        }

        string tStr;

        foreach (long extIp in ipsToDelete)
        {
            exteriorNodeRefs.Remove(extIp);
            if( currActiveExteriorNodes.TryGetValue(extIp, out tStr) )
            {
                currActiveExteriorNodes.Remove(extIp);
            }
        }
    }

    public void removeActiveNode(NodeStatus ns)
    {
        NodeStatus tns;

        if (currentActiveNodes.TryGetValue(ns.currIpInfo.ipAddress, out tns))
        {
            currentActiveNodes.Remove(ns.currIpInfo.ipAddress);

            if (displayType == actionAreaDisplayType.SELECTED_TRAFFIC)
            {
                removeExteriorNodeReferences(TestSQLiteConn.getIpNumFromString(ns.currIpInfo.ipAddress));
            }
        }

        updateList();

        if (displayType == actionAreaDisplayType.SELECTED_TRAFFIC)
        { 
            updateActionAreaForSelectedTraffic();
        }
        else if (displayType == actionAreaDisplayType.ALL_TRAFFIC)
        {
            if (activeNodeIpAddress.Equals(ns.currIpInfo.ipAddress))
            {
                activeNodeIpAddress = "";
                updateActiveNodeForAllTraffic();
            }
        }
    }

    public void activateSelectedNode(NodeStatus ns)
    {
        if (ns != null && !activeNodeIpAddress.Equals(ns.currIpInfo.ipAddress))
        {
            activeNodeIpAddress = ns.currIpInfo.ipAddress;
            updateList();

            if (displayType == actionAreaDisplayType.ALL_TRAFFIC)
            {
                updateActiveNodeForAllTraffic();
            }
        }
    }

    public void activateSelectedExteriorNode(GameObject gObj)
    {
        TextMesh tMesh = gObj.GetComponent<TextMesh>();
        if (tMesh == null) return;

        string ipAddress = tMesh.text;
        long ipNum = TestSQLiteConn.getIpNumFromString(ipAddress);

        string tStr;

        if( currActiveExteriorNodes.TryGetValue(ipNum, out tStr) )
        {
            currActiveExteriorNodes.Remove(ipNum);
        }
        else
        {
            currActiveExteriorNodes.Add(ipNum, ipAddress);
        }

        updateList();

        if (displayType == actionAreaDisplayType.SELECTED_TRAFFIC)
        {
            updateActionAreaForSelectedTraffic();
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

        int numNodes = currentActiveNodes.Count;
        int numExtNodes = exteriorNodeRefs.Count;



        Debug.Log("Curr Page before logic: " + currNodePage);

        int maxPage = (numNodes + maxNodesPerPage - 1) / maxNodesPerPage;

        if (currNodePage < 1) currNodePage = 1;
        else if (currNodePage > maxPage) currNodePage = maxPage;

        Debug.Log("Curr Page after logic: " + currNodePage);

        // update the page label and arrows
        TextMesh tMesh = pageText.GetComponent<TextMesh>();
        if(numNodes < 1) tMesh.text = "empty";
        else tMesh.text = "Page " + currNodePage + " of " + maxPage;

        MeshRenderer prevMeshRend = prevPageQuad.GetComponent<MeshRenderer>();
        MeshRenderer nextMeshRend = nextPageQuad.GetComponent<MeshRenderer>();

        if (currNodePage == 1) prevMeshRend.material.color = Color.gray;
        else prevMeshRend.material.color = Color.white;

        if (currNodePage == maxPage) nextMeshRend.material.color = Color.gray;
        else nextMeshRend.material.color = Color.white;







        int firstIdx = (currNodePage-1)* maxNodesPerPage + 1;
        int lastIdx = firstIdx + maxNodesPerPage;
        int currIdx = 0;
        
        foreach(KeyValuePair<string, NodeStatus> entry in currentActiveNodes )
        {
            currIdx++;
            if (currIdx < firstIdx) continue;
            else if (currIdx >= lastIdx) break;

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




        offset = new Vector3(0.0f, 0.0f, -0.01f);

        maxPage = (numExtNodes + maxNodesPerPage - 1) / maxNodesPerPage;

        if (currExtNodePage < 1) currExtNodePage = 1;
        else if (currExtNodePage > maxPage) currExtNodePage = maxPage;

        // update the external page label and arrows
        tMesh = extPageText.GetComponent<TextMesh>();
        if (numNodes < 1) tMesh.text = "empty";
        else tMesh.text = "Page " + currExtNodePage + " of " + maxPage;

        prevMeshRend = prevPageExtQuad.GetComponent<MeshRenderer>();
        nextMeshRend = nextPageExtQuad.GetComponent<MeshRenderer>();

        if (currExtNodePage == 1) prevMeshRend.material.color = Color.gray;
        else prevMeshRend.material.color = Color.white;

        if (currExtNodePage == maxPage) nextMeshRend.material.color = Color.gray;
        else nextMeshRend.material.color = Color.white;

        firstIdx = (currExtNodePage - 1) * maxNodesPerPage + 1;
        lastIdx = firstIdx + maxNodesPerPage;
        currIdx = 0;

        foreach (long key in exteriorNodeRefs.Keys)
        {
            currIdx++;
            if (currIdx < firstIdx) continue;
            else if (currIdx >= lastIdx) break;

            GameObject txtObj = (GameObject)Instantiate(labelTxtExtNodePrefab);
            ListLabelManager man = txtObj.GetComponent<ListLabelManager>();
            man.nodeStatus = null;

            TextMesh mesh = man.labelText.GetComponent<TextMesh>();
            mesh.text = TestSQLiteConn.getIpStringFromLong(key);

            if( currActiveExteriorNodes.ContainsKey(key) ) mesh.color = Color.yellow;

            txtObj.transform.SetParent(extListObject.transform);

            listLabelList.Add(txtObj);

            txtObj.transform.position = extListObject.transform.position + offset;
            txtObj.transform.rotation = extListObject.transform.rotation;

            offset += yOffsetInc;

        }




    }

    public void updateActiveNodeForAllTraffic()
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
                        Debug.Log(details.srcIp + " connected with " + details.dstIp);
                        break;
                    }
                }

                nodeDetailsLists[tgtIdx].Add(details);
            }
        }



        double minTime = float.MaxValue;
        double maxTime = float.MinValue;

        ipTimelineDetails tmpDetails;
        bool isSrc;

        Dictionary<long, double> maxTimePerIp = new Dictionary<long, double>();
        double tTime;

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



                if (maxTimePerIp.TryGetValue(details.srcIpNum, out tTime))
                {
                    if (details.time > tTime)
                    {
                        maxTimePerIp[details.srcIpNum] = details.time;
                    }
                }
                else maxTimePerIp.Add(details.srcIpNum, details.time);

                if (maxTimePerIp.TryGetValue(details.dstIpNum, out tTime))
                {
                    if (details.time > tTime)
                    {
                        maxTimePerIp[details.dstIpNum] = details.time;
                    }
                }
                else maxTimePerIp.Add(details.dstIpNum, details.time);



            }

        }

        double timeDiff = maxTime - minTime;
        float minHeight = 0.1f;
        float maxHeight = 1.0f;
        float heightDiff = maxHeight - minHeight;
        double heighTimeRatio = heightDiff / timeDiff;
        Vector3 pos;

        Vector3[] tmpPts = new Vector3[2];
        Vector3 offset = Vector3.zero;

        timePosition[] tpArray = new timePosition[numTotalPoints];
        int tpIdx = 0;

        for (int i = 0; i < 4; i++)
        {
            foreach (ipTimelineDetails det in nodeDetailsLists[i])
            {
                tgtIpNum = det.isSrc ? det.srcIpNum : det.dstIpNum;
                offset.y = (float)((det.time - minTime) * heighTimeRatio + minHeight);
                if (!subnetPointMaps.TryGetValue(tgtIpNum, out pos))
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

                tpArray[tpIdx] = new timePosition(det.time, gameObject.transform.TransformPoint(pos + offset));
                tpArray[tpIdx].srcPort = det.firstSeenSrcPort;
                tpArray[tpIdx].dstPort = det.firstSeenDstPort;
                tpIdx++;

                GameObject ptObj = (GameObject)Instantiate(ptPrefab);
                ptObj.transform.SetParent(gameObject.transform);
                ptObj.transform.localPosition = pos + offset;
 
                acticeNodeObjects.Add(ptObj);
            }
        }



        foreach (KeyValuePair<long, double> kv in maxTimePerIp)
        {
            offset.y = (float)((kv.Value - minTime) * heighTimeRatio + minHeight);

            if (!subnetPointMaps.TryGetValue(kv.Key, out pos))
            {
                Debug.LogError("Cannot find the value for " + kv.Key);
                continue;

            }

            GameObject lineObj = (GameObject)Instantiate(linePrefab);
            lineObj.transform.SetParent(gameObject.transform);

            tmpPts[0] = gameObject.transform.TransformPoint(pos);
            tmpPts[1] = gameObject.transform.TransformPoint(pos + offset);

            LineRenderer lineRend = lineObj.GetComponent<LineRenderer>();
            lineRend.useWorldSpace = false;
            lineRend.SetPositions(tmpPts);
            lineRend.startWidth = 0.1f;

            acticeNodeObjects.Add(lineObj);
        }

        if(drawTimeConnections)
        {
            Array.Sort<timePosition>(tpArray);
            Vector3[] tmpLinePts = new Vector3[tpArray.Length];

            for (int i = 0; i < tpArray.Length; i++)
            {
                tmpLinePts[i] = tpArray[i].srcPosition;
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

    public void updateActionAreaForSelectedTraffic()
    {
        foreach (GameObject gObj in acticeNodeObjects)
        {
            GameObject.Destroy(gObj);
        }

        acticeNodeObjects.Clear();

        for (int i = 0; i < 4; i++)
        {
            nodeDetailsLists[i].Clear();
        }

        int totalCount = currentActiveNodes.Count + currActiveExteriorNodes.Count;

        if (totalCount < 2) return;

        string[] uniqueIpAddresses = new string[totalCount];

        int idx = 0;

        foreach( KeyValuePair<string, NodeStatus> kv in currentActiveNodes )
        {
            uniqueIpAddresses[idx++] = kv.Key;
        }

        foreach (KeyValuePair<long, string> kv in currActiveExteriorNodes)
        {
            uniqueIpAddresses[idx++] = kv.Value;
        }

        List<ipTimelineDetails> detTraffList = new List<ipTimelineDetails>();

        for ( int i = 0; i < uniqueIpAddresses.Length; i++ )
        {
            for (int j = i+1; j < uniqueIpAddresses.Length; j++)
            {
                List<ipTimelineDetails> tmpList = dbConnClass.getDetailedNFTrafficBetweenNodes(uniqueIpAddresses[i], uniqueIpAddresses[j]);
                detTraffList.AddRange(tmpList);
            }
        }


        // iterate and get 

        Dictionary<long, double> maxTimePerIp = new Dictionary<long, double>();

        double minTime = float.MaxValue;
        double maxTime = float.MinValue;

        double tTime;

        foreach (ipTimelineDetails details in detTraffList)
        {
            if (details.time < minTime) minTime = details.time;
            if (details.time > maxTime) maxTime = details.time;

            nodeDetailsLists[0].Add(details);

            if (maxTimePerIp.TryGetValue(details.srcIpNum, out tTime))
            {
                if (details.time > tTime)
                {
                    maxTimePerIp[details.srcIpNum] = details.time;
                }
            }
            else maxTimePerIp.Add(details.srcIpNum, details.time);

            if (maxTimePerIp.TryGetValue(details.dstIpNum, out tTime))
            {
                if (details.time > tTime)
                {
                    maxTimePerIp[details.dstIpNum] = details.time;
                }
            }
            else maxTimePerIp.Add(details.dstIpNum, details.time);
        }


        double timeDiff = maxTime - minTime;
        float minHeight = 0.1f;
        float maxHeight = 1.0f;
        float heightDiff = maxHeight - minHeight;
        double heighTimeRatio = 0.0;
        if (Math.Abs(timeDiff) < 0.000001)
        {
            heighTimeRatio = 1.0;
            minHeight = 0.5f;
        }
        else
        {
            heighTimeRatio = heightDiff / timeDiff;
        }

        long[] tmpIpNums = new long[2];
        Vector3 offset = Vector3.zero;
        Vector3 pos;

        List<timePosition> timePosList = new List<timePosition>();

        foreach (ipTimelineDetails det in nodeDetailsLists[0])
        {
            tmpIpNums[0] = det.srcIpNum;
            tmpIpNums[1] = det.dstIpNum;
            offset.y = (float)((det.time - minTime) * heighTimeRatio + minHeight);

            timePosition tmpTp = new timePosition();
            tmpTp.time = det.time;
            tmpTp.srcPort = det.firstSeenSrcPort;
            tmpTp.dstPort = det.firstSeenDstPort;

            for (int i = 0; i < 2; i++)
            {
                if (!subnetPointMaps.TryGetValue(tmpIpNums[i], out pos))
                {
                    if (numOuterPointsUnassigned < 1)
                    {
                        Debug.LogError("Reached maximum number of internet node points in active area");
                        continue;
                    }
                    int tmpIdx = (int)(UnityEngine.Random.value * (numOuterPointsUnassigned - 1));
                    pos = outerPlotPoints[tmpIdx];

                    subnetPointMaps.Add(tmpIpNums[i], outerPlotPoints[tmpIdx]);
                    outerPlotPoints[tmpIdx] = outerPlotPoints[numOuterPointsUnassigned - 1];
                    numOuterPointsUnassigned--;
                }

                GameObject ptObj = (GameObject)Instantiate(ptPrefab);
                ptObj.transform.SetParent(gameObject.transform);
                ptObj.transform.localPosition = pos;
                ptObj.transform.localPosition += offset;

                if (i == 0) tmpTp.srcPosition = pos + offset;
                else tmpTp.dstPosition = pos + offset;

                acticeNodeObjects.Add(ptObj);

                timePosList.Add(tmpTp);
            }

        }


        Vector3[] tmpPts = new Vector3[2];

        foreach (KeyValuePair<long, double> kv in maxTimePerIp)
        {
            offset.y = (float)((kv.Value - minTime) * heighTimeRatio + minHeight);

            if (!subnetPointMaps.TryGetValue(kv.Key, out pos))
            {
                Debug.LogError("Cannot find the value for " + kv.Key);
                continue;

            }

            GameObject lineObj = (GameObject)Instantiate(linePrefab);
            lineObj.transform.SetParent(gameObject.transform);

            tmpPts[0] = gameObject.transform.TransformPoint(pos);
            tmpPts[1] = gameObject.transform.TransformPoint(pos + offset);

            LineRenderer lineRend = lineObj.GetComponent<LineRenderer>();
            lineRend.useWorldSpace = false;
            lineRend.SetPositions(tmpPts);
            lineRend.startWidth = 0.008f;
            lineRend.endWidth = 0.008f;

            acticeNodeObjects.Add(lineObj);
        }


        if (drawTimeConnections)
        {
            Vector3[] tmpLinePts = new Vector3[2];

            foreach(timePosition tp in timePosList)
            {
                tmpLinePts[0] = gameObject.transform.TransformPoint(tp.srcPosition);
                tmpLinePts[1] = gameObject.transform.TransformPoint(tp.dstPosition);

                GameObject lineObj = (GameObject)Instantiate(linePrefab);
                lineObj.transform.SetParent(gameObject.transform);

                LineRenderer lineRend = lineObj.GetComponent<LineRenderer>();
                lineRend.useWorldSpace = false;
                lineRend.SetPositions(tmpLinePts);
                lineRend.startColor = getPortColor(tp.srcPort);
                lineRend.endColor = getPortColor(tp.dstPort);

                acticeNodeObjects.Add(lineObj);
            }

            
        }
        
    }

    Color getPortColor(int port)
    {
        Color result = Color.white;
        switch (port)
        {
            case 21:
                result = Color.red;
                break;
            case 23:
                result = Color.green;
                break;
            case 80:
                result = Color.yellow;
                break;
        }

        return result;
    }

    public void nextNodePage()
    {
        currNodePage++;
        updateList();
    }

    public void prevNodePage()
    {
        currNodePage--;
        updateList();
    }

    public void nextExtNodePage()
    {
        currExtNodePage++;
        updateList();
    }

    public void prevExtNodePage()
    {
        currExtNodePage--;
        updateList();
    }
}

class timePosition : IComparable<timePosition>
{
    public double time;
    public Vector3 srcPosition;
    public Vector3 dstPosition;
    public int srcPort;
    public int dstPort;

    public timePosition(){}

    public timePosition(double t, Vector3 p)
    {
        time = t;
        srcPosition = p;
    }

    public timePosition(double t, Vector3 srcP, Vector3 dstP)
    {
        time = t;
        srcPosition = srcP;
        dstPosition = dstP;
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
