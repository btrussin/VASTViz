using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public class SubnetMapping : MonoBehaviour {

    //public string pointPrefabPath = "Prefabs/PointPrefab.prefab"; //Set by default. May be changed in the editor.
    public GameObject ptPrefab = null;

    Dictionary<int, GameObject> ptMap = new Dictionary<int, GameObject>();

    Dictionary<int, Vector3> ptVectorMap;



    // Use this for initialization
    void Start () {
        //ptPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(pointPrefabPath);

        
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

        int num = 0;
        foreach (KeyValuePair<long, string> entry in map )
        {

            lowerByte = (int)(entry.Key & 0xFFFF);
            GameObject point = (GameObject)Instantiate(ptPrefab);
            point.name = "IP: " + entry.Value;

            point.transform.SetParent(transform);
            point.transform.position = tmpVecs[num] + transform.position;

            point.SetActive(false);

            ptMap.Add(lowerByte, point);
            num++;
        }
    }

    public void activateNodes(Dictionary<long, string> map)
    {
        foreach (KeyValuePair<int, GameObject> entry in ptMap)
        {
            entry.Value.SetActive(false);
        }

        int lowerByte;
        GameObject g;

        foreach (KeyValuePair < long, string> entry in map)
        {
            lowerByte = (int)(entry.Key & 0xFFFF);
            if (ptMap.TryGetValue(lowerByte, out g))
            {
                g.SetActive(true);
            }
        }
    }

}
