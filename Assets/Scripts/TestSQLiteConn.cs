using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Mono.Data.Sqlite;
using System.Data;
using System;

public enum ipType
{
    WORKSTATION,
    SERVER
}

public struct ipInfo
{
    public int subnet;
    public string ipAddress;
    public ipType type;
}

public struct ipDataStruct
{
    public string ipAddress;
    public int numTimesSeen;
}

public struct bbDataStruct
{
    public string ipAddress;
    public int status;  // 0-UNK, 1-Good, 2-Warning, 3-Problem
}


public struct ipsDataStruct
{
    public string ipAddress;
    public int priority; 
}


public enum dbDataType
{
    NETWORK_FLOW,
    BIG_BROTHER,
    INTRUSION_PROTECTION
}

public class TestSQLiteConn : MonoBehaviour {

    public RealityCheck.RealitySessionState realitySessionState;

    Dictionary<long, ipInfo>[] subnetMaps = new Dictionary<long, ipInfo>[3];

    long[] minSubnetIpNum = new long[3];
    long[] maxSubnetIpNum = new long[3];

    long minUTCTime = 1364802600; // first minute: 1364802600:  04-01-2013 07:50:00
    long maxUTCTime = 1366045200; // last minute: 1366045200: 04-15-2013 17:00:00

    long[] timeSlices;
    int[] timesliceNFCounts;
    int[] timesliceBBCounts;
    int[] timesliceIPSCounts;

    int numMinutesPerSlice = 10;
    int numSecondsPerSlice;

    bool queryActive = false;

    Dictionary<long, ipDataStruct>[] currNfIpsSeen = new Dictionary<long, ipDataStruct>[3];
    Dictionary<long, ipDataStruct>[] nextNfIpsSeen = new Dictionary<long, ipDataStruct>[3];

    Dictionary<long, bbDataStruct>[] currBbIpsSeen = new Dictionary<long, bbDataStruct>[3];
    Dictionary<long, bbDataStruct>[] nextBbIpsSeen = new Dictionary<long, bbDataStruct>[3];

    Dictionary<long, ipsDataStruct>[] currIPSIpsSeen = new Dictionary<long, ipsDataStruct>[3];
    Dictionary<long, ipsDataStruct>[] nextIPSIpsSeen = new Dictionary<long, ipsDataStruct>[3];


    Dictionary<long, ipDataStruct> outOfNetIpsSeen = new Dictionary<long, ipDataStruct>();

    public double maxIterTime = 0.005;
    double iterTimeOffset;

    bool hasResultSets;


    SqliteConnection dbconn;
    IDbCommand dbcmd = null;
    IDataReader dataReader = null;

    TimeSpan queryStartTime;
    TimeSpan queryEndTime;

    public int currTimeIdx;

    public GameObject[] subnetObjects = new GameObject[3];
    SubnetMapping[] subnetMappings = new SubnetMapping[3];
    public GameObject timeline;

    dbDataType currDbDataType = dbDataType.NETWORK_FLOW;

    bool inAnimation = false;

