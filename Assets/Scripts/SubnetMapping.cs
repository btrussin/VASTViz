using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public class SubnetMapping : MonoBehaviour {

    //public string pointPrefabPath = "Prefabs/PointPrefab.prefab"; //Set by default. May be changed in the editor.
    public GameObject ptPrefab = null;

    Dictionary<int, GameObject> nfMap = new Dictionary<int, GameObject>();
    Dictionary<int, GameObject> bbMap = new Dictionary<int, GameObject>();
    Dictionary<int, GameObject> ipsMap = new Dictionary<int, GameObject>();

    Dictionary<int, Vector3> ptVectorMap;

    float minDomain = 0.0f;
    float maxDomain = 1.0f;
    float minRange = 0.0f;
    float maxRange = 1.0f;

    float conversionScale = 1.0f;

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

    private float clamp(float val, float min, float max)
    {
        if (val < min) return min;
        else if (val > max) return max;
        else return val;
    }

    public float getScaleVal(float d)
    {
        return clamp((d - minDomain) * conversionScale + minRange, minRange, maxRange);
    }



    // Use this for initialization
    void Start () {
        //ptPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(pointPrefabPath);

        setDomain(1.0f, 2000.0f);
        setRange(0.01f, 0.05f);
        setScale();
    }
	
	// Update is called once per frame
	void Update () {
	
	}

    private Vector3[] getSubnetVectorsWalk(int numPoints)
    {
        //RandomWalk2D walk = RandomWalk2D.createWalk(numPoints, 50, 10);
        RandomWalk2D walk = RandomWalk2D.createWalk(numPoints, 100, numPoints);


        walk.addOffset(-0.5f, -0.5f);

        List<Vector2> vList = walk.getPoints();

        Vector3[] tmpVecs = new Vector3[vList.Count];

        int idx = 0;
        foreach ( Vector2 v in vList )
        {
            tmpVecs[idx] = new Vector3(v.x, v.y, 0.0f);

            idx++;
        }

        return tmpVecs;
    }

    private Vector3[] getSubnetVectors(int width, int height)
    {
        Vector3[] tmpVecs = new Vector3[height * width];

        int num = 0;
        float w = 1.0f / (float)width;
        float h = 1.0f / (float)height;

        float currH = 0.5f;
        float currW = -0.5f;

        Random rand = new Random();
        Vector2 whR;

        for (int i = 0; i < height; i++)
        {
            currW = -0.5f;
            for (int j = 0; j < width; j++, num++)
            {
                whR = Random.insideUnitCircle;

                tmpVecs[num] = new Vector3(currW + whR.x * w, currH + whR.y * h, 0.0f);
                currW += w;
            }

            currH -= h;
        }

        return tmpVecs;
    }

    public void mapAllIps(Dictionary<long, string> map)
    {
        
        int lowerByte;

        int numInMap = map.Count;

        int vecLayoutWidth = (int)Mathf.Ceil(Mathf.Sqrt((float)numInMap));
        int vecLayoutHeight = vecLayoutWidth;
        int currSize;
        while(true)
        {
            currSize = vecLayoutHeight * vecLayoutWidth;
            if (currSize == numInMap) break;
            else if( currSize < numInMap)
            {
                vecLayoutHeight++;
                break;
            }

            vecLayoutHeight--;
        }

        //Vector3[] tmpVecs = getSubnetVectorsWalk(numInMap);
        Vector3[] tmpVecs = getSubnetVectors(vecLayoutWidth, vecLayoutHeight);
        Vector3 bbOffset = new Vector3(0.0f, 0.0f, -0.1f);
        Vector3 ipsOffset = new Vector3(0.0f, 0.0f, -0.2f);

        int num = 0;
        foreach (KeyValuePair<long, string> entry in map )
        {
            // min domain = 1
            // max domain = 2000

            // min range = 0.005
            // max range = 0.1


            lowerByte = (int)(entry.Key & 0xFFFF);

            // the network flow points
            GameObject nfPoint = (GameObject)Instantiate(ptPrefab);
            nfPoint.name = "IP(NF): " + entry.Value;

            nfPoint.transform.SetParent(transform);
            nfPoint.transform.position = tmpVecs[num] + transform.position;
            nfPoint.transform.localScale = new Vector3(0.05f, 0.05f, 1.0f);

            nfPoint.SetActive(false);

            nfMap.Add(lowerByte, nfPoint);

            // the big brother points
            GameObject bbPoint = (GameObject)Instantiate(ptPrefab);
            bbPoint.name = "IP(BB): " + entry.Value;

            bbPoint.transform.SetParent(transform);
            bbPoint.transform.position = tmpVecs[num] + transform.position + bbOffset;
            bbPoint.transform.localScale = new Vector3(0.05f, 0.05f, 1.0f);

            bbPoint.SetActive(false);

            bbMap.Add(lowerByte, bbPoint);


            // the IPS points
            GameObject ipsPoint = (GameObject)Instantiate(ptPrefab);
            ipsPoint.name = "IP(IPS): " + entry.Value;

            ipsPoint.transform.SetParent(transform);
            ipsPoint.transform.position = tmpVecs[num] + transform.position + ipsOffset;
            ipsPoint.transform.localScale = new Vector3(0.05f, 0.05f, 1.0f);

            ipsPoint.SetActive(false);

            ipsMap.Add(lowerByte, ipsPoint);




            num++;
        }
    }

    public void activateNodes(Dictionary<long, ipDataStruct> map)
    {
        float tScaleVal;


        foreach (KeyValuePair<int, GameObject> entry in nfMap)
        {
            entry.Value.SetActive(false);
        }

        int lowerByte;
        GameObject g;

        foreach (KeyValuePair<long, ipDataStruct> entry in map)
        {
            lowerByte = (int)(entry.Key & 0xFFFF);
            if (nfMap.TryGetValue(lowerByte, out g))
            {
                tScaleVal = getScaleVal((float)entry.Value.numTimesSeen);

                g.transform.localScale = new Vector3(tScaleVal, tScaleVal, 1.0f);

                g.SetActive(true);
            }
        }
    }

    public void activateBBNodes(Dictionary<long, bbDataStruct> map)
    {
        if (map.Count < 1) return;

        Material mat;
        GameObject g;

        float scale = maxRange * 0.15f + minRange * 0.85f;

        foreach (KeyValuePair<int, GameObject> entry in bbMap)
        {
            g = entry.Value;
            g.SetActive(true);
            g.transform.localScale = new Vector3(scale, scale, 1.0f);
            mat = g.GetComponent<MeshRenderer>().material;
            mat.color = Color.gray;
        }

        int lowerByte;
        

        

        scale = maxRange * 0.75f + minRange * 0.25f;

        foreach (KeyValuePair<long, bbDataStruct> entry in map)
        {
            lowerByte = (int)(entry.Key & 0xFFFF);
            if (bbMap.TryGetValue(lowerByte, out g))
            {
                g.transform.localScale = new Vector3(scale, scale, 1.0f);
                g.SetActive(true);

                mat = g.GetComponent<MeshRenderer>().material;
                switch(entry.Value.status)
                {
                    case 3:
                        mat.color = Color.red;
                        break;
                    case 2:
                        mat.color = Color.yellow;
                        break;
                    case 1:
                        mat.color = Color.green;
                        break;
                    default:
                        g.SetActive(false);
                        break;
                }
            }
        }
    }

    public void activateIPSNodes(Dictionary<long, ipsDataStruct> map)
    {
        Debug.Log("Trying to show " + map.Count + " IPS nodes");

        foreach (KeyValuePair<int, GameObject> entry in ipsMap)
        {
            entry.Value.SetActive(false);
        }

        int lowerByte;
        GameObject g;

        Material mat;
        float scale = maxRange * 0.25f + minRange * 0.75f;

        foreach (KeyValuePair<long, ipsDataStruct> entry in map)
        {
            lowerByte = (int)(entry.Key & 0xFFFF);
            if (ipsMap.TryGetValue(lowerByte, out g))
            {
                g.transform.localScale = new Vector3(scale, scale, 1.0f);
                g.SetActive(true);

                mat = g.GetComponent<MeshRenderer>().material;
                switch (entry.Value.priority)
                {
                    case 6:
                    case 5:
                    case 4:
                        mat.color = Color.red;
                        break;
                    case 3:
                    case 2:
                        mat.color = Color.yellow;
                        break;
                    case 1:
                    case 0:
                        mat.color = Color.green;
                        break;
                    default:
                        g.SetActive(false);
                        break;
                }
            }
        }
    }

}
