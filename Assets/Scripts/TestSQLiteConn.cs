using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Mono.Data.Sqlite;
using System.Data;
using System;

public class TestSQLiteConn : MonoBehaviour {

    Dictionary<long, string>[] subnetMaps = new Dictionary<long, string>[3];

    long[] minSubnetIpNum = new long[3];
    long[] maxSubnetIpNum = new long[3];

    long minUTCTime = 1364802600; // first minute: 1364802600:  04-01-2013 07:50:00
    long maxUTCTime = 1366045200; // last minute: 1366045200: 04-15-2013 10:00:00

    long[] timeSlices;
    int[] timeSliceCounts;

    int numMinutesPerSlice = 30;
    int numSecondsPerSlice;

    bool queryActive = false;

    Dictionary<long, String>[] prevIpsSeen = new Dictionary<long, String>[3];
    Dictionary<long, String>[] currIpsSeen = new Dictionary<long, String>[3];
    Dictionary<long, String>[] nextIpsSeen = new Dictionary<long, String>[3];

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

        for (int i = 0; i < 3; i++)
        {
            prevIpsSeen[i] = new Dictionary<long, String>();
            nextIpsSeen[i] = new Dictionary<long, String>();
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

        if (Input.GetKeyDown(KeyCode.C)) for (int i = 0; i < 3; i++) Debug.Log("Subnet " + (i + 1) + ": " + currIpsSeen[i].Count);


        if (queryActive)
        {
            long ipNum;
            string ipAddress;

            int idx;

            // int numLoaded = 0;
            double currTime = DateTime.Now.TimeOfDay.TotalMilliseconds;
            double maxTime = currTime + iterTimeOffset;

            string tmpValue;

            while (currTime < maxTime && hasResultSets)
            {
                while (dataReader.Read())
                {
                    try
                    {
                        ipAddress = dataReader.GetString(0);
                        ipNum = dataReader.GetInt64(1);

                        for (idx = 0; idx < 3; idx++)
                        {
                            if (ipNum <= maxSubnetIpNum[idx] && ipNum >= minSubnetIpNum[idx])
                            {
                                if (!nextIpsSeen[idx].TryGetValue(ipNum, out tmpValue))
                                {
                                    nextIpsSeen[idx].Add(ipNum, ipAddress);
                                }

                                break;
                            }
                        }
                    }
                    catch (System.InvalidCastException e) { }

                    //numLoaded++;
                    currTime = DateTime.Now.TimeOfDay.TotalMilliseconds;

                    if (currTime >= maxTime) break;

                }

                if (currTime >= maxTime) break;
                else if (dataReader.NextResult()) continue;
                else hasResultSets = false;

            }

            if (!hasResultSets)
            {
                queryActive = false;

                queryEndTime = DateTime.Now.TimeOfDay;

                for (int i = 0; i < subnetObjects.Length; i++)
                {

                    if (prevIpsSeen[i] != null) prevIpsSeen[i].Clear();
                    prevIpsSeen[i] = currIpsSeen[i];
                    currIpsSeen[i] = nextIpsSeen[i];

                    subnetMappings[i].activateNodes(nextIpsSeen[i]);
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
        timeSliceCounts = new int[numSlices];

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

            if (numMinutesPerSlice % 60 == 0) tableName = "nfminute_60";
            else if (numMinutesPerSlice % 30 == 0) tableName = "nfminute_30";
            else if (numMinutesPerSlice % 10 == 0) tableName = "nfminute_10";
            else if (numMinutesPerSlice % 5 == 0) tableName = "nfminute_5";
            else tableName = "nfminute_1";

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
                    timeSliceCounts[i] = reader.GetInt32(0);
                }

            }

            endTime = DateTime.Now.TimeOfDay;

            Debug.Log("Time: ");

            for (int i = 0; i < numSlices - 1 && i < maxCount; i++)
            {
                //Debug.Log("["+i+"]: " + timeSliceCounts[i]);
            }

            TimelineScript timelineScript = timeline.GetComponent<TimelineScript>();
            timelineScript.createLines(timeSliceCounts);

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
        if (currTimeIdx >= timeSlices.Length) return;

        for (int i = 0; i < subnetObjects.Length; i++)
        {
            nextIpsSeen[i] = new Dictionary<long, string>();

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

        if (numMinutesPerSlice % 60 == 0) tableName = "nfminute_60";
        else if (numMinutesPerSlice % 30 == 0) tableName = "nfminute_30";
        else if (numMinutesPerSlice % 10 == 0) tableName = "nfminute_10";
        else if (numMinutesPerSlice % 5 == 0) tableName = "nfminute_5";
        else tableName = "nfminute_1";

        sql = "SELECT firstSeenSrcIp, srcIpNum from " + tableName + " WHERE TimeSeconds>=" + timeSlices[currTimeIdx] + " AND TimeSeconds<" + timeSlices[currTimeIdx + 1] + ";" +
                 "SELECT firstSeenDestIp, dstIpNum from " + tableName + " WHERE TimeSeconds>=" + timeSlices[currTimeIdx] + " AND TimeSeconds<" + timeSlices[currTimeIdx + 1] + ";" +
                 "SELECT receivedfrom, recIpNum from bigbrother WHERE currenttime>=" + timeSlices[currTimeIdx] + " AND currenttime<" + timeSlices[currTimeIdx + 1] + ";";
        dbcmd = dbconn.CreateCommand();
        dbcmd.CommandText = sql;

        queryStartTime = DateTime.Now.TimeOfDay;

        dataReader = dbcmd.ExecuteReader();

        queryActive = true;
        hasResultSets = true;
        
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