    // Use this for initialization
    void Start()
    {
        
        iterTimeOffset = maxIterTime * 1000.0;

        for (int i = 0; i < 3; i++)
        {
            subnetMaps[i] = new Dictionary<long, ipInfo>();

            minSubnetIpNum[i] = getIpNumFromString("172." + (i + 1) + "0.0.0");
            maxSubnetIpNum[i] = getIpNumFromString("172." + (i + 1) + "0.255.255");

        }

        //string conn = "URI=file:C:\\Users\\btrus\\Documents\\VAST\\sqlite\\vast.db";
        string conn = "URI=file:C:\\Users\\Public\\Documents\\VAST\\sqlite\\vast.db";

        //conn = "URI=file:" + Application.dataPath + "/vast.db";

        Debug.Log("Connecting to: " + conn);

        dbconn = new SqliteConnection(conn);

        dbconn.Open(); //Open connection to the database.

        /* DELETE NEXT LINE */
        //temp();

       

        numSecondsPerSlice = numMinutesPerSlice * 60;

        setupAllSubnets();

        for (int i = 0; i < subnetObjects.Length; i++)
        {
            subnetMappings[i] = subnetObjects[i].GetComponent<SubnetMapping>();

            if (subnetMappings[i] != null)
            {
                subnetMappings[i].mapAllIps(subnetMaps[i]);
            }
        }


        setupTimeSlices();
        setupTimeSlicesForBB();
        setupTimeSlicesForIPS();

        for (int i = 0; i < 3; i++)
        {
            currNfIpsSeen[i] = new Dictionary<long, ipDataStruct>();
            currBbIpsSeen[i] = new Dictionary<long, bbDataStruct>();
            currIPSIpsSeen[i] = new Dictionary<long, ipsDataStruct>();

            nextNfIpsSeen[i] = new Dictionary<long, ipDataStruct>();
            nextBbIpsSeen[i] = new Dictionary<long, bbDataStruct>();
            nextIPSIpsSeen[i] = new Dictionary<long, ipsDataStruct>();
        }



        currTimeIdx = -1;

        //getVals();

    }

    void temp()
    {
        IDbCommand cmd = dbconn.CreateCommand();
        string sql = "REINDEX time_recIpNum_idx;";
        
        cmd.CommandText = sql;

        cmd.ExecuteNonQuery();

    }

