﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Mono.Data.Sqlite;
using System.Data;
using System;

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

    Dictionary<long, string>[] subnetMaps = new Dictionary<long, string>[3];

    long[] minSubnetIpNum = new long[3];
    long[] maxSubnetIpNum = new long[3];

    long minUTCTime = 1364802600; // first minute: 1364802600:  04-01-2013 07:50:00
    long maxUTCTime = 1366045200; // last minute: 1366045200: 04-15-2013 17:00:00

    long[] timeSlices;
    int[] timesliceNFCounts;
    int[] timesliceBBCounts;
    int[] timesliceIPSCounts;

    int numMinutesPerSlice = 5;
    int numSecondsPerSlice;

    bool queryActive = false;

    Dictionary<long, ipDataStruct>[] currNfIpsSeen = new Dictionary<long, ipDataStruct>[3];
    Dictionary<long, ipDataStruct>[] nextNfIpsSeen = new Dictionary<long, ipDataStruct>[3];

    Dictionary<long, bbDataStruct>[] currBbIpsSeen = new Dictionary<long, bbDataStruct>[3];
    Dictionary<long, bbDataStruct>[] nextBbIpsSeen = new Dictionary<long, bbDataStruct>[3];

    Dictionary<long, ipsDataStruct>[] currIPSIpsSeen = new Dictionary<long, ipsDataStruct>[3];
    Dictionary<long, ipsDataStruct>[] nextIPSIpsSeen = new Dictionary<long, ipsDataStruct>[3];

    public double maxIterTime = 0.005;
    double iterTimeOffset;

    bool hasResultSets;


    IDbConnection dbconn;
    IDbCommand dbcmd = null;
    IDataReader dataReader = null;

    TimeSpan queryStartTime;
    TimeSpan queryEndTime;

    public int currTimeIdx;

    public GameObject[] subnetObjects = new GameObject[3];
    SubnetMapping[] subnetMappings = new SubnetMapping[3];
    public GameObject timeline;

    dbDataType currDbDataType = dbDataType.NETWORK_FLOW;

    // Use this for initialization
    void Start() {

        iterTimeOffset = maxIterTime * 1000.0;

        for (int i = 0; i < 3; i++)
        {
            subnetMaps[i] = new Dictionary<long, string>();

            minSubnetIpNum[i] = getIpNumFromString("172." + (i + 1) + "0.0.0");
            maxSubnetIpNum[i] = getIpNumFromString("172." + (i + 1) + "0.255.255");

        }

        string conn = "URI=file:C:\\Users\\btrus\\Documents\\VAST\\sqlite\\vast.db";

        //conn = "URI=file:" + Application.dataPath + "/vast.db";

        Debug.Log("Trying to connect to: " + conn);

        dbconn = (IDbConnection)new SqliteConnection(conn);

        dbconn.Open(); //Open connection to the database.

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



        currTimeIdx = 0;

        getVals();

    }

    // Update is called once per frame
    void Update() {



        if (Input.GetKeyDown(KeyCode.Space) && !queryActive)
        {
            currTimeIdx++;

            if (currTimeIdx >= timeSlices.Length) currTimeIdx = 0;

            getVals();
        }

        if (Input.GetKeyDown(KeyCode.C)) for (int i = 0; i < 3; i++) Debug.Log("Subnet " + (i + 1) + ": " + currNfIpsSeen[i].Count);


        if (queryActive)
        {
            long ipNum;
            string ipAddress;
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

                                    break;
                                }
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
                            Debug.Log(e.Message);
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

            TimelineScript timelineScript = timeline.GetComponent<TimelineScript>();
            timelineScript.createLines(timesliceNFCounts, Color.white, Vector3.zero);

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

            Vector3 offset = new Vector3(0.0f, 0.0f, -0.02f);

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

            Vector3 offset = new Vector3(0.0f, 0.0f, -0.04f);

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

        string[] lines = System.IO.File.ReadAllLines(@"C:\\Users\\btrus\\Documents\\VAST\\sqlite\\uniqueIPsBySubnet.txt");
        char[] sep = { '|' };

        long ipNum;

        foreach ( string line in lines )
        {
            string[] parts = line.Split(sep);
            if (parts.Length != 3)
            {
                Debug.Log("Parts has " + parts.Length + " things in it");
                continue;
            }
            ipNum = Int64.Parse(parts[1]);

            if ( parts[0].CompareTo("1") == 0 )
            {
                subnetMaps[0].Add(ipNum, parts[2]);
            }
            else if (parts[0].CompareTo("2") == 0)
            {
                subnetMaps[1].Add(ipNum, parts[2]);
            }
            else if (parts[0].CompareTo("3") == 0)
            {
                subnetMaps[2].Add(ipNum, parts[2]);
            }

        }

    }

    public static long getIpNumFromString(string s)
    {
        char[] sep = { '.' };
        string[] parts = s.Split(sep);

        long result = Int64.Parse(parts[3]);

        result += Int64.Parse(parts[2]) * 256;
        result += Int64.Parse(parts[1]) * 256 * 256;
        result += Int64.Parse(parts[0]) * 256 * 256 * 256;

        return result;
    }

    private void OnDestroy()
    {
        dbconn.Close();
        dbconn = null;
    }
}