    // Update is called once per frame
    void Update() {

        if ((Input.GetKeyDown(KeyCode.Space) || inAnimation || currTimeIdx < 0 ) && !queryActive)
        {
            currTimeIdx++;

            if (currTimeIdx >= timeSlices.Length) currTimeIdx = 0;

            TimelineScript ts = timeline.GetComponent<TimelineScript>();
            ts.updateSliderPosition((float)currTimeIdx / (float)timeSlices.Length);

            Debug.Log("Getting DB Values");
            getVals();

            UpdateActiveTimeRange();
        }

        if (Input.GetKeyDown(KeyCode.C)) for (int i = 0; i < 3; i++) Debug.Log("Subnet " + (i + 1) + ": " + currNfIpsSeen[i].Count);


        if (queryActive)
        {
            long ipNum;
            string ipAddress = "";
            int numTimesSeen;
            int statusVal;

            long dstIpNum;
            string dstIpAddress;
            int priorityVal;

            int idx;

            // int numLoaded = 0;
            double currTime = DateTime.Now.TimeOfDay.TotalMilliseconds;
            double maxTime = currTime + iterTimeOffset;

            ipDataStruct tmpNfValue = new ipDataStruct();
            bbDataStruct tmpBbValue = new bbDataStruct();
            ipsDataStruct tmpIPSValue = new ipsDataStruct();

            bool foundInSubnet = false;

            while (currTime < maxTime && hasResultSets)
            {
                if (currDbDataType == dbDataType.NETWORK_FLOW)
                {
                    while (dataReader.Read())
                    {
                        try
                        {
                            ipAddress = dataReader.GetString(0);
                            ipNum = dataReader.GetInt64(1);
                            numTimesSeen = dataReader.GetInt32(2);

                            foundInSubnet = false;

                            for (idx = 0; idx < 3; idx++)
                            {
                                if (ipNum <= maxSubnetIpNum[idx] && ipNum >= minSubnetIpNum[idx])
                                {
                                    //nextIpsSeen[idx].



                                    if (!nextNfIpsSeen[idx].TryGetValue(ipNum, out tmpNfValue))
                                    {

                                        tmpNfValue = new ipDataStruct();
                                        tmpNfValue.ipAddress = ipAddress;
                                        tmpNfValue.numTimesSeen = numTimesSeen;
                                        nextNfIpsSeen[idx].Add(ipNum, tmpNfValue);   
                                    }
                                    else
                                    {
                                        tmpNfValue.numTimesSeen += numTimesSeen;
                                    }

                                    foundInSubnet = true;

                                    break;
                                }
                            }



                            if(!foundInSubnet)
                            {
                                tmpNfValue = new ipDataStruct();
                                tmpNfValue.ipAddress = ipAddress;
                                outOfNetIpsSeen.Add(ipNum, tmpNfValue);
                            } 
                        }

                        catch (System.InvalidCastException e)
                        {
                            Debug.Log(e);
                        }

                        currTime = DateTime.Now.TimeOfDay.TotalMilliseconds;

                        if (currTime >= maxTime) break;
                    }
                }
                else if (currDbDataType == dbDataType.BIG_BROTHER)
                {
                    while (dataReader.Read())
                    {
                        try
                        {
                            ipAddress = dataReader.GetString(0);

                            if( ipAddress == null || ipAddress.Length < 7 ) continue;

                            ipNum = dataReader.GetInt64(1);
                            statusVal = dataReader.GetInt32(2);

                            for (idx = 0; idx < 3; idx++)
                            {
                                if (ipNum <= maxSubnetIpNum[idx] && ipNum >= minSubnetIpNum[idx])
                                {
                                    //nextIpsSeen[idx].

                                    if (!nextBbIpsSeen[idx].TryGetValue(ipNum, out tmpBbValue))
                                    {

                                        tmpBbValue = new bbDataStruct();
                                        tmpBbValue.ipAddress = ipAddress;
                                        tmpBbValue.status = statusVal;
                                        nextBbIpsSeen[idx].Add(ipNum, tmpBbValue);

                                    }
                                    else
                                    {
                                        if (statusVal > tmpBbValue.status) tmpBbValue.status = statusVal;
                                    }
                                    break;
                                }
                            }
                        }

                        catch (System.InvalidCastException e)
                        {
                            Debug.Log(e.Message + " for ip/time: " + ipAddress + "/" + timeSlices[currTimeIdx]);
                        }

                        currTime = DateTime.Now.TimeOfDay.TotalMilliseconds;

                        if (currTime >= maxTime) break;
                    }
                }
                else if (currDbDataType == dbDataType.INTRUSION_PROTECTION)
                {
                    while (dataReader.Read())
                    {
                        try
                        {
                            ipAddress = dataReader.GetString(0);
                            ipNum = dataReader.GetInt64(1);
                            dstIpAddress = dataReader.GetString(2);
                            dstIpNum = dataReader.GetInt64(3);

                            priorityVal = dataReader.GetInt32(4);

                            for (idx = 0; idx < 3; idx++)
                            {
                                if (ipNum <= maxSubnetIpNum[idx] && ipNum >= minSubnetIpNum[idx])
                                {
                                    if (!nextIPSIpsSeen[idx].TryGetValue(ipNum, out tmpIPSValue))
                                    {
                                        tmpIPSValue = new ipsDataStruct();
                                        tmpIPSValue.ipAddress = ipAddress;
                                        tmpIPSValue.priority = priorityVal;
                                        nextIPSIpsSeen[idx].Add(ipNum, tmpIPSValue);
                                    }
                                    else
                                    {
                                        if (priorityVal > tmpIPSValue.priority) tmpIPSValue.priority = priorityVal;
                                    }

                                    break;
                                }
                            }

                            for (idx = 0; idx < 3; idx++)
                            {
                                if (dstIpNum <= maxSubnetIpNum[idx] && dstIpNum >= minSubnetIpNum[idx])
                                {
                                    if (!nextIPSIpsSeen[idx].TryGetValue(dstIpNum, out tmpIPSValue))
                                    {
                                        tmpIPSValue = new ipsDataStruct();
                                        tmpIPSValue.ipAddress = dstIpAddress;
                                        tmpIPSValue.priority = priorityVal;
                                        nextIPSIpsSeen[idx].Add(dstIpNum, tmpIPSValue);
                                    }
                                    else
                                    {
                                        if (priorityVal > tmpIPSValue.priority) tmpIPSValue.priority = priorityVal;
                                    }

                                    break;
                                }
                            }
                        }

                        catch (System.InvalidCastException e)
                        {
                            Debug.Log(e.Message);
                        }

                        currTime = DateTime.Now.TimeOfDay.TotalMilliseconds;

                        if (currTime >= maxTime) break;
                    }
                }

                if (currTime >= maxTime) break;
                else if (dataReader.NextResult())
                {
                    if (currDbDataType == dbDataType.NETWORK_FLOW) currDbDataType = dbDataType.BIG_BROTHER;
                    else if (currDbDataType == dbDataType.BIG_BROTHER) currDbDataType = dbDataType.INTRUSION_PROTECTION;
                    continue;
                }
                else hasResultSets = false;

            }

            if (!hasResultSets)
            {
                queryActive = false;

                queryEndTime = DateTime.Now.TimeOfDay;

                for (int i = 0; i < subnetObjects.Length; i++)
                {
                    currNfIpsSeen[i].Clear();
                    currBbIpsSeen[i].Clear();
                    currIPSIpsSeen[i].Clear();

                    currNfIpsSeen[i] = nextNfIpsSeen[i];
                    currBbIpsSeen[i] = nextBbIpsSeen[i];
                    currIPSIpsSeen[i] = nextIPSIpsSeen[i];

                    subnetMappings[i].activateNodes(nextNfIpsSeen[i]);
                    subnetMappings[i].activateBBNodes(nextBbIpsSeen[i]);
                    subnetMappings[i].activateIPSNodes(nextIPSIpsSeen[i]);
                }
            }

        }


    }

    public void startAnimation()
    {
        inAnimation = true;
    }

    public void stopAnimation()
    {
        inAnimation = false;
    }

    void setupTimeSlices()
    {
        long numSecs = (long)numSecondsPerSlice;

        long diff = maxUTCTime - minUTCTime;
        int numSlices = (int)Math.Ceiling((double)diff / (double)numSecs);

        timeSlices = new long[numSlices];
        timesliceNFCounts = new int[numSlices];

        timeSlices[0] = minUTCTime;
        for (int i = 1; i < timeSlices.Length; i++)
        {
            timeSlices[i] = timeSlices[i - 1] + numSecs;
        }

        bool getTotals = true;

        if (getTotals)
        {
            string sql = "";
            string tableName = "networkflow";

            if (numMinutesPerSlice % 60 == 0) tableName = "nfipcount_60";
            else if (numMinutesPerSlice % 30 == 0) tableName = "nfipcount_30";
            else if (numMinutesPerSlice % 10 == 0) tableName = "nfipcount_10";
            else if (numMinutesPerSlice % 5 == 0) tableName = "nfipcount_5";
            else tableName = "nfipcount_1";

            int maxCount = 10;

            TimeSpan startTime;
            TimeSpan endTime;

            startTime = DateTime.Now.TimeOfDay;

            maxCount = numSlices;
            for (int i = 0; i < numSlices - 1 && i < maxCount; i++)
            {
                IDbCommand cmd = dbconn.CreateCommand();

                sql = "SELECT COUNT(*) FROM " + tableName + " WHERE TimeSeconds>=" + timeSlices[i] + " AND TimeSeconds<" + timeSlices[i + 1] + ";";
                cmd.CommandText = sql;

                IDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    timesliceNFCounts[i] = reader.GetInt32(0);
                }


            }

            endTime = DateTime.Now.TimeOfDay;

            Vector3 offset = new Vector3(0.0f, 0.0f, -0.02f);

            TimelineScript timelineScript = timeline.GetComponent<TimelineScript>();
            timelineScript.createLines(timesliceNFCounts, Color.white, offset);

        }

    }


    void setupTimeSlicesForBB()
    {
        long numSecs = (long)numSecondsPerSlice;

        long diff = maxUTCTime - minUTCTime;
        //int numSlices = (int)Math.Ceiling((double)diff / (double)numSecs);


        timesliceBBCounts = new int[timeSlices.Length];



        bool getTotals = true;

        if (getTotals)
        {
            string sql = "";
            string tableName = "bigbrother";

            int maxCount = 10;

            TimeSpan startTime;
            TimeSpan endTime;

            startTime = DateTime.Now.TimeOfDay;

            maxCount = timeSlices.Length;
            for (int i = 0; i < timeSlices.Length - 1 && i < maxCount; i++)
            {
                IDbCommand cmd = dbconn.CreateCommand();

                sql = "SELECT COUNT(*) FROM " + tableName + " WHERE currenttime>=" + timeSlices[i] + " AND currenttime<" + timeSlices[i + 1] + ";";
                cmd.CommandText = sql;

                IDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    timesliceBBCounts[i] = reader.GetInt32(0);
                }

            }

            endTime = DateTime.Now.TimeOfDay;

            Vector3 offset = new Vector3(0.0f, 0.0f, -0.04f);

            TimelineScript timelineScript = timeline.GetComponent<TimelineScript>();
            timelineScript.createLines(timesliceBBCounts, Color.red, offset);



        }

    }

    void setupTimeSlicesForIPS()
    {
        
        timesliceIPSCounts = new int[timeSlices.Length];



        bool getTotals = true;

        if (getTotals)
        {
            string sql = "";
            string tableName = "ipsdata";

            int maxCount = 10;

            TimeSpan startTime;
            TimeSpan endTime;

            startTime = DateTime.Now.TimeOfDay;

            maxCount = timeSlices.Length;
            for (int i = 0; i < timeSlices.Length - 1 && i < maxCount; i++)
            {
                IDbCommand cmd = dbconn.CreateCommand();

                sql = "SELECT COUNT(*) FROM " + tableName + " WHERE dateTimeNum>=" + timeSlices[i] + " AND dateTimeNum<" + timeSlices[i + 1] + ";";
                cmd.CommandText = sql;

                IDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    timesliceIPSCounts[i] = reader.GetInt32(0);
                }

            }

            endTime = DateTime.Now.TimeOfDay;

            Vector3 offset = new Vector3(0.0f, 0.0f, 0.0f);

            TimelineScript timelineScript = timeline.GetComponent<TimelineScript>();
            timelineScript.createLines(timesliceIPSCounts, Color.yellow, offset);



        }

    }

    public void setTimeSlice(float inc)
    {
        if (inc <= 0.0f) setTimeSlice(0);
        else if (inc >= 1.0f) setTimeSlice(timeSlices.Length - 1);
        else setTimeSlice((int)(inc * (float)timeSlices.Length));
    }

    public void setTimeSlice(int inc)
    {
        if (currTimeIdx == inc) return;
        if (inc < 0 || inc >= timeSlices.Length) return;

        currTimeIdx = inc;
        getVals();

        UpdateActiveTimeRange();
    }

    private void UpdateActiveTimeRange()
    {
        if (realitySessionState == null)
        {
            return;
        }

        // First calculate the times
        TimeSpan whenStart = GetActiveTimeStart();
        TimeSpan whenEnd = GetActiveTimeEnd();  

        realitySessionState.SetActiveTimeRange(whenStart, whenEnd);
    }
    public TimeSpan GetActiveTimeStart()
    {
        long secondsCurrent = minUTCTime + ((long)currTimeIdx) * ((long)numSecondsPerSlice);
        TimeSpan whenStart = TimeSpan.FromSeconds(secondsCurrent);
        return whenStart;
    }
    public TimeSpan GetActiveTimeEnd()
    {
        long secondsCurrent = minUTCTime + ((long)currTimeIdx) * ((long)numSecondsPerSlice);
        TimeSpan whenEnd = TimeSpan.FromSeconds(secondsCurrent + numSecondsPerSlice);
        return whenEnd;
    }

    public int getTimeSliceIdx()
    {
        return currTimeIdx;
    }

    public float getTimeSliceFloat()
    {
        return (float)currTimeIdx/(float) timeSlices.Length;
    }

    void getVals()
    {
        if (currTimeIdx >= timeSlices.Length-1 || currTimeIdx < 0 ) return;

        for (int i = 0; i < subnetObjects.Length; i++)
        {
            nextNfIpsSeen[i] = new Dictionary<long, ipDataStruct>();
            nextBbIpsSeen[i] = new Dictionary<long, bbDataStruct>();
            nextIPSIpsSeen[i] = new Dictionary<long, ipsDataStruct>();
        }

        outOfNetIpsSeen.Clear();

        if (dataReader != null)
        {
            dataReader.Close();
            dataReader = null;
        }

        if (dbcmd != null)
        {
            dbcmd.Dispose();
            dbcmd = null;
        }
       
        string sql = "";
        string tableName = "networkflow";

        if (numMinutesPerSlice % 60 == 0) tableName = "nfipcount_60";
        else if (numMinutesPerSlice % 30 == 0) tableName = "nfipcount_30";
        else if (numMinutesPerSlice % 10 == 0) tableName = "nfipcount_10";
        else if (numMinutesPerSlice % 5 == 0) tableName = "nfipcount_5";
        else tableName = "nfipcount_1";

        sql = "SELECT ipAddress, ipNum, numTimesSeen from " + tableName + " WHERE TimeSeconds>=" + timeSlices[currTimeIdx] + " AND TimeSeconds<" + timeSlices[currTimeIdx + 1] + ";" +
            "SELECT receivedfrom, recIpNum, statusNum from bigbrother WHERE currenttime>= " + timeSlices[currTimeIdx] + " AND currenttime< " + timeSlices[currTimeIdx + 1] + "; " +
            "SELECT SrcIp, srcIpNum, destIp, dstIpNum, priorityNum from ipsdata WHERE dateTimeNum>= " + timeSlices[currTimeIdx] + " AND dateTimeNum< " + timeSlices[currTimeIdx + 1] + "; ";
        dbcmd = dbconn.CreateCommand();
        dbcmd.CommandText = sql;

        queryStartTime = DateTime.Now.TimeOfDay;

        dataReader = dbcmd.ExecuteReader();

        queryActive = true;
        hasResultSets = true;

        currDbDataType = dbDataType.NETWORK_FLOW;

    }

    void setupAllSubnets()
    {
        Dictionary<string, int> typeMap = new Dictionary<string, int>();
        string sql = "SELECT id, description FROM iptypemap;";
        IDbCommand cmd = dbconn.CreateCommand();
        cmd.CommandText = sql;

        int id;
        string desc;

        IDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            id = reader.GetInt32(0);
            desc = reader.GetString(1);

            typeMap.Add(desc, id);
        }

        int workstationId;

        if (!typeMap.TryGetValue("Workstation", out workstationId)) {
            Debug.LogError("Could not get Workstatin type ID");
            return;
        }




        sql = "SELECT subnet, ipNum, ipAddress, type FROM ipmap;";
        cmd = dbconn.CreateCommand();
        cmd.CommandText = sql;

        int subnet;
        long ipNum;
        string ipAddress;
        int type;

        ipInfo currInfo;


        reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            subnet = reader.GetInt32(0);
            ipNum = reader.GetInt64(1);
            ipAddress = reader.GetString(2);
            type = reader.GetInt32(3);

            currInfo = new ipInfo();

            currInfo.subnet = subnet;
            currInfo.ipAddress = ipAddress;
            if (type == workstationId) currInfo.type = ipType.WORKSTATION;
            else currInfo.type = ipType.SERVER;

            if( subnet > 0 && subnet < 4 )
            {
                subnetMaps[subnet-1].Add(ipNum, currInfo);
            }

        }



       
    }

    public static long getIpNumFromString(string s)
    {
        try
        {
            char[] sep = { '.' };
            string[] parts = s.Split(sep);

            long result = Int64.Parse(parts[3]);

            result += Int64.Parse(parts[2]) * 256;
            result += Int64.Parse(parts[1]) * 256 * 256;
            result += Int64.Parse(parts[0]) * 256 * 256 * 256;

            return result;
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
            return -1;
        }
    }

    void OnDestroy()
    {
        dbconn.Close();
        //dbconn = null;
    }

    public void getNFTrafficCountForNode(string ipAddress, out int numHits, out int numMinutes)
    {
        long ipNum = getIpNumFromString(ipAddress);
        getNFTrafficCountForNode(ipNum, out numHits, out numMinutes);
    }

    public void getNFTrafficCountForNode(long ipNum, out int numHits, out int numMinutes)
    {
        numHits = 0;
        numMinutes = numMinutesPerSlice;

        IDbCommand cmd = dbconn.CreateCommand();

        string sql = "";
        string tableName = "networkflow";

        if (numMinutesPerSlice % 60 == 0) tableName = "nfipcount_60";
        else if (numMinutesPerSlice % 30 == 0) tableName = "nfipcount_30";
        else if (numMinutesPerSlice % 10 == 0) tableName = "nfipcount_10";
        else if (numMinutesPerSlice % 5 == 0) tableName = "nfipcount_5";
        else tableName = "nfipcount_1";

        sql = "SELECT COUNT(*) FROM " + tableName + " WHERE TimeSeconds>=" + timeSlices[currTimeIdx] + 
            " AND TimeSeconds<" + timeSlices[currTimeIdx + 1] + 
            " AND ipNum = " + ipNum + ";";
        cmd.CommandText = sql;

        IDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            numHits += reader.GetInt32(0);
        }

        reader.Close();
    }

    public void getMostRecentBigBrotherStatusForNode(string ipAddress, out int status)
    {
        long ipNum = getIpNumFromString(ipAddress);

        getMostRecentBigBrotherStatusForNode(ipNum, out status);
    }

    public void getMostRecentBigBrotherStatusForNode(long ipNum, out int status)
    {

        IDbCommand cmd = dbconn.CreateCommand();

        long lastTime = -1;
        status = 0;

        string sql;
        if(currTimeIdx > 9)
        {
            sql = "SELECT MAX(currenttime) from bigbrother WHERE currenttime>= " + timeSlices[currTimeIdx - 10] +
                " AND currenttime<=" + timeSlices[currTimeIdx + 1] +
                " AND recIpNum = " + ipNum + "; ";
        }
        else
        {
            sql = "SELECT MAX(currenttime) from bigbrother WHERE currenttime<= " + timeSlices[currTimeIdx + 1] + " AND recIpNum = " + ipNum + "; ";
        }
        
        cmd.CommandText = sql;


        IDataReader reader = cmd.ExecuteReader();
        if(reader.Read())
        {
            try
            {
                lastTime = reader.GetInt64(0);
            }
            catch (Exception e)
            {
                lastTime = -1;
            }
        }
        reader.Close();

        if( lastTime < 0 )
        {
            return;
        }

        sql = "SELECT statusNum from bigbrother WHERE currenttime= " + lastTime + " AND recIpNum = " + ipNum + ";";
        cmd.CommandText = sql;

        reader = cmd.ExecuteReader();

        if (reader.Read())
        {
            try
            {
                status = reader.GetInt32(0);
            }
            catch (Exception e)
            {
                status = 0;
            }
        }

    }


    public List<ipTimelineDetails> getDetailedNFTrafficForNode(string ipAddress)
    {
        List<ipTimelineDetails> results = new List<ipTimelineDetails>();

        long ipNum = getIpNumFromString(ipAddress);
        if (ipNum < 0)
        {
            return results;
        }

        IDbCommand cmd = dbconn.CreateCommand();

        string sql = "";

        int numMinInEachDirection = 60;

        // go numMinInEachDirection in each direction +/- 1000 * 60 * numMinInEachDirection
        long minTime = timeSlices[currTimeIdx] - 1000 * 60 * numMinInEachDirection;
        long maxTime = timeSlices[currTimeIdx] + 1000 * 60 * numMinInEachDirection;

        
        sql = "SELECT TimeSeconds, firstSeenSrcIp, firstSeenDestIp, srcIpNum, dstIpNum, firstSeenSrcPayloadBytes, firstSeenDestPayloadBytes FROM networkflow" +
            " WHERE (srcIpNum = " + ipNum + " OR dstIpNum = " + ipNum + ") AND TimeSeconds>=" + minTime +
            " AND TimeSeconds<=" + maxTime + ";";
        
       
        cmd.CommandText = sql;

        IDataReader reader = cmd.ExecuteReader();
        do
        {
            while (reader.Read())
            {
                ipTimelineDetails tmpData = new ipTimelineDetails();
                tmpData.time = reader.GetDouble(0);
                tmpData.srcIp = reader.GetString(1);
                tmpData.dstIp = reader.GetString(2);
                tmpData.srcIpNum = reader.GetInt64(3);
                tmpData.dstIpNum = reader.GetInt64(4);
                tmpData.firstSeenSrcPayloadBytes = reader.GetInt32(5);
                tmpData.firstSeenDestPayloadBytes = reader.GetInt32(6);

                results.Add(tmpData);
            }
        } while (reader.NextResult());

        reader.Close();

        return results;
    }

    public List<ipTimelineDetails> getDetailedNFTrafficBetweenNodes(string ipAddress1, string ipAddress2)
    {
        List<ipTimelineDetails> results = new List<ipTimelineDetails>();

        long ipNum1 = getIpNumFromString(ipAddress1);
        long ipNum2 = getIpNumFromString(ipAddress2);
        if (ipNum1 < 0 || ipNum2 < 0 )
        {
            return results;
        }

        IDbCommand cmd = dbconn.CreateCommand();

        string sql = "";

        int numMinInEachDirection = 60;

        // go numMinInEachDirection in each direction +/- 1000 * 60 * numMinInEachDirection
        long minTime = timeSlices[currTimeIdx] - 1000 * 60 * numMinInEachDirection;
        long maxTime = timeSlices[currTimeIdx] + 1000 * 60 * numMinInEachDirection;

        sql = "SELECT TimeSeconds, firstSeenSrcIp, firstSeenDestIp, srcIpNum, dstIpNum, firstSeenSrcPayloadBytes, firstSeenDestPayloadBytes FROM networkflow" +
            " WHERE TimeSeconds>=" + minTime +
            " AND TimeSeconds<=" + maxTime +
        " AND ( (srcIpNum = " + ipNum1 + " AND dstIpNum = " + ipNum2 + " ) OR (srcIpNum = " + ipNum2 + " AND dstIpNum = " + ipNum1 + " ) );";
        cmd.CommandText = sql;

        IDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            ipTimelineDetails tmpData = new ipTimelineDetails();
            tmpData.time = reader.GetDouble(0);
            tmpData.srcIp = reader.GetString(1);
            tmpData.dstIp = reader.GetString(2);
            tmpData.srcIpNum = reader.GetInt64(3);
            tmpData.dstIpNum = reader.GetInt64(4);
            tmpData.firstSeenSrcPayloadBytes = reader.GetInt32(5);
            tmpData.firstSeenDestPayloadBytes = reader.GetInt32(6);

            results.Add(tmpData);
        }

        reader.Close();


        return results;
    }

    public Dictionary<long, ipInfo>[] getSubnetMaps()
    {
        return subnetMaps;
    }

}
